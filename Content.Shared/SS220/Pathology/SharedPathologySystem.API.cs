// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Content.Shared.Traits;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.SS220.Pathology;

public abstract partial class SharedPathologySystem
{
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private INetManager _net = default!;

    /// <summary>
    /// Deals damage that is not a hit, e.g. of a surgery step: it doesn't roll traumas and doesn't tear body parts off.
    /// </summary>
    public bool TryChangeDamageWithoutTraumas(
        EntityUid target,
        DamageSpecifier damage,
        out DamageSpecifier appliedDamage,
        bool ignoreResistances = false,
        EntityUid? origin = null)
    {
        var holder = CompOrNull<PathologyHolderComponent>(target);
        if (holder is not null)
            holder.DamageWithoutTraumas = true;

        try
        {
            return _damageable.TryChangeDamage(target, damage, out appliedDamage, ignoreResistances, origin: origin);
        }
        finally
        {
            if (holder is not null)
                holder.DamageWithoutTraumas = false;
        }
    }

    public static bool IsSevere(PathologyPrototype prototype, PathologyInstanceData data)
    {
        if (prototype.IsSevere || data.Level > 0)
            return true;

        return prototype.Definition[data.Level].StacksAreSevere && data.StackCount > OneStack;
    }

    public static LocId GetName(PathologyPrototype prototype, PathologyInstanceData data)
    {
        return prototype.Definition[data.Level].Name ?? prototype.Name;
    }

    public static LocId? GetOperationRecommendation(PathologyPrototype prototype, PathologyInstanceData data)
    {
        return prototype.Definition[data.Level].OperationRecommendation ?? prototype.OperationRecommendation;
    }

    /// <summary>
    /// Rolls <paramref name="chancePerStack"/> for each stack of the pathology, true if any roll passed.
    /// Effects can't roll per stack by themselves: their probability is seeded by the tick and the entity, so all the rolls of a tick are equal.
    /// </summary>
    public bool RollChancePerStack(PathologyInstanceData data, float chancePerStack)
    {
        var chance = 1f - MathF.Pow(1f - Math.Clamp(chancePerStack, 0f, 1f), data.StackCount);
        return _random.Prob(chance);
    }

    public bool TryAddRandom(Entity<PathologyHolderComponent?> entity, ProtoId<WeightedRandomPrototype> weightedPathology, float chance, IPathologyContext? context = null)
    {
        if (!_prototype.Resolve(weightedPathology, out var weightedRandomPrototype))
            return false;

        return TryAddRandom(entity, weightedRandomPrototype.Weights, chance, context);
    }

    /// <param name="level">Minimal level of the added pathology, see <see cref="TryAddPathology"/></param>
    public bool TryAddRandom(Entity<PathologyHolderComponent?> entity, Dictionary<string, float> weightsPathology, float chance, IPathologyContext? context = null, int level = 0)
    {
        // no shared random, so we drop
        if (_net.IsClient)
            return false;

        if (!Resolve(entity.Owner, ref entity.Comp, false))
            return false;

        if (chance < 1f && !_random.Prob(chance))
            return false;

        var correctInput = new Dictionary<string, float>(weightsPathology.Count);
        foreach (var (pathologyId, weight) in weightsPathology)
        {
            if (_prototype.HasIndex<PathologyPrototype>(pathologyId) && CanAddPathology(entity, pathologyId, level))
                correctInput[pathologyId] = weight;
        }

        if (correctInput.Count == 0)
            return false;

        var pickedPathology = _random.Pick(correctInput);

        return TryAddPathology(entity, pickedPathology, context, level);
    }

    /// <summary>
    /// Adds the pathology, or a stack to it when the entity already has it.
    /// An active pathology is worsened instead when it is below <paramref name="level"/> or its level <see cref="PathologyDefinition.WorsensOnRepeat"/>.
    /// </summary>
    /// <param name="level">Minimal level of the pathology, e.g. a strong burn starts right from necrosis</param>
    public bool TryAddPathology(Entity<PathologyHolderComponent?> entity, ProtoId<PathologyPrototype> pathologyId, IPathologyContext? context = null, int level = 0)
    {
        if (!Resolve(entity.Owner, ref entity.Comp, false))
            return false;

        if (!_prototype.Resolve(pathologyId, out var pathologyPrototype))
            return false;

        entity.Comp.ActivePathologies.TryGetValue(pathologyId, out var instanceData);
        var targetLevel = GetAddedLevel(pathologyPrototype, instanceData, level);

        if (instanceData is not null
            && targetLevel == instanceData.Level
            && instanceData.StackCount >= pathologyPrototype.Definition[instanceData.Level].MaxStackCount)
        {
            return false;
        }

        var attemptEv = new PathologyAddedAttempt(pathologyId);
        RaiseLocalEvent(entity, ref attemptEv);

        if (attemptEv.Cancelled)
            return false;

        if (instanceData is not null && targetLevel == instanceData.Level)
        {
            TryChangePathologyStack(entity, pathologyPrototype, context: context);
            return true;
        }

        // a new pathology that starts right from a later level doesn't show the popups of the levels it skipped
        var isNew = instanceData is null;
        if (instanceData is null)
        {
            StartPathology(entity!, pathologyPrototype, context);
            instanceData = entity.Comp.ActivePathologies[pathologyId];
        }

        while (instanceData.Level < targetLevel)
        {
            AdvanceLevel(entity!, pathologyPrototype, instanceData, context, showPopup: !isNew);
        }

        return true;
    }

    /// <summary>
    /// Level the pathology gets when it is added at least at <paramref name="level"/>.
    /// </summary>
    private static int GetAddedLevel(PathologyPrototype prototype, PathologyInstanceData? instanceData, int level)
    {
        var maxLevel = prototype.Definition.Length - 1;
        if (instanceData is null)
            return Math.Clamp(level, 0, maxLevel);

        var worsened = prototype.Definition[instanceData.Level].WorsensOnRepeat ? instanceData.Level + 1 : instanceData.Level;
        return Math.Clamp(Math.Max(worsened, level), instanceData.Level, maxLevel);
    }

    public bool TryRemovePathology(Entity<PathologyHolderComponent?> entity, ProtoId<PathologyPrototype> pathologyId, bool checkStacks = true)
    {
        if (!Resolve(entity.Owner, ref entity.Comp, logMissing: false))
            return false;

        if (!_prototype.Resolve(pathologyId, out var pathologyPrototype))
            return false;

        if (!entity.Comp.ActivePathologies.TryGetValue(pathologyId, out var instanceData))
            return false;

        if (checkStacks && instanceData.StackCount > OneStack)
            return false;

        var ev = new PathologyRemoveAttempt(pathologyId, instanceData.Level);
        RaiseLocalEvent(entity, ref ev);
        if (ev.Cancelled)
            return false;

        RemovePathology(entity!, pathologyPrototype);
        return true;
    }

    /// <summary>
    /// Puts a pathology instance taken from another holder (e.g. from an inserted organ) on the entity, keeping its level, stacks and contexts.
    /// Raises <see cref="PathologyAddedAttempt"/>. Returns false if it was cancelled or the pathology is already active.
    /// </summary>
    public bool TryRestorePathology(Entity<PathologyHolderComponent?> entity, ProtoId<PathologyPrototype> pathologyId, PathologyInstanceData data)
    {
        if (!Resolve(entity.Owner, ref entity.Comp, logMissing: false))
            return false;

        if (!_prototype.Resolve(pathologyId, out var pathologyPrototype))
            return false;

        if (entity.Comp.ActivePathologies.ContainsKey(pathologyId))
            return false;

        var attemptEv = new PathologyAddedAttempt(pathologyId);
        RaiseLocalEvent(entity, ref attemptEv);

        if (attemptEv.Cancelled)
            return false;

        var addedEv = new PathologyAddedEvent(pathologyId);
        RaiseLocalEvent(entity, ref addedEv);

        data.Level = Math.Min(data.Level, pathologyPrototype.Definition.Length - 1);
        entity.Comp.ActivePathologies.Add(pathologyId, data);

        // replay the progression, so every level applies its traits and status effects as if it was reached naturally
        for (var level = 0; level <= data.Level; level++)
        {
            AddTrait(entity!, pathologyPrototype.Definition[level].Trait);

            var severityEv = new PathologySeverityChanged(pathologyId, level - 1, level);
            RaiseLocalEvent(entity, ref severityEv);
        }

        var stackEv = new PathologyStackCountChanged(pathologyId, data.Level, 0, data.StackCount);
        RaiseLocalEvent(entity, ref stackEv);

        Dirty(entity);
        return true;
    }

    /// <summary>
    /// Lowers the pathology to <paramref name="toLevel"/> or by one level if it isn't set,
    /// e.g. when surgery repairs a failed organ back to damaged.
    /// Effects of the dropped levels are removed and the stack count is set to <paramref name="stackCount"/>.
    /// Returns false if the pathology is absent or already at or below the target level.
    /// </summary>
    public bool TryRegressPathology(Entity<PathologyHolderComponent?> entity, ProtoId<PathologyPrototype> pathologyId, int stackCount = 1, int? toLevel = null)
    {
        if (!Resolve(entity.Owner, ref entity.Comp, logMissing: false))
            return false;

        if (!_prototype.Resolve(pathologyId, out var pathologyPrototype))
            return false;

        if (!entity.Comp.ActivePathologies.TryGetValue(pathologyId, out var instanceData))
            return false;

        var targetLevel = toLevel ?? instanceData.Level - 1;
        if (targetLevel < 0 || targetLevel >= instanceData.Level)
            return false;

        var previousLevel = instanceData.Level;
        var previousStackCount = instanceData.StackCount;
        var preservedContext = GetFirstContext(instanceData);

        instanceData.Level = targetLevel;
        instanceData.LevelStartTime = _gameTiming.CurTime;
        instanceData.StackCount = Math.Clamp(stackCount, OneStack, pathologyPrototype.Definition[instanceData.Level].MaxStackCount);

        instanceData.PathologyContexts.Clear();
        instanceData.PathologyContexts.Add(preservedContext);
        for (var i = OneStack; i < instanceData.StackCount; i++)
        {
            instanceData.PathologyContexts.Add(null);
        }

        RemoveDefinitionEffects(entity!, pathologyPrototype, targetLevel + 1, previousLevel);

        var severityEv = new PathologySeverityChanged(pathologyId, previousLevel, instanceData.Level);
        RaiseLocalEvent(entity, ref severityEv);

        var stackEv = new PathologyStackCountChanged(pathologyId, instanceData.Level, previousStackCount, instanceData.StackCount);
        RaiseLocalEvent(entity, ref stackEv);

        DebugTools.Assert(instanceData.PathologyContexts.Count == instanceData.StackCount);
        Dirty(entity);
        return true;
    }

    public bool HavePathology(Entity<PathologyHolderComponent?> entity, ProtoId<PathologyPrototype> pathologyId)
    {
        if (!Resolve(entity.Owner, ref entity.Comp, logMissing: false))
            return false;

        return entity.Comp.ActivePathologies.ContainsKey(pathologyId);
    }

    /// <summary>
    /// Adds the pathology with the given stack count, or adds the stacks to it when the entity already has it.
    /// </summary>
    public bool TryAddOrStackPathology(Entity<PathologyHolderComponent?> entity, ProtoId<PathologyPrototype> pathologyId, int stacks = 1)
    {
        if (HavePathology(entity, pathologyId))
            return TryChangePathologyStack(entity, pathologyId, stacks);

        if (!TryAddPathology(entity, pathologyId))
            return false;

        return stacks <= 1 || TryChangePathologyStack(entity, pathologyId, stacks - 1);
    }

    /// <summary>
    /// Whether an active pathology gives the trait, so its components belong to the pathology.
    /// </summary>
    public bool IsTraitInUse(Entity<PathologyHolderComponent?> entity, ProtoId<TraitPrototype> traitId)
    {
        if (!Resolve(entity.Owner, ref entity.Comp, logMissing: false))
            return false;

        return IsTraitInUse(entity.Comp, traitId);
    }

    /// <summary>
    /// Checks if it possible to apply in general
    /// </summary>
    /// <param name="level">Minimal level of the added pathology, see <see cref="TryAddPathology"/></param>
    public bool CanAddPathology(Entity<PathologyHolderComponent?> entity, ProtoId<PathologyPrototype> pathologyId, int level = 0)
    {
        if (!Resolve(entity.Owner, ref entity.Comp, logMissing: false))
            return false;

        if (!_prototype.Resolve(pathologyId, out var pathologyPrototype))
            return false;

        if (!entity.Comp.ActivePathologies.TryGetValue(pathologyId, out var instanceData))
            return true;

        if (GetAddedLevel(pathologyPrototype, instanceData, level) > instanceData.Level)
            return true;

        return instanceData.StackCount < pathologyPrototype.Definition[instanceData.Level].MaxStackCount;
    }

    public bool TryGetPathologyStack(Entity<PathologyHolderComponent?> entity, ProtoId<PathologyPrototype> pathologyId, [NotNullWhen(true)] out int? stackCount)
    {
        stackCount = null;
        if (!Resolve(entity.Owner, ref entity.Comp, logMissing: false))
            return false;

        if (!entity.Comp.ActivePathologies.TryGetValue(pathologyId, out var instanceData))
            return false;

        stackCount = instanceData.StackCount;
        return true;
    }

    public bool TryChangePathologyStack(Entity<PathologyHolderComponent?> entity, ProtoId<PathologyPrototype> pathologyId, int toAdd = 1, IPathologyContext? context = null)
    {
        if (!Resolve(entity.Owner, ref entity.Comp, logMissing: false))
            return false;

        if (!_prototype.Resolve(pathologyId, out var pathologyPrototype))
            return false;

        if (!entity.Comp.ActivePathologies.TryGetValue(pathologyId, out var instanceData))
            return false;

        var newStackCount = Math.Clamp(instanceData.StackCount + toAdd, OneStack - 1, pathologyPrototype.Definition[instanceData.Level].MaxStackCount);

        if (newStackCount == instanceData.StackCount)
            return false;

        if (newStackCount < OneStack)
            return TryRemovePathology(entity, pathologyId, checkStacks: false);

        var previousCount = instanceData.StackCount;
        instanceData.StackCount = newStackCount;

        if (newStackCount > previousCount)
        {
            for (var _ = 0; _ < newStackCount - previousCount; _++)
                instanceData.PathologyContexts.Add(context);

            foreach (var stackAddEffect in pathologyPrototype.Definition[instanceData.Level].AddStackEffects)
                stackAddEffect.ApplyEffect(entity, instanceData, EntityManager);
        }
        else
        {
            for (var _ = 0; _ < previousCount - newStackCount; _++)
                ApplyPathologyContext(entity!, instanceData.PathologyContexts.Pop());
        }

        var ev = new PathologyStackCountChanged(pathologyId, instanceData.Level, previousCount, newStackCount);
        RaiseLocalEvent(entity, ref ev);

        DebugTools.Assert(instanceData.PathologyContexts.Count == instanceData.StackCount);
        Dirty(entity);

        return true;
    }
}
