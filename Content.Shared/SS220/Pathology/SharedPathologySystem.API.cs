// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Shared.SS220.Pathology;

public abstract partial class SharedPathologySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private INetManager _net = default!;

    public Dictionary<ProtoId<PathologyPrototype>, PathologyDefinition> GetActivePathologies(Entity<PathologyHolderComponent?> entity)
    {
        if (!Resolve(entity.Owner, ref entity.Comp, false))
            return new();

        Dictionary<ProtoId<PathologyPrototype>, PathologyDefinition> result = new();
        foreach (var (pathologyId, instanceData) in entity.Comp.ActivePathologies)
        {
            if (!_prototype.Resolve(pathologyId, out var pathologyPrototype))
                continue;

            result.Add(pathologyId, pathologyPrototype.Definition[instanceData.Level]);
        }

        return result;
    }

    public int ForceAdvanceAllPathologies(Entity<PathologyHolderComponent?> entity)
    {
        if (!Resolve(entity.Owner, ref entity.Comp, false))
            return 0;

        var advanced = 0;
        foreach (var pathologyId in new List<ProtoId<PathologyPrototype>>(entity.Comp.ActivePathologies.Keys))
        {
            if (!entity.Comp.ActivePathologies.TryGetValue(pathologyId, out var data)
                || !_prototype.Resolve(pathologyId, out var proto))
                continue;

            if (AdvancePathologyStage((entity.Owner, entity.Comp), proto, data))
                advanced++;
        }

        return advanced;
    }

    public bool TryAddRandom(Entity<PathologyHolderComponent?> entity, ProtoId<WeightedRandomPrototype> weightedPathology, float chance, IPathologyContext? context = null)
    {
        if (!_prototype.Resolve(weightedPathology, out var weightedRandomPrototype))
            return false;

        return TryAddRandom(entity, weightedRandomPrototype.Weights, chance, context);
    }

    public bool TryAddRandom(Entity<PathologyHolderComponent?> entity, Dictionary<string, float> weightsPathology, float chance, IPathologyContext? context = null)
    {
        // no shared random, so we drop
        if (_net.IsClient)
            return false;

        if (!Resolve(entity.Owner, ref entity.Comp, false))
            return false;

        if (chance < 1f && !_random.Prob(chance))
            return false;

        var correctInput = weightsPathology.Where((entry) => _prototype.HasIndex<PathologyPrototype>(entry.Key) && CanAddPathology(entity, entry.Key)).ToDictionary();

        if (correctInput.Count == 0)
            return false;

        var pickedPathology = _random.Pick(correctInput);

        return TryAddPathology(entity, pickedPathology, context);
    }

    public bool TryAddPathology(Entity<PathologyHolderComponent?> entity, ProtoId<PathologyPrototype> pathologyId, IPathologyContext? context = null)
    {
        if (!Resolve(entity.Owner, ref entity.Comp, false))
            return false;

        if (!_prototype.Resolve(pathologyId, out var pathologyPrototype))
            return false;

        entity.Comp.ActivePathologies.TryGetValue(pathologyId, out var instanceData);

        if (instanceData is not null && instanceData.StackCount > pathologyPrototype.Definition[instanceData.Level].MaxStackCount)
            return false;

        var attemptEv = new PathologyAddedAttempt(pathologyId);
        RaiseLocalEvent(entity, ref attemptEv);

        if (attemptEv.Cancelled)
            return false;

        if (instanceData is null)
            StartPathology(entity!, pathologyPrototype, context);
        else
            TryChangePathologyStack(entity, pathologyPrototype, context: context);

        return true;
    }

    public bool TryRemovePathology(Entity<PathologyHolderComponent?> entity, ProtoId<PathologyPrototype> pathologyId, bool checkStacks = true)
    {
        if (!Resolve(entity.Owner, ref entity.Comp, logMissing: false))
            return false;

        if (!_prototype.Resolve(pathologyId, out var pathologyPrototype))
            return false;

        // we actually removed so true, anyQ?
        if (!entity.Comp.ActivePathologies.TryGetValue(pathologyId, out var instanceData))
            return true;

        if (checkStacks && instanceData.StackCount > OneStack)
            return false;

        var ev = new PathologyRemoveAttempt(pathologyId, instanceData.Level);
        RaiseLocalEvent(entity, ref ev);
        if (ev.Cancelled)
            return false;

        RemovePathology(entity!, pathologyPrototype);
        return true;
    }

    public bool HavePathology(Entity<PathologyHolderComponent?> entity, ProtoId<PathologyPrototype> pathologyId)
    {
        if (!Resolve(entity.Owner, ref entity.Comp))
            return false;

        return entity.Comp.ActivePathologies.ContainsKey(pathologyId);
    }

    /// <summary>
    /// Checks if it possible to apply in general
    /// </summary>
    public bool CanAddPathology(Entity<PathologyHolderComponent?> entity, ProtoId<PathologyPrototype> pathologyId)
    {
        if (!Resolve(entity.Owner, ref entity.Comp))
            return false;

        if (!_prototype.Resolve(pathologyId, out var pathologyPrototype))
            return false;

        if (!entity.Comp.ActivePathologies.TryGetValue(pathologyId, out var instanceData))
            return true;

        return pathologyPrototype.Definition[instanceData.Level].MaxStackCount > OneStack;
    }

    public bool TryGetPathologyStack(Entity<PathologyHolderComponent?> entity, ProtoId<PathologyPrototype> pathologyId, [NotNullWhen(true)] out int? stackCount)
    {
        stackCount = null;
        if (!Resolve(entity.Owner, ref entity.Comp))
            return false;

        if (!entity.Comp.ActivePathologies.TryGetValue(pathologyId, out var instanceData))
            return false;

        stackCount = instanceData.StackCount;
        return true;
    }

    public bool TryChangePathologyStack(Entity<PathologyHolderComponent?> entity, ProtoId<PathologyPrototype> pathologyId, int toAdd = 1, IPathologyContext? context = null)
    {
        if (!Resolve(entity.Owner, ref entity.Comp))
            return false;

        if (!_prototype.Resolve(pathologyId, out var pathologyPrototype))
            return false;

        if (!entity.Comp.ActivePathologies.TryGetValue(pathologyId, out var instanceData))
            return false;

        var newStackCount = Math.Clamp(instanceData.StackCount + toAdd, OneStack - 1, pathologyPrototype.Definition[instanceData.Level].MaxStackCount);

        if (newStackCount == instanceData.StackCount)
            return false;

        if (newStackCount > instanceData.StackCount)
        {
            for (var _ = 0; _ < newStackCount - instanceData.StackCount; _++)
                instanceData.PathologyContexts.Add(context);

            var args = new PathologyEffectArgs(entity.Owner, instanceData, EntityManager, _gameTiming.CurTime, _net.IsClient);
            foreach (var stackAddEffect in pathologyPrototype.Definition[instanceData.Level].AddStackEffects)
                stackAddEffect.ApplyEffect(in args);
        }
        else
        {
            for (var _ = 0; _ < instanceData.StackCount - newStackCount; _++)
                ApplyPathologyContext(entity!, instanceData.PathologyContexts.Pop());
        }

        var ev = new PathologyStackCountChanged(pathologyId, instanceData.Level, instanceData.StackCount, newStackCount);
        RaiseLocalEvent(entity, ref ev);

        instanceData.StackCount = newStackCount;

        DebugTools.Assert(instanceData.PathologyContexts.Count == instanceData.StackCount);
        Dirty(entity);

        if (instanceData.StackCount == OneStack - 1)
            return TryRemovePathology(entity, pathologyId);

        return true;
    }
}
