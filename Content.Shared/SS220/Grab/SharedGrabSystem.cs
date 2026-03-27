// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Numerics;
using Content.Shared.ActionBlocker;
using Content.Shared.Alert;
using Content.Shared.DoAfter;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
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
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.Grab;

// Baby steps for a bigger system to come
// This is a system separate from PullingSystem due to their different purposes: PullingSystem is meant just to pull things around and GrabSystem is designed for combat
// Current hacks:
// - The control flow comes from PullingSystem 'cuz of input handling
public abstract partial class SharedGrabSystem : EntitySystem
{
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedVirtualItemSystem _virtualItem = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedJointSystem _joints = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;

    protected EntityQuery<GrabbableComponent> _grabbableQuery;
    private EntityQuery<GrabberComponent> _grabberQuery;
    private EntityQuery<PhysicsComponent> _physicsQuery;

    public override void Initialize()
    {
        base.Initialize();
        UpdatesAfter.Add(typeof(SharedMouseRotatorSystem));

        SubscribeLocalEvent<GrabberComponent, GrabDoAfterEvent>(OnGrabDoAfter);
        SubscribeLocalEvent<GrabbableComponent, MoveInputEvent>(OnMove);
        SubscribeLocalEvent<GrabbableComponent, DownedEvent>(OnDowned);
        SubscribeLocalEvent<GrabbableComponent, ThrownEvent>(OnThrown);
        SubscribeLocalEvent<GrabberComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeed);
        SubscribeLocalEvent<GrabbableComponent, ComponentShutdown>(OnGrabbableShutdown);
        SubscribeLocalEvent<GrabberComponent, ComponentShutdown>(OnGrabberShutdown);

        SubscribeLocalEvent<GrabberComponent, EnableMouseRotationAttemptEvent>(OnMouseRotatorAttempt);
        SubscribeLocalEvent<GrabbableComponent, UpdateCanMoveEvent>(OnCanMove);
        SubscribeLocalEvent<GrabbableComponent, InteractionAttemptEvent>(OnInteractionAttempt);
        SubscribeLocalEvent<GrabbableComponent, DownAttemptEvent>(OnDownAttempt);
        SubscribeLocalEvent<GrabbableComponent, AttackAttemptEvent>(OnCanAttack);

        SubscribeLocalEvent<GrabberComponent, AttemptMobTargetCollideEvent>(OnAttemptMobTargetCollide);
        SubscribeLocalEvent<GrabbableComponent, AttemptMobTargetCollideEvent>(OnAttemptMobTargetCollide);

        SubscribeLocalEvent<GrabberComponent, PickupAttemptEvent>(OnGrabberPickupAttempt);
        SubscribeLocalEvent<GrabbableComponent, PickupAttemptEvent>(OnGrabbablePickupAttempt);

        SubscribeLocalEvent<GrabberComponent, VirtualItemDeletedEvent>(OnVirtualItemDeleted);

        InitializeResistance();

        _grabbableQuery = GetEntityQuery<GrabbableComponent>();
        _grabberQuery = GetEntityQuery<GrabberComponent>();
        _physicsQuery = GetEntityQuery<PhysicsComponent>();
    }

    #region Events Handling

    private void OnGrabDoAfter(Entity<GrabberComponent> grabber, ref GrabDoAfterEvent ev)
    {
        if (ev.Cancelled)
            return;

        if (ev.Target is not { } grabbable)
            return;

        if (!_grabbableQuery.TryComp(grabbable, out var grabbableComp))
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

        if (!_grabbableQuery.TryComp(grabbing, out var grabbableComp))
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
        if (IsGrabbed((grabbable, grabbable.Comp)))
            ev.Cancel();
    }

    private void OnInteractionAttempt(Entity<GrabbableComponent> grabbable, ref InteractionAttemptEvent ev)
    {
        if (IsGrabbed((grabbable, grabbable.Comp)))
            ev.Cancelled = true;
    }

    private void OnDownAttempt(Entity<GrabbableComponent> grabbable, ref DownAttemptEvent ev)
    {
        if (IsGrabbed((grabbable, grabbable.Comp)))
            ev.Cancel();
    }

    private void OnCanAttack(Entity<GrabbableComponent> grabbable, ref AttackAttemptEvent ev)
    {
        if (IsGrabbed((grabbable, grabbable.Comp)))
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
        if (!IsGrabbed((grabbable, grabbable.Comp)))
            BreakGrab((grabbable, grabbable.Comp));
    }

    private void OnVirtualItemDeleted(Entity<GrabberComponent> grabber, ref VirtualItemDeletedEvent ev)
    {
        if (!_grabbableQuery.TryComp(ev.BlockingEntity, out var grabbable))
            return;

        if (!IsGrabbed((ev.BlockingEntity, grabbable)))
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

    private void OnGrabbableShutdown(Entity<GrabbableComponent> grabbable, ref ComponentShutdown ev)
    {
        var comp = grabbable.Comp;

        if (!comp.Grabbed)
            return;

        var grabber = comp.GrabbedBy.Value;
        _grabberQuery.TryComp(grabber, out var grabberComp);

        ClearJoints((grabber, grabberComp), (grabbable, grabbable.Comp));
    }

    private void OnGrabberShutdown(Entity<GrabberComponent> grabber, ref ComponentShutdown ev)
    {
        var comp = grabber.Comp;

        if (comp.Grabbing is not { } grabbable)
            return;

        _grabbableQuery.TryComp(grabbable, out var grabbableComp);

        ClearJoints((grabber, grabber.Comp), (grabbable, grabbableComp));
    }

    #endregion

    #region Public API
    public bool TryDoGrab(Entity<GrabberComponent?> grabber, Entity<GrabbableComponent?> grabbable)
    {
        // checks
        if (!_grabberQuery.Resolve(grabber, ref grabber.Comp))
            return false;

        if (!_grabbableQuery.Resolve(grabbable, ref grabbable.Comp))
            return false;

        if (!CanGrab(grabber, grabbable, checkCanPull: false)) // the control flow comes from pulling system after pull checks
            return false;

        if (grabbable.Comp.GrabStage >= GrabStage.Last)
            return true;

        // popup
        var grabberName = Identity.Name(grabber, EntityManager);
        var grabbableName = Identity.Name(grabbable, EntityManager);

        var msg = grabbable.Comp.GrabStage == GrabStage.None
            ? Loc.GetString(grabber.Comp.NewGrabPopup, ("grabber", grabberName), ("grabbable", grabbableName))
            : Loc.GetString(grabber.Comp.GrabUpgradePopup, ("grabber", grabberName), ("grabbable", grabbableName));

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
            DistanceThreshold = 2f
        };

        return _doAfter.TryStartDoAfter(args);
    }

    public bool CanGrab(Entity<GrabberComponent?> grabber, Entity<GrabbableComponent?> grabbable, bool checkCanPull = true)
    {
        if (!_grabberQuery.Resolve(grabber, ref grabber.Comp, false))
            return false;

        if (!_grabbableQuery.Resolve(grabbable, ref grabbable.Comp, false))
            return false;

        if (grabbable.Comp.GrabbedBy != null && grabbable.Comp.GrabbedBy != grabber)
            return false;

        if (_grabbableQuery.TryComp(grabber, out var grabberGrabbable) && grabberGrabbable.GrabbedBy != null)
            return false;

        if (!_interaction.InRangeAndAccessible(grabber.Owner, grabbable.Owner, grabber.Comp.Range))
            return false;

        if (checkCanPull)
            return _pulling.CanPull(grabber, grabbable, ignoreHands: true);

        return true;
    }

    public void ChangeGrabStage(Entity<GrabberComponent> grabber, Entity<GrabbableComponent> grabbable, GrabStage newStage)
    {
        var oldStage = grabbable.Comp.GrabStage;
        grabbable.Comp.GrabStage = newStage;
        Dirty(grabbable);

        RefreshGrabResistance((grabbable, grabbable.Comp));

        var ev = new GrabStageChangeEvent(grabber, grabbable, oldStage, newStage); // all fields are readonly so using for both entities should be ok
        RaiseLocalEvent(grabber, ev);
        RaiseLocalEvent(grabbable, ev);

        _movementSpeed.RefreshMovementSpeedModifiers(grabber);
        _blocker.UpdateCanMove(grabbable);
        UpdateAlerts(grabber, grabbable, newStage);
    }

    public void BreakGrab(Entity<GrabbableComponent?> grabbable)
    {
        if (!_grabbableQuery.Resolve(grabbable, ref grabbable.Comp))
            return;

        if (grabbable.Comp.GrabbedBy is not { } grabber)
            return;

        if (!_grabberQuery.TryComp(grabber, out var grabberComp))
            return;

        ChangeGrabStage((grabber, grabberComp), (grabbable, grabbable.Comp), GrabStage.None);

        grabberComp.Grabbing = null;
        Dirty(grabber, grabberComp);

        grabbable.Comp.GrabbedBy = null;
        Dirty(grabbable);

        ClearJoints((grabber, grabberComp), grabbable);

        _popup.PopupPredicted(Loc.GetString(grabbable.Comp.BreakFreePopup, ("grabbable", MetaData(grabbable).EntityName)), grabbable, null); // cannot be predicted but if somehow BreakGrab will be called at the client side we'll ensure nothing wrong happens

        _virtualItem.DeleteInHandsMatching(grabber, grabbable);
        _virtualItem.DeleteInHandsMatching(grabbable, grabber);
    }

    public bool IsGrabbed(Entity<GrabbableComponent?> grabbable)
    {
        if (!_grabbableQuery.Resolve(grabbable, ref grabbable.Comp, false))
            return false;

        return grabbable.Comp.GrabStage != GrabStage.None;
    }

    public void DoInitialGrab(Entity<GrabberComponent> grabber, Entity<GrabbableComponent> grabbable, GrabStage grabStage)
    {
        if (IsGrabbed((grabbable, grabbable.Comp)))
            return;

        foreach (var hand in _hands.EnumerateHands(grabber.Owner))
            _hands.TryDrop(grabber.Owner, hand);

        var freeHands = _hands.GetEmptyHandCount(grabber.Owner);

        if (freeHands < grabber.Comp.NeededHands)
        {
            _popup.PopupClient(Loc.GetString(grabber.Comp.NoFreeHandsPopup), grabber);
            return;
        }

        // grab confirmed

        for (var i = 0; i < grabber.Comp.NeededHands; i++)
        {
            _virtualItem.TrySpawnVirtualItemInHand(grabbable, grabber, out _);

            if (_virtualItem.TrySpawnVirtualItemInHand(grabber, grabbable, out var virtualItem))
                EnsureComp<UnremoveableComponent>(virtualItem.Value);
        }

        grabber.Comp.Grabbing = grabbable;
        grabbable.Comp.GrabbedBy = grabber;

        PlaceGrabbable(grabber, grabbable);

        // Create the joint
        var jointId = $"grab_joint_{GetNetEntity(grabbable)}";
        grabber.Comp.GrabJointId = jointId;
        grabbable.Comp.GrabJointId = jointId;

        var joint = _joints.CreatePrismaticJoint(grabbable, grabber, id: grabbable.Comp.GrabJointId);
        joint.CollideConnected = false;

        if (_physicsQuery.TryComp(grabbable, out var grabbablePhysics) && _physicsQuery.TryComp(grabber, out var grabberPhysics))
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

        Dirty(grabbable);
        Dirty(grabber);

        // grab initialized, update statuses
        ChangeGrabStage(grabber, grabbable, grabStage);
    }

    #endregion

    #region Private API
    /// <summary>
    /// Position victim in front of grabber
    /// </summary>
    protected void PlaceGrabbable(Entity<GrabberComponent> grabber, Entity<GrabbableComponent> grabbable)
    {

        var grabberXform = Transform(grabber);
        var worldRot = _transform.GetWorldRotation(grabberXform);
        var worldPos = _transform.GetWorldPosition(grabberXform) + worldRot.RotateVec(grabber.Comp.GrabOffset);

        _transform.SetWorldPositionRotation(grabbable, worldPos, worldRot);
    }

    private void UpgradeGrab(Entity<GrabberComponent> grabber, Entity<GrabbableComponent> grabbable)
    {
        ChangeGrabStage(grabber, grabbable, grabbable.Comp.GrabStage + 1);
    }

    private void UpdateAlerts(Entity<GrabberComponent> grabber, Entity<GrabbableComponent> grabbable, GrabStage stage)
    {
        UpdateAlertFor(grabber, grabber.Comp.Alert, stage, false);
        UpdateAlertFor(grabbable, grabbable.Comp.Alert, stage, true);
    }

    private void UpdateAlertFor(EntityUid uid, ProtoId<AlertPrototype> alert, GrabStage stage, bool showResistanceCooldown)
    {
        if (stage == GrabStage.None && _alerts.IsShowingAlert(uid, alert))
        {
            _alerts.ClearAlert(uid, alert);
        }
        else if (stage != GrabStage.None)
        {
            var severity = (short)stage;
            var cooldown = GetResistanceStartEndTime(uid);
            if (_alerts.IsShowingAlert(uid, alert))
            {
                var (_, cooldownEnd) = cooldown;
                _alerts.UpdateAlert(uid, alert, severity, showResistanceCooldown ? cooldownEnd : null);
            }
            else
                _alerts.ShowAlert(uid, alert, severity, showResistanceCooldown ? cooldown : null);
        }
    }

    private TimeSpan GetDelay(Entity<GrabberComponent> grabber, Entity<GrabbableComponent> grabbable, GrabStage nextStage)
    {
        var delay = grabber.Comp.FallbackGrabDelay;

        if (grabber.Comp.GrabDelays.TryGetValue(nextStage, out var fetchedDelay))
        {
            delay = fetchedDelay;
        }

        var ev = new GrabDelayModifiersEvent(grabber, grabbable, nextStage, delay);
        RaiseLocalEvent(grabber, ev);

        return ev.Delay;
    }

    /// <summary>
    /// NOTE: Doesn't resolves null components on entities
    /// </summary>
    private void ClearJoints(Entity<GrabberComponent?> grabber, Entity<GrabbableComponent?> grabbable)
    {
        if (grabbable.Comp?.GrabJointId != null)
        {
            DebugTools.Assert(grabbable.Comp.GrabJointId == grabber.Comp?.GrabJointId);

            _joints.RemoveJoint(grabbable, grabbable.Comp.GrabJointId);
            grabbable.Comp.GrabJointId = null;

            if (grabber.Comp != null)
                grabber.Comp.GrabJointId = null;
        }

        if (grabber.Comp?.GrabJointId != null) // in most cases joint should be cleaned up in block above, but to ensure nothing is broken
        {
            _joints.RemoveJoint(grabber, grabber.Comp.GrabJointId);
            grabber.Comp.GrabJointId = null;
        }

        Dirty(grabbable);
        Dirty(grabber);
    }
    #endregion
}
