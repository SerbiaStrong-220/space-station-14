// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Shared.SS220.Pathology;

public sealed partial class PathologySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly INetManager _net = default!;


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

    public bool TryAddRandom(Entity<PathologyHolderComponent?> entity, ProtoId<WeightedRandomPrototype> weightedPathology, float chance)
    {
        if (!_prototype.Resolve(weightedPathology, out var weightedRandomPrototype))
            return false;

        return TryAddRandom(entity, weightedRandomPrototype.Weights, chance);
    }

    public bool TryAddRandom(Entity<PathologyHolderComponent?> entity, Dictionary<string, float> weightsPathology, float chance)
    {
        // no shared random, so we drop
        if (_net.IsClient)
            return false;

        if (!Resolve(entity.Owner, ref entity.Comp, false))
            return false;

        if (chance < 1f && !_random.Prob(chance))
            return false;

        var correctInput = weightsPathology.Where((entry) => _prototype.HasIndex<PathologyPrototype>(entry.Key) && !HavePathology(entity, entry.Key)).ToDictionary();

        if (correctInput.Count == 0)
            return false;

        var pickedPathology = _random.Pick(correctInput);

        return TryAddPathology(entity, pickedPathology);
    }

    public bool TryAddPathology(Entity<PathologyHolderComponent?> entity, ProtoId<PathologyPrototype> pathologyId)
    {
        if (!Resolve(entity.Owner, ref entity.Comp, false))
            return false;

        if (entity.Comp.ActivePathologies.ContainsKey(pathologyId))
            return false;

        if (!_prototype.Resolve(pathologyId, out var pathologyPrototype))
            return false;

        var attemptEv = new PathologyAddedAttempt(pathologyId);
        RaiseLocalEvent(entity, ref attemptEv);

        if (attemptEv.Cancelled)
            return false;

        AddPathology(entity!, pathologyPrototype);
        return true;
    }

    public bool TryRemovePathology(Entity<PathologyHolderComponent?> entity, ProtoId<PathologyPrototype> pathologyId)
    {
        if (!Resolve(entity.Owner, ref entity.Comp, logMissing: false))
            return false;

        if (!_prototype.Resolve(pathologyId, out var pathologyPrototype))
            return false;

        // we actually removed so true, anyQ?
        if (!entity.Comp.ActivePathologies.TryGetValue(pathologyId, out var instanceData))
            return true;

        if (instanceData.StackCount > OneStack)
            return false;

        var ev = new PathologyRemoveAttempt(pathologyId, instanceData.Level);
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

    public bool TryChangePathologyStack(Entity<PathologyHolderComponent?> entity, ProtoId<PathologyPrototype> pathologyId, int toAdd = 1)
    {
        if (!Resolve(entity.Owner, ref entity.Comp))
            return false;

        if (!_prototype.Resolve(pathologyId, out var pathologyPrototype))
            return false;

        if (!entity.Comp.ActivePathologies.TryGetValue(pathologyId, out var instanceData))
            return false;

        var newStackCount = Math.Clamp(instanceData.StackCount + toAdd, OneStack, pathologyPrototype.Definition[instanceData.Level].MaxStackCount);

        if (newStackCount == instanceData.StackCount)
            return false;

        if (newStackCount > instanceData.StackCount)
        {
            foreach (var stackAddEffect in pathologyPrototype.Definition[instanceData.Level].AddStackEffects)
            {
                stackAddEffect.ApplyEffect(entity, instanceData, EntityManager);
            }
        }

        var ev = new PathologyStackCountChanged(pathologyId, instanceData.StackCount, newStackCount);
        RaiseLocalEvent(entity, ref ev);

        instanceData.StackCount = newStackCount;
        Dirty(entity);
        return true;
    }
}
