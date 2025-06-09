// Original code licensed under Imperial CLA.
// Copyright holders: orix0689 (discord) and pocchitsu (discord)
// See Content.Shared/SS220/Ninja/Objectives/LeaveNoTrace/license.txt

// Modified and/or redistributed under SS220 CLA with hosting restrictions.
// Full license text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Examine;
using Content.Shared.Ghost;
using Content.Shared.Objectives.Components;
using Content.Shared.SS220.Ninja.Objectives.LeaveNoTrace.Events;
using Content.Shared.Stealth.Components;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.Ninja.Objectives.LeaveNoTrace;

public abstract class SharedLeaveNoTraceSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ExamineSystemShared _examineSystem = default!;
    [Dependency] private readonly EntityLookupSystem _lookupSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LeaveNoTraceConditionComponent, ObjectiveAfterAssignEvent>(OnObjectiveAfterAssign);
        SubscribeLocalEvent<LeaveNoTraceConditionComponent, ObjectiveGetProgressEvent>(OnObjectiveGetProgress);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var enumerator = EntityQueryEnumerator<LeaveNoTraceComponent>();

        while (enumerator.MoveNext(out var uid, out var component))
        {
            if (_timing.CurTime <= component.NextVisibilityCheck)
                continue;

            if (component.RevealEndTime is { } endTime && endTime <= _timing.CurTime)
            {
                RemCompDeferred<LeaveNoTraceComponent>(uid);
                continue;
            }

            component.NextVisibilityCheck = _timing.CurTime + component.VisibilityCheckInterval;

            if (!NinjaIsVisible(uid))
            {
                HideNinja(uid, component);
                continue;
            }

            var ninjaPosition = Transform(uid).Coordinates;
            var nearbyPlayers = _lookupSystem.GetEntitiesInRange<ActorComponent>(ninjaPosition, component.Range, LookupFlags.Dynamic);
            var ninjaSeeingEntities = GetNinjaSeeingEntities(nearbyPlayers, uid);

            if (ninjaSeeingEntities.Count == 0)
            {
                HideNinja(uid, component);
                continue;
            }

            if (!component.IsSeen)
                RevealNinja(uid, component);

            component.IsSeen = true;
            component.WitnessEntities = ninjaSeeingEntities;
        }
    }

    #region Event Handlers

    private void OnObjectiveAfterAssign(Entity<LeaveNoTraceConditionComponent> ent, ref ObjectiveAfterAssignEvent args)
    {
        var ninja = args.Mind.OwnedEntity;

        if (ninja == null)
            return;

        EnsureComp<LeaveNoTraceComponent>(ninja.Value);
    }

    private void OnObjectiveGetProgress(Entity<LeaveNoTraceConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        var player = args.Mind.OwnedEntity;

        args.Progress = HasComp<LeaveNoTraceComponent>(player) ? 1f : 0f;
    }

    #endregion

    #region Helpers

    private bool NinjaIsVisible(EntityUid uid)
    {
        var ev = new NinjaHideAttemptEvent();
        RaiseLocalEvent(uid, ev);

        if (ev.Cancelled)
            return true;

        if (_containerSystem.IsEntityInContainer(uid))
            return false;

        if (TryComp<StealthComponent>(uid, out var stealthComponent) && stealthComponent.Enabled)
            return false;

        return true;
    }

    private bool EntitySeesNinja(EntityUid entity, EntityUid ninja)
    {
        if (entity == ninja)
            return false;

        if (HasComp<GhostComponent>(entity))
            return false;

        if (!_examineSystem.InRangeUnOccluded(entity, ninja))
            return false;

        var ev = new NinjaRevealedAttemptEvent(ninja, entity);
        RaiseLocalEvent(ninja, ev);
        RaiseLocalEvent(entity, ev);

        return !ev.Cancelled;
    }

    private HashSet<EntityUid> GetNinjaSeeingEntities(HashSet<Entity<ActorComponent>> nearbyPlayers, EntityUid ninja)
    {
        var entities = new HashSet<EntityUid>();

        foreach (var player in nearbyPlayers)
        {
            if (!EntitySeesNinja(player, ninja))
                continue;

            entities.Add(player.Owner);
        }

        return entities;
    }

    private void HideNinja(EntityUid ninja, LeaveNoTraceComponent component)
    {
        if (component.IsSeen)
        {
            var ev = new NinjaHideEvent();
            RaiseLocalEvent(ninja, ev);
        }

        component.IsSeen = false;
        component.RevealEndTime = null;
        component.WitnessEntities.Clear();
    }

    private void RevealNinja(EntityUid ninja, LeaveNoTraceComponent component)
    {
        component.RevealEndTime = _timing.CurTime + component.TimeForReveal;

        var ev = new NinjaRevealedEvent();
        RaiseLocalEvent(ninja, ev);
    }

    #endregion
};
