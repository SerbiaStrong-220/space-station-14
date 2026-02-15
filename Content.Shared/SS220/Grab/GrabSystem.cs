using System.Numerics;
using Content.Shared.ActionBlocker;
using Content.Shared.Alert;
using Content.Shared.DoAfter;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Item;
using Content.Shared.MouseRotator;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.Grab;

// Baby steps for a bigger system to come
// This is a system separate from PullingSystem due to their different purposes: PullingSystem is meant just to pull things around and GrabSystem is designed for combat
// Current hacks:
// - The control flow comes from PullingSystem 'cuz of input handling
public sealed partial class GrabSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedVirtualItemSystem _virtualItem = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedJointSystem _joints = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GrabberComponent, GrabDoAfterEvent>(OnGrabDoAfter);
        SubscribeLocalEvent<GrabbableComponent, MoveInputEvent>(OnMove);
        SubscribeLocalEvent<GrabbableComponent, DownedEvent>(OnDowned);
        SubscribeLocalEvent<GrabbableComponent, ThrownEvent>(OnThrown);
        SubscribeLocalEvent<GrabberComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeed);

        SubscribeLocalEvent<GrabberComponent, EnableMouseRotationAttemptEvent>(OnMouseRotatorAttempt);
        SubscribeLocalEvent<GrabbableComponent, UpdateCanMoveEvent>(OnCanMove);
        SubscribeLocalEvent<GrabbableComponent, InteractionAttemptEvent>(OnInteractionAttempt);
        SubscribeLocalEvent<GrabbableComponent, AttackAttemptEvent>(OnCanAttack);

        SubscribeLocalEvent<GrabberComponent, AttemptMobTargetCollideEvent>(OnAttemptMobTargetCollide);
        SubscribeLocalEvent<GrabbableComponent, AttemptMobTargetCollideEvent>(OnAttemptMobTargetCollide);

        SubscribeLocalEvent<GrabberComponent, PickupAttemptEvent>(OnGrabberPickupAttempt);
        SubscribeLocalEvent<GrabbableComponent, PickupAttemptEvent>(OnGrabbablePickupAttempt);

        SubscribeLocalEvent<GrabberComponent, VirtualItemDeletedEvent>(OnVirtualItemDeleted);

        InitializeResistance();
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<GrabberComponent>();
        while (query.MoveNext(out var uid, out var grabber))
        {
            if (grabber.Grabbing is not { } grabbable)
                continue;

            if (!Exists(grabbable))
                continue;

            var grabberRot = _transform.GetWorldRotation(uid);
            _transform.SetWorldRotation(grabbable, grabberRot);
        }
    }

    #region Events Handling

    private void OnGrabDoAfter(Entity<GrabberComponent> grabber, ref GrabDoAfterEvent ev)
    {
        if (ev.Cancelled)
            return;

        if (ev.Target is not { } grabbable)
            return;

        if (!TryComp<GrabbableComponent>(grabbable, out var grabbableComp))
            return;

        _audio.PlayPredicted(grabber.Comp.GrabSound, grabber, grabber);

        if (grabbableComp.GrabStage == GrabStage.None)
        {
            DoInitialGrab((grabber, grabber.Comp), (grabbable, grabbableComp), GrabStage.Passive);
            return;
        }

        UpgradeGrab((grabber, grabber.Comp), (grabbable, grabbableComp));
    }

    private void OnMove(Entity<GrabbableComponent> grabbable, ref MoveInputEvent ev)
    {
        if (grabbable.Comp.GrabStage == GrabStage.Passive)
            TryBreakGrab((grabbable, grabbable.Comp));
    }

    private void OnThrown(Entity<GrabbableComponent> ent, ref ThrownEvent args)
    {
        if (!ent.Comp.Grabbed)
            return;

        BreakGrab((ent, ent.Comp));
    }

    private void OnRefreshMovementSpeed(Entity<GrabberComponent> grabber, ref RefreshMovementSpeedModifiersEvent ev)
    {
        if (grabber.Comp.Grabbing is not { } grabbing)
            return;

        if (!TryComp<GrabbableComponent>(grabbing, out var grabbableComp))
            return;

        if (!grabber.Comp.GrabStagesSpeedModifier.TryGetValue(grabbableComp.GrabStage, out var modifier))
            return;

        ev.ModifySpeed(modifier);
    }

    private void OnMouseRotatorAttempt(Entity<GrabberComponent> grabber, ref EnableMouseRotationAttemptEvent ev)
    {
        if (grabber.Comp.Grabbing != null)
            ev.Cancel();
    }

    private void OnCanMove(Entity<GrabbableComponent> grabbable, ref UpdateCanMoveEvent ev)
    {
        if (grabbable.Comp.GrabStage != GrabStage.None)
            ev.Cancel();
    }

    private void OnInteractionAttempt(Entity<GrabbableComponent> grabbable, ref InteractionAttemptEvent ev)
    {
        if (grabbable.Comp.GrabStage != GrabStage.None)
            ev.Cancelled = true;
    }

    private void OnCanAttack(Entity<GrabbableComponent> grabbable, ref AttackAttemptEvent ev)
    {
        if (grabbable.Comp.GrabStage != GrabStage.None)
            ev.Cancel();
    }

    // cuz of mob collisions
    private void OnAttemptMobTargetCollide(Entity<GrabberComponent> ent, ref AttemptMobTargetCollideEvent args)
    {
        if (ent.Comp.Grabbing == args.User)
            args.Cancelled = true;
    }

    private void OnAttemptMobTargetCollide(Entity<GrabbableComponent> ent, ref AttemptMobTargetCollideEvent args)
    {
        if (ent.Comp.GrabbedBy == args.User)
            args.Cancelled = true;
    }

    private void OnDowned(Entity<GrabbableComponent> grabbable, ref DownedEvent ev)
    {
        if (grabbable.Comp.GrabStage != GrabStage.None)
        {
            BreakGrab((grabbable, grabbable.Comp));
        }
    }

    private void OnVirtualItemDeleted(Entity<GrabberComponent> grabber, ref VirtualItemDeletedEvent ev)
    {
        if (!TryComp<GrabbableComponent>(ev.BlockingEntity, out var grabbable))
            return;

        if (grabbable.GrabStage == GrabStage.None)
            return;

        BreakGrab((ev.BlockingEntity, grabbable));
    }

    private void OnGrabberPickupAttempt(Entity<GrabberComponent> grabber, ref PickupAttemptEvent ev)
    {
        if (grabber.Comp.Grabbing != null)
            ev.Cancel();
    }

    private void OnGrabbablePickupAttempt(Entity<GrabbableComponent> grabbable, ref PickupAttemptEvent ev)
    {
        if (grabbable.Comp.Grabbed)
            ev.Cancel();
    }

    #endregion

    #region Public API
    public bool TryDoGrab(Entity<GrabberComponent?> grabber, Entity<GrabbableComponent?> grabbable)
    {
        // checks
        if (!Resolve(grabber, ref grabber.Comp))
            return false;
        if (!Resolve(grabbable, ref grabbable.Comp))
            return false;

        if (!CanGrab(grabber, grabbable, checkCanPull: false)) // the control flow comes from pulling system after pull checks
            return false;

        if (grabbable.Comp.GrabStage >= GrabStage.Last)
            return true;

        // popup
        var grabberMeta = MetaData(grabber);
        var grabbableMeta = MetaData(grabbable);

        var msg = grabbable.Comp.GrabStage == GrabStage.None
            ? Loc.GetString("grabber-component-new-grab-popup", ("grabber", grabberMeta.EntityName), ("grabbable", grabbableMeta.EntityName))
            : Loc.GetString("grabber-component-grab-upgrade-popup", ("grabber", grabberMeta.EntityName), ("grabbable", grabbableMeta.EntityName));

        _popup.PopupPredicted(msg, grabber, grabber);

        // get delay
        var nextStage = grabbable.Comp.GrabStage + 1;
        var delay = GetDelay((grabber.Owner, grabber.Comp), (grabbable.Owner, grabbable.Comp), nextStage);

        // do after
        var args = new DoAfterArgs(EntityManager, user: grabber, delay, new GrabDoAfterEvent(), eventTarget: grabber, target: grabbable)
        {
            BlockDuplicate = true,
            BreakOnDamage = true,
            BreakOnMove = true,
        };

        return _doAfter.TryStartDoAfter(args);
    }

    public bool CanGrab(Entity<GrabberComponent?> grabber, Entity<GrabbableComponent?> grabbable, bool checkCanPull = true)
    {
        if (!Resolve(grabber, ref grabber.Comp, false))
            return false;
        if (!Resolve(grabbable, ref grabbable.Comp, false))
            return false;

        if (grabbable.Comp.GrabbedBy != null && grabbable.Comp.GrabbedBy != grabber)
            return false;

        if (TryComp<GrabbableComponent>(grabber, out var grabberGrabbable) && grabberGrabbable.GrabbedBy != null)
            return false;

        if (!_interaction.InRangeAndAccessible(grabber.Owner, grabbable.Owner, grabber.Comp.Range))
            return false;

        if (checkCanPull)
            return _pulling.CanPull(grabber, grabbable, ignoreHands: true);

        return true;
    }

    public void ChangeGrabStage(Entity<GrabberComponent> grabber, Entity<GrabbableComponent> grabbable, GrabStage newStage)
    {
        grabbable.Comp.GrabStage = newStage;

        RefreshGrabResistance((grabbable, grabbable.Comp));
        _movementSpeed.RefreshMovementSpeedModifiers(grabber);
        UpdateAlerts(grabber, grabbable, newStage);
        _blocker.UpdateCanMove(grabbable);

        var ev = new GrabStageChangeEvent(grabber, grabbable, newStage);
        RaiseLocalEvent(grabber, ev);
    }

    public void BreakGrab(Entity<GrabbableComponent?> grabbable)
    {
        if (!Resolve(grabbable, ref grabbable.Comp))
            return;

        if (grabbable.Comp.GrabbedBy is not { } grabber)
            return;

        if (!TryComp<GrabberComponent>(grabber, out var grabberComp))
            return;

        ChangeGrabStage((grabber, grabberComp), (grabbable, grabbable.Comp), GrabStage.None);
        grabberComp.Grabbing = null;
        grabbable.Comp.GrabbedBy = null;

        if (grabbable.Comp.GrabJointId != null)
        {
            _joints.RemoveJoint(grabbable, grabbable.Comp.GrabJointId);
            grabbable.Comp.GrabJointId = null;
        }

        _popup.PopupPredicted(Loc.GetString("grabbable-component-break-free", ("grabbable", MetaData(grabbable).EntityName)), grabbable, grabbable);

        _virtualItem.DeleteInHandsMatching(grabber, grabbable);
        _virtualItem.DeleteInHandsMatching(grabbable, grabber);
    }

    #endregion

    #region Private API
    private void DoInitialGrab(Entity<GrabberComponent> grabber, Entity<GrabbableComponent> grabbable, GrabStage grabStage)
    {
        var freeHands = 0;

        foreach (var hand in _hands.EnumerateHands(grabber.Owner))
        {
            if (!_hands.TryGetHeldItem(grabber.Owner, hand, out var held))
            {
                freeHands++;
                continue;
            }

            if (HasComp<UnremoveableComponent>(held))
                continue;

            _hands.DoDrop(grabber.Owner, hand, true);
            freeHands++;

            if (freeHands == 2)
                break;
        }

        if (freeHands < 2)
        {
            _popup.PopupClient(Loc.GetString("grabber-component-no-free-hands"), grabber);
            return;
        }

        // grab confirmed

        _virtualItem.TrySpawnVirtualItemInHand(grabbable, grabber, out _);
        _virtualItem.TrySpawnVirtualItemInHand(grabbable, grabber, out _);

        if (_virtualItem.TrySpawnVirtualItemInHand(grabber, grabbable, out var virtualItem))
            EnsureComp<UnremoveableComponent>(virtualItem.Value);
        if (_virtualItem.TrySpawnVirtualItemInHand(grabber, grabbable, out var virtualItem2))
            EnsureComp<UnremoveableComponent>(virtualItem2.Value);

        grabber.Comp.Grabbing = grabbable;
        grabbable.Comp.GrabbedBy = grabber;

        // Position victim in front of grabber
        var grabberXform = Transform(grabber);
        var worldRot = _transform.GetWorldRotation(grabberXform);
        var worldPos = _transform.GetWorldPosition(grabberXform) + worldRot.RotateVec(grabber.Comp.GrabOffset);

        _transform.SetWorldPositionRotation(grabbable, worldPos, worldRot);

        // Create the joint
        grabbable.Comp.GrabJointId = $"grab_joint_{GetNetEntity(grabbable)}";
        var joint = _joints.CreatePrismaticJoint(grabbable, grabber, id: grabbable.Comp.GrabJointId);
        joint.CollideConnected = false;

        if (TryComp<PhysicsComponent>(grabbable, out var grabbablePhysics) && TryComp<PhysicsComponent>(grabber, out var grabberPhysics))
        {
            joint.LocalAnchorA = grabbablePhysics.LocalCenter;
            joint.LocalAnchorB = grabberPhysics.LocalCenter + grabber.Comp.GrabOffset;
        }
        else
        {
            joint.LocalAnchorA = Vector2.Zero;
            joint.LocalAnchorB = grabber.Comp.GrabOffset;
        }

        joint.ReferenceAngle = 0f;
        joint.EnableLimit = true;
        joint.LowerTranslation = 0f;
        joint.UpperTranslation = 0f;

        // grab initialized, update statuses
        ChangeGrabStage(grabber, grabbable, grabStage);
    }

    private void UpgradeGrab(Entity<GrabberComponent> grabber, Entity<GrabbableComponent> grabbable)
    {
        ChangeGrabStage(grabber, grabbable, grabbable.Comp.GrabStage + 1);
    }

    private void UpdateAlerts(Entity<GrabberComponent> grabber, Entity<GrabbableComponent> grabbable, GrabStage stage)
    {
        UpdateAlertsGrabber(grabber, stage);
        UpdateAlertsGrabbable(grabbable, stage);
    }

    private void UpdateAlertsGrabber(Entity<GrabberComponent> grabber, GrabStage stage)
    {
        if (stage == GrabStage.None && _alerts.IsShowingAlert(grabber.Owner, grabber.Comp.Alert))
        {
            _alerts.ClearAlert(grabber.Owner, grabber.Comp.Alert);
        }
        else if (stage != GrabStage.None)
        {
            var severity = (short)(stage - 1); // -1 cuz in alerts prototype zero stands for passive, none stage stands for no alert
            if (_alerts.IsShowingAlert(grabber.Owner, grabber.Comp.Alert))
                _alerts.UpdateAlert(grabber.Owner, grabber.Comp.Alert, severity);
            else
                _alerts.ShowAlert(grabber.Owner, grabber.Comp.Alert, severity);
        }
    }

    private void UpdateAlertsGrabbable(Entity<GrabbableComponent> grabbable, GrabStage stage)
    {
        if (stage == GrabStage.None && _alerts.IsShowingAlert(grabbable.Owner, grabbable.Comp.Alert))
        {
            _alerts.ClearAlert(grabbable.Owner, grabbable.Comp.Alert);
        }
        else if (stage != GrabStage.None)
        {
            var severity = (short)(stage - 1); // -1 cuz in alerts prototype zero stands for passive, none stage stands for no alert
            var cooldown = GetResistanceCooldown(grabbable.Owner);
            if (_alerts.IsShowingAlert(grabbable.Owner, grabbable.Comp.Alert))
            {
                var (_, cooldownEnd) = cooldown;
                _alerts.UpdateAlert(grabbable.Owner, grabbable.Comp.Alert, severity, cooldownEnd);
            }
            else
                _alerts.ShowAlert(grabbable.Owner, grabbable.Comp.Alert, severity, cooldown);
        }
    }

    private TimeSpan GetDelay(Entity<GrabberComponent> grabber, Entity<GrabbableComponent> grabbable, GrabStage nextStage)
    {
        var (uid, comp) = grabber;
        var delay = comp.FallbackGrabDelay;

        if (comp.GrabDelays.TryGetValue(nextStage, out var fetchedDelay))
        {
            delay = fetchedDelay;
        }

        var ev = new GrabDelayModifiersEvent(grabber, grabbable, nextStage, delay);
        RaiseLocalEvent(grabber, ev);

        return ev.Delay;
    }
    #endregion
}
