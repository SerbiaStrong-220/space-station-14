// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Bed.Components;
using Content.Shared.Body.Events;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Rejuvenate;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.Pathology;

public abstract partial class SharedPathologySystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private IComponentFactory _componentFactory = default!;

    public static readonly int OneStack = 1;
    public static readonly int DefaultMaxStack = 7;
    public static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(1f);

    private TimeSpan _lastUpdate;

    public override void Initialize()
    {
        SubscribeLocalEvent<PathologyHolderComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<PathologyHolderComponent, MobStateChangedEvent>(OnMobStateChanged);

        InitializeStatusEffectContainerEvents();
    }

    private void OnMobStateChanged(Entity<PathologyHolderComponent> ent, ref MobStateChangedEvent args)
    {
        if (_net.IsClient)
            return;

        if (args.NewMobState == MobState.Dead)
        {
            ent.Comp.DiedAt = _gameTiming.CurTime;
        }
        else if (args.OldMobState == MobState.Dead && ent.Comp.DiedAt is { } diedAt)
        {
            ent.Comp.DiedAt = null;
            ShiftPathologyTimers(ent, _gameTiming.CurTime - diedAt);
        }
    }

    // Shifts every active pathology's stage timer forward by the same delta, so time the pathology
    // shouldn't be progressing (spent dead, or on stasis bed) doesn't advance it.
    private void ShiftPathologyTimers(Entity<PathologyHolderComponent> ent, TimeSpan delta)
    {
        foreach (var data in ent.Comp.ActivePathologies.Values)
            data.StageStartTime += delta;

        Dirty(ent);
    }

    private void ApplyMetabolicSlowdown(Entity<PathologyHolderComponent> ent)
    {
        if (_net.IsClient)
            return;

        if (!HasComp<StasisBedBuckledComponent>(ent))
            return;

        var ev = new GetMetabolicMultiplierEvent();
        RaiseLocalEvent(ent.Owner, ref ev);

        if (ev.Multiplier <= 1f)
            return;

        var banked = UpdateInterval * (1d - 1d / ev.Multiplier);
        ShiftPathologyTimers(ent, banked);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_gameTiming.Paused)
            return;

        if (_gameTiming.CurTime < _lastUpdate)
            return;

        _lastUpdate = _gameTiming.CurTime + UpdateInterval;

        var query = EntityQueryEnumerator<PathologyHolderComponent>();
        while (query.MoveNext(out var uid, out var holder))
        {
            if (holder.ActivePathologies.Count == 0)
                continue;

            if (_mobState.IsDead(uid))
                continue;

            ApplyMetabolicSlowdown((uid, holder));

            foreach (var (protoId, data) in holder.ActivePathologies)
            {
                if (!_prototype.Resolve(protoId, out var pathologyProto))
                    continue;

                TryProgressPathology((uid, holder), pathologyProto, data);

                if (!_net.IsClient)
                {
                    var args = new PathologyEffectArgs(uid, data, EntityManager, _gameTiming.CurTime, _net.IsClient);
                    foreach (var effect in pathologyProto.Definition[data.Level].Effects)
                        effect.ApplyEffect(in args);
                }
            }
        }
    }

    private void OnRejuvenate(Entity<PathologyHolderComponent> entity, ref RejuvenateEvent args)
    {
        // snapshot keys — TryRemovePathology removes from ActivePathologies, so the live dict can't be iterated
        foreach (var pathologyId in new List<ProtoId<PathologyPrototype>>(entity.Comp.ActivePathologies.Keys))
        {
            TryRemovePathology(entity!, pathologyId, checkStacks: false);
        }

        Dirty(entity);
    }

    private bool TryProgressPathology(Entity<PathologyHolderComponent> entity, PathologyPrototype pathologyPrototype, PathologyInstanceData instanceData)
    {
        if (pathologyPrototype.Definition[instanceData.Level].ProgressConditions.Length == 0)
            return false;

        var args = new PathologyEffectArgs(entity, instanceData, EntityManager, _gameTiming.CurTime, _net.IsClient);
        foreach (var req in pathologyPrototype.Definition[instanceData.Level].ProgressConditions)
        {
            if (req.CheckCondition(in args))
                continue;

            return false;
        }

        return AdvancePathologyStage(entity, pathologyPrototype, instanceData);
    }

    private bool AdvancePathologyStage(Entity<PathologyHolderComponent> entity, PathologyPrototype pathologyPrototype, PathologyInstanceData instanceData, bool popup = true)
    {
        if (instanceData.Level + 1 >= pathologyPrototype.Definition.Length)
            return false;

        instanceData.Level++;
        var current = pathologyPrototype.Definition[instanceData.Level];
        instanceData.StageStartTime = _gameTiming.CurTime;
        AddPathologyDefinitionEffects(entity, instanceData, current, popup);
        DebugTools.Assert(instanceData.PathologyContexts.Count == instanceData.StackCount);

        var ev = new PathologySeverityChanged(pathologyPrototype.ID, instanceData.Level - 1, instanceData.Level);
        RaiseLocalEvent(entity, ref ev);

        Dirty(entity);
        return true;
    }

    private void StartPathology(Entity<PathologyHolderComponent> entity, PathologyPrototype pathologyPrototype, IPathologyContext? context)
    {
        var ev = new PathologyAddedEvent(pathologyPrototype.ID);
        RaiseLocalEvent(entity, ref ev);

        var instanceData = new PathologyInstanceData(_gameTiming.CurTime, context);
        entity.Comp.ActivePathologies.Add(pathologyPrototype.ID, instanceData);

        AddPathologyDefinitionEffects(entity, instanceData, pathologyPrototype.Definition[0]);

        var severityChangedEv = new PathologySeverityChanged(pathologyPrototype.ID, -1, 0);
        RaiseLocalEvent(entity, ref severityChangedEv);

        var stackChangeEv = new PathologyStackCountChanged(pathologyPrototype.ID, instanceData.Level, 0, instanceData.StackCount);
        RaiseLocalEvent(entity, ref stackChangeEv);

        Dirty(entity);
    }

    private void AddPathologyDefinitionEffects(Entity<PathologyHolderComponent> entity, PathologyInstanceData data, PathologyDefinition definition, bool popup = true)
    {
        AddTrackedComponents(entity, data, definition.Components);

        // we don't want to message dead ones, nor when silently restoring suppressed stages
        if (!popup || _mobState.IsIncapacitated(entity))
            return;

        // stage-progress feedback goes to the carrier's own chat, same channel as symptom self-messages
        if (definition.ProgressMessage is { } progressMessage)
            SendSelfMessage(entity, Loc.GetString(progressMessage), definition.ProgressMessageColor);
    }

    private void RemovePathology(Entity<PathologyHolderComponent> entity, PathologyPrototype pathologyPrototype)
    {
        var data = entity.Comp.ActivePathologies[pathologyPrototype.ID];

        foreach (var context in data.PathologyContexts)
            ApplyPathologyContext(entity, context);

        for (var i = 0; i <= data.Level; i++)
        {
            if (i >= pathologyPrototype.Definition.Length)
            {
                Log.Error($"Got level more than pathology definitions in {pathologyPrototype.ID} pathology!");
                break;
            }

            foreach (var effect in pathologyPrototype.Definition[i].StatusEffects)
            {
                _statusEffects.TryRemoveStatusEffect(entity, effect);
            }
        }

        foreach (var name in data.AddedComponents)
        {
            if (_componentFactory.TryGetRegistration(name, out var registration))
                RemComp(entity, registration.Type);
        }

        entity.Comp.ActivePathologies.Remove(pathologyPrototype.ID);

        Dirty(entity);
    }

    protected virtual void ApplyPathologyContext(Entity<PathologyHolderComponent> entity, IPathologyContext? context) { }

    protected virtual void SendSelfMessage(EntityUid entity, string message, Color? color) { }

    // we need to record what this symptom adds so a full cure strips exactly these
    private void AddTrackedComponents(Entity<PathologyHolderComponent> entity, PathologyInstanceData data, ComponentRegistry registry)
    {
        if (registry.Count == 0)
            return;

        foreach (var name in registry.Keys)
        {
            if (_componentFactory.TryGetRegistration(name, out var registration)
                && !HasComp(entity, registration.Type))
                data.AddedComponents.Add(name);
        }

        EntityManager.AddComponents(entity, registry, false);
    }
}
