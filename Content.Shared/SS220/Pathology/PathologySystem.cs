// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.Pathology;

public sealed class PathologySystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public static int OneStack = 1;

    private readonly TimeSpan _updateInterval = TimeSpan.FromSeconds(1f);

    private TimeSpan _lastUpdate;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_gameTiming.Paused)
            return;

        if (_gameTiming.CurTime > _lastUpdate)
            return;

        _lastUpdate = _gameTiming.CurTime + _updateInterval;

        var query = EntityQueryEnumerator<PathologyHolderComponent>();
        while (query.MoveNext(out var uid, out var holder))
        {
            if (holder.ActivePathologies.Count == 0)
                continue;

            foreach (var (protoId, data) in holder.ActivePathologies)
            {
                if (!_prototype.Resolve(protoId, out var pathologyProto))
                    continue;

                if (!TryProgressPathology((uid, holder), pathologyProto, data))
                    continue;

                foreach (var effect in pathologyProto.Definition[data.Level].Effects)
                {
                    effect.ApplyEffect(uid, data, EntityManager);
                }
            }
        }
    }

    private bool TryProgressPathology(Entity<PathologyHolderComponent> entity, PathologyPrototype pathologyPrototype, PathologyInstanceData instanceData)
    {
        foreach (var req in pathologyPrototype.Definition[instanceData.Level].ProgressConditions)
        {
            if (req.CheckCondition(entity, instanceData, EntityManager))
                continue;

            return false;
        }

        if (instanceData.Level + 1 >= pathologyPrototype.Definition.Length)
            return false;

        instanceData.Level++;

        EntityManager.AddComponents(entity, pathologyPrototype.Definition[instanceData.Level].Components);

        var ev = new PathologySeverityChanged(pathologyPrototype.ID, instanceData.Level - 1, instanceData.Level);
        RaiseLocalEvent(entity, ref ev);

        return true;
    }

    public bool TryAddPathology(Entity<PathologyHolderComponent?> entity, ProtoId<PathologyPrototype> pathologyId)
    {
        if (!Resolve(entity.Owner, ref entity.Comp, false))
            return false;

        if (entity.Comp.ActivePathologies.ContainsKey(pathologyId))
            return false;

        if (!_prototype.Resolve(pathologyId, out var proto))
            return false;

        var attemptEv = new PathologyAddedAttempt(pathologyId);
        RaiseLocalEvent(entity, ref attemptEv);

        if (attemptEv.Cancelled)
            return false;

        var ev = new PathologyAddedEvent(pathologyId);
        RaiseLocalEvent(entity, ref ev);

        entity.Comp.ActivePathologies.Add(pathologyId, new PathologyInstanceData(_gameTiming.CurTime));
        Dirty(entity);

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

    private void RemovePathology(Entity<PathologyHolderComponent> entity, PathologyPrototype pathologyPrototype)
    {
        for (var i = 0; i < entity.Comp.ActivePathologies[pathologyPrototype.ID].Level; i++)
        {
            if (i >= pathologyPrototype.Definition.Length)
            {
                Log.Error($"Got level more than pathology definitions in {pathologyPrototype.ID} pathology!");
                break;
            }

            EntityManager.RemoveComponents(entity, pathologyPrototype.Definition[i].Components);
        }

        entity.Comp.ActivePathologies.Remove(pathologyPrototype.ID);

        Dirty(entity);
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

        instanceData.StackCount = newStackCount;
        return true;
    }
}
