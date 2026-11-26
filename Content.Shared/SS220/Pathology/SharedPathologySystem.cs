// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Rejuvenate;
using Content.Shared.StatusEffectNew;
using Content.Shared.Traits;
using Content.Shared.Zombies;
using Robust.Shared.Collections;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.Pathology;

public abstract partial class SharedPathologySystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private MobStateSystem _mobState = default!;

    public static readonly int OneStack = 1;

    public static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(1f);

    private readonly List<ProtoId<PathologyPrototype>> _updateBuffer = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<PathologyHolderComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<ZombieComponent, PathologyAddedAttempt>(OnZombiePathologyAddAttempt);
        SubscribeLocalEvent<PathologyHolderComponent, EntityZombifiedEvent>(OnZombified);

        InitializeStatusEffectContainerEvents();
        InitializeExamineEvents();
        InitializeTraumaEffectsEvents();
        InitializeOrganEvents();
        InitializeTopicalCureEvents();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient || _gameTiming.Paused)
            return;

        var query = EntityQueryEnumerator<PathologyHolderComponent>();
        while (query.MoveNext(out var uid, out var holder))
        {
            if (holder.ActivePathologies.Count == 0)
                continue;

            if (holder.NextUpdate > _gameTiming.CurTime)
                continue;

            if (holder.NextUpdate == TimeSpan.Zero || _gameTiming.CurTime - holder.NextUpdate > UpdateInterval)
                holder.NextUpdate = _gameTiming.CurTime + UpdateInterval;
            else
                holder.NextUpdate += UpdateInterval;

            // effects can add or remove pathologies (damage triggers, death, zombification), so iterate over a snapshot
            _updateBuffer.Clear();
            _updateBuffer.AddRange(holder.ActivePathologies.Keys);

            foreach (var protoId in _updateBuffer)
            {
                if (!holder.ActivePathologies.TryGetValue(protoId, out var data))
                    continue;

                if (!_prototype.Resolve(protoId, out var pathologyProto))
                    continue;

                TryProgressPathology((uid, holder), pathologyProto, data, null);

                var decayChance = pathologyProto.Definition[data.Level].StackDecayChance;
                if (decayChance > 0f && _random.Prob(decayChance)
                    && TryChangePathologyStack((uid, holder), protoId, -1)
                    && !holder.ActivePathologies.ContainsKey(protoId))
                {
                    continue;
                }

                foreach (var effect in pathologyProto.Definition[data.Level].Effects)
                {
                    effect.ApplyEffect(uid, data, EntityManager);
                }
            }
        }
    }

    private void OnZombiePathologyAddAttempt(Entity<ZombieComponent> entity, ref PathologyAddedAttempt args)
    {
        args.Cancelled = true;
    }

    private void OnZombified(Entity<PathologyHolderComponent> entity, ref EntityZombifiedEvent args)
    {
        // zombies can't have pathologies at all, missing organs included
        RemoveAllPathologies(entity, force: true);
    }

    private void OnRejuvenate(Entity<PathologyHolderComponent> entity, ref RejuvenateEvent args)
    {
        RemoveAllPathologies(entity, force: false);
    }

    /// <param name="force">If true, removal attempts are not raised and nothing can keep a pathology</param>
    private void RemoveAllPathologies(Entity<PathologyHolderComponent> entity, bool force)
    {
        var pathologies = new ValueList<ProtoId<PathologyPrototype>>();
        foreach (var pathologyId in entity.Comp.ActivePathologies.Keys)
        {
            pathologies.Add(pathologyId);
        }

        foreach (var pathologyId in pathologies)
        {
            if (!force)
            {
                TryRemovePathology(entity!, pathologyId, checkStacks: false);
                continue;
            }

            if (_prototype.Resolve(pathologyId, out var pathologyPrototype))
                RemovePathology(entity, pathologyPrototype);
        }
    }

    private bool TryProgressPathology(Entity<PathologyHolderComponent> entity, PathologyPrototype pathologyPrototype, PathologyInstanceData instanceData, IPathologyContext? context)
    {
        if (pathologyPrototype.Definition[instanceData.Level].ProgressConditions.Length == 0)
            return false;

        foreach (var req in pathologyPrototype.Definition[instanceData.Level].ProgressConditions)
        {
            if (req.CheckCondition(entity, instanceData, _gameTiming.CurTime, EntityManager))
                continue;

            return false;
        }

        if (instanceData.Level + 1 >= pathologyPrototype.Definition.Length)
            return false;

        AdvanceLevel(entity, pathologyPrototype, instanceData, context, showPopup: true);
        return true;
    }

    /// <summary>
    /// Raises the pathology by one level, the caller checks that the next level exists.
    /// </summary>
    private void AdvanceLevel(Entity<PathologyHolderComponent> entity, PathologyPrototype pathologyPrototype, PathologyInstanceData instanceData, IPathologyContext? context, bool showPopup)
    {
        var preservedContext = context ?? GetFirstContext(instanceData);

        instanceData.Level++;
        instanceData.LevelStartTime = _gameTiming.CurTime;
        // we drop all previous
        instanceData.StackCount = OneStack;
        instanceData.PathologyContexts.Clear();

        instanceData.PathologyContexts.Add(preservedContext);
        AddPathologyDefinitionEffects(entity, pathologyPrototype.Definition[instanceData.Level], showPopup);
        DebugTools.Assert(instanceData.PathologyContexts.Count == instanceData.StackCount);

        var ev = new PathologySeverityChanged(pathologyPrototype.ID, instanceData.Level - 1, instanceData.Level);
        RaiseLocalEvent(entity, ref ev);

        Dirty(entity);
    }

    private static IPathologyContext? GetFirstContext(PathologyInstanceData instanceData)
    {
        foreach (var existingContext in instanceData.PathologyContexts)
        {
            if (existingContext != null)
                return existingContext;
        }

        return null;
    }

    private void StartPathology(Entity<PathologyHolderComponent> entity, PathologyPrototype pathologyPrototype, IPathologyContext? context)
    {
        var ev = new PathologyAddedEvent(pathologyPrototype.ID);
        RaiseLocalEvent(entity, ref ev);

        var instanceData = new PathologyInstanceData(_gameTiming.CurTime, context);
        entity.Comp.ActivePathologies.Add(pathologyPrototype.ID, instanceData);

        AddPathologyDefinitionEffects(entity, pathologyPrototype.Definition[0]);

        var severityChangedEv = new PathologySeverityChanged(pathologyPrototype.ID, -1, 0);
        RaiseLocalEvent(entity, ref severityChangedEv);

        var stackChangeEv = new PathologyStackCountChanged(pathologyPrototype.ID, instanceData.Level, 0, instanceData.StackCount);
        RaiseLocalEvent(entity, ref stackChangeEv);

        Dirty(entity);
    }

    private void AddPathologyDefinitionEffects(Entity<PathologyHolderComponent> entity, PathologyDefinition definition, bool showPopup = true)
    {
        AddTrait(entity, definition.Trait);

        // we don't want to popup dead ones
        if (!showPopup || _mobState.IsIncapacitated(entity))
            return;

        if (definition.ProgressPopup is { } progressPopup)
            _popup.PopupEntity(Loc.GetString(progressPopup), entity, entity, PopupType.MediumCaution);
    }

    /// <param name="applyContexts">False when the pathology is moved somewhere else (e.g. into an extracted organ) and keeps its contexts</param>
    private void RemovePathology(Entity<PathologyHolderComponent> entity, PathologyPrototype pathologyPrototype, bool applyContexts = true)
    {
        var data = entity.Comp.ActivePathologies[pathologyPrototype.ID];

        entity.Comp.ActivePathologies.Remove(pathologyPrototype.ID);

        RemoveDefinitionEffects(entity, pathologyPrototype, 0, data.Level);
        RefreshOverriddenStatusEffects(entity, pathologyPrototype);

        if (applyContexts)
        {
            foreach (var context in data.PathologyContexts)
            {
                ApplyPathologyContext(entity, context);
            }

            data.PathologyContexts.Clear();
        }

        Dirty(entity);

        var removedEv = new PathologyRemovedEvent(pathologyPrototype.ID);
        RaiseLocalEvent(entity, ref removedEv);
    }

    /// <summary>
    /// Removes status effects and traits of the given definition levels, unless another active pathology still uses them.
    /// Holder data must already reflect the change, so these levels are not counted as in use.
    /// </summary>
    private void RemoveDefinitionEffects(Entity<PathologyHolderComponent> entity, PathologyPrototype pathologyPrototype, int fromLevel, int toLevel)
    {
        for (var i = fromLevel; i <= toLevel; i++)
        {
            if (i >= pathologyPrototype.Definition.Length)
            {
                Log.Error($"Got level more than pathology definitions in {pathologyPrototype.ID} pathology!");
                break;
            }

            foreach (var effect in pathologyPrototype.Definition[i].StatusEffects)
            {
                if (!IsStatusEffectInUse(entity.Comp, effect))
                    _statusEffects.TryRemoveStatusEffect(entity, effect);
                else
                    UpdateStackableStatusEffect(entity, effect);
            }

            if (pathologyPrototype.Definition[i].Trait is { } traitId && !IsTraitInUse(entity.Comp, traitId))
                RemoveTrait(entity, traitId);
        }
    }

    private bool IsStatusEffectInUse(PathologyHolderComponent holder, EntProtoId effect)
    {
        foreach (var (otherId, otherData) in holder.ActivePathologies)
        {
            if (!_prototype.Resolve(otherId, out var otherProto) || IsStatusEffectOverridden(holder, otherId))
                continue;

            var maxLevel = Math.Min(otherData.Level, otherProto.Definition.Length - 1);
            for (var i = 0; i <= maxLevel; i++)
            {
                if (otherProto.Definition[i].StatusEffects.Contains(effect))
                    return true;
            }
        }

        return false;
    }

    private bool IsTraitInUse(PathologyHolderComponent holder, ProtoId<TraitPrototype> traitId)
    {
        foreach (var (otherId, otherData) in holder.ActivePathologies)
        {
            if (!_prototype.Resolve(otherId, out var otherProto))
                continue;

            var maxLevel = Math.Min(otherData.Level, otherProto.Definition.Length - 1);
            for (var i = 0; i <= maxLevel; i++)
            {
                if (otherProto.Definition[i].Trait == traitId)
                    return true;
            }
        }

        return false;
    }

    protected virtual void ApplyPathologyContext(Entity<PathologyHolderComponent> entity, IPathologyContext? context) { }

    // Kill it with TraitPrototype pls
    private void AddTrait(Entity<PathologyHolderComponent> entity, ProtoId<TraitPrototype>? traitId)
    {
        if (traitId is not { } id || !_prototype.Resolve(id, out var traitPrototype))
            return;

        if (traitPrototype.Components is null)
            return;

        if (entity.Comp.AppliedTraits.Contains(id))
            return;

        foreach (var entry in traitPrototype.Components.Values)
        {
            if (HasComp(entity.Owner, entry.Component.GetType()))
                return;
        }

        EntityManager.AddComponents(entity.Owner, traitPrototype.Components, false);
        entity.Comp.AppliedTraits.Add(id);
    }

    // and it
    private void RemoveTrait(Entity<PathologyHolderComponent> entity, ProtoId<TraitPrototype>? traitId)
    {
        if (traitId is not { } id || !_prototype.Resolve(id, out var traitPrototype))
            return;

        if (traitPrototype.Components is null)
            return;

        if (!entity.Comp.AppliedTraits.Remove(id))
            return;

        EntityManager.RemoveComponents(entity.Owner, traitPrototype.Components);
    }
}
