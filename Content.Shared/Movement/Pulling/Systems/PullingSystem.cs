using System.Numerics;
using Content.Shared.ActionBlocker;
using Content.Shared.Administration.Logs;
using Content.Shared.Alert;
using Content.Shared.Buckle.Components;
using Content.Shared.CombatMode;
using Content.Shared.Cuffs;
using Content.Shared.Cuffs.Components;
using Content.Shared.Database;
using Content.Shared.Effects;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Input;
using Content.Shared.Interaction;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Item;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Pulling.Events;
using Content.Shared.SS220.Cart.Components;
using Content.Shared.Standing;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Input.Binding;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Movement.Pulling.Systems;

/// <summary>
/// Allows one entity to pull another behind them via a physics distance joint.
/// </summary>
public sealed class PullingSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!; // SS220-MIT-pull-visualization
    [Dependency] private readonly RotateToFaceSystem _rotateTo = default!; // SS220-MIT-pull-visualization
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly AlertsSystem _alertsSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!; // SS220-MIT-pull-visualization
    [Dependency] private readonly SharedMeleeWeaponSystem _melee = default!; // SS220-MIT-pull-visualization
    [Dependency] private readonly MovementSpeedModifierSystem _modifierSystem = default!;
    [Dependency] private readonly SharedJointSystem _joints = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _colorFlash = default!; // SS220-MIT-pull-visualization
    [Dependency] private readonly SharedCombatModeSystem _combatMode = default!; // SS220-MIT-pull-visualization
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly HeldSpeedModifierSystem _clothingMoveSpeed = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedVirtualItemSystem _virtual = default!;

    static readonly Color СolorCaptureEffect = Color.Yellow; // SS220-MIT-pull-visualization

    public override void Initialize()
    {
        base.Initialize();

        UpdatesAfter.Add(typeof(SharedPhysicsSystem));
        UpdatesOutsidePrediction = true;

        SubscribeLocalEvent<PullableComponent, MoveInputEvent>(OnPullableMoveInput);
        SubscribeLocalEvent<PullableComponent, CollisionChangeEvent>(OnPullableCollisionChange);
        SubscribeLocalEvent<PullableComponent, JointRemovedEvent>(OnJointRemoved);
        SubscribeLocalEvent<PullableComponent, GetVerbsEvent<Verb>>(AddPullVerbs);
        SubscribeLocalEvent<PullableComponent, EntGotInsertedIntoContainerMessage>(OnPullableContainerInsert);
        SubscribeLocalEvent<PullableComponent, ModifyUncuffDurationEvent>(OnModifyUncuffDuration);
        SubscribeLocalEvent<PullableComponent, StopBeingPulledAlertEvent>(OnStopBeingPulledAlert);

        SubscribeLocalEvent<PullerComponent, UpdateMobStateEvent>(OnStateChanged, after: [typeof(MobThresholdSystem)]);
        SubscribeLocalEvent<PullerComponent, AfterAutoHandleStateEvent>(OnAfterState);
        SubscribeLocalEvent<PullerComponent, EntGotInsertedIntoContainerMessage>(OnPullerContainerInsert);
        SubscribeLocalEvent<PullerComponent, EntityUnpausedEvent>(OnPullerUnpaused);
        SubscribeLocalEvent<PullerComponent, VirtualItemDeletedEvent>(OnVirtualItemDeleted);
        SubscribeLocalEvent<PullerComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovespeed);
        SubscribeLocalEvent<PullerComponent, ComponentStartup>(OnPullerStartup); // SS220-cart-system
        SubscribeLocalEvent<PullerComponent, DropHandItemsEvent>(OnDropHandItems);
        SubscribeLocalEvent<PullerComponent, StopPullingAlertEvent>(OnStopPullingAlert);

        SubscribeLocalEvent<HandsComponent, PullStartedMessage>(HandlePullStarted);
        SubscribeLocalEvent<HandsComponent, PullStoppedMessage>(HandlePullStopped);

        SubscribeLocalEvent<PullableComponent, StrappedEvent>(OnBuckled);
        SubscribeLocalEvent<PullableComponent, BuckledEvent>(OnGotBuckled);
        SubscribeLocalEvent<ActivePullerComponent, TargetHandcuffedEvent>(OnTargetHandcuffed);

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.ReleasePulledObject, InputCmdHandler.FromDelegate(OnReleasePulledObject, handle: false))
            .Register<PullingSystem>();
    }

    //SS220-Cart-system begin
    private void OnPullerStartup(Entity<PullerComponent> entity, ref ComponentStartup args)
    {
        // Soooooooooo this is fucking dumb
        // but I WILL NOT rewrite the whole component to be
        // networked for the space wizards to do it themselves
        // right after me, so this will work for now and I don't really care
        // COPIUM
        if (!HasComp<CartPullerComponent>(entity))
            return;

        entity.Comp.NeedsHands = false;
    }
    //SS220-Cart-system end
    private void OnTargetHandcuffed(Entity<ActivePullerComponent> ent, ref TargetHandcuffedEvent args)
    {
        if (!TryComp<PullerComponent>(ent, out var comp))
            return;

        if (comp.Pulling == null)
            return;

        if (CanPull(ent, comp.Pulling.Value, comp))
            return;

        if (!TryComp<PullableComponent>(comp.Pulling, out var pullableComp))
            return;

        TryStopPull(comp.Pulling.Value, pullableComp);
    }

    private void HandlePullStarted(EntityUid uid, HandsComponent component, PullStartedMessage args)
    {
        if (args.PullerUid != uid)
            return;

        if (TryComp(args.PullerUid, out PullerComponent? pullerComp) && !pullerComp.NeedsHands)
            return;

        if (!_virtual.TrySpawnVirtualItemInHand(args.PulledUid, uid))
        {
            DebugTools.Assert("Unable to find available hand when starting pulling??");
        }
    }

    private void HandlePullStopped(EntityUid uid, HandsComponent component, PullStoppedMessage args)
    {
        if (args.PullerUid != uid)
            return;

        // Try find hand that is doing this pull.
        // and clear it.
        foreach (var held in _handsSystem.EnumerateHeld((uid, component)))
        {
            if (!TryComp(held, out VirtualItemComponent? virtualItem) || virtualItem.BlockingEntity != args.PulledUid)
                continue;

            _handsSystem.TryDrop((args.PullerUid, component), held);
            break;
        }
    }

    private void OnStateChanged(EntityUid uid, PullerComponent component, ref UpdateMobStateEvent args)
    {
        if (component.Pulling == null)
            return;

        if (TryComp<PullableComponent>(component.Pulling, out var comp) && (args.State == MobState.Critical || args.State == MobState.Dead))
        {
            TryStopPull(component.Pulling.Value, comp);
        }
    }

    private void OnBuckled(Entity<PullableComponent> ent, ref StrappedEvent args)
    {
        // Prevent people from pulling the entity they are buckled to
        if (ent.Comp.Puller == args.Buckle.Owner && !args.Buckle.Comp.PullStrap)
            StopPulling(ent, ent);
    }

    private void OnGotBuckled(Entity<PullableComponent> ent, ref BuckledEvent args)
    {
        StopPulling(ent, ent);
    }

    private void OnAfterState(Entity<PullerComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (ent.Comp.Pulling == null)
            RemComp<ActivePullerComponent>(ent.Owner);
        else
            EnsureComp<ActivePullerComponent>(ent.Owner);
    }

    private void OnDropHandItems(EntityUid uid, PullerComponent pullerComp, DropHandItemsEvent args)
    {
        if (pullerComp.Pulling == null || pullerComp.NeedsHands)
            return;

        if (!TryComp(pullerComp.Pulling, out PullableComponent? pullableComp))
            return;

        TryStopPull(pullerComp.Pulling.Value, pullableComp, uid);
    }

    private void OnStopPullingAlert(Entity<PullerComponent> ent, ref StopPullingAlertEvent args)
    {
        if (args.Handled)
            return;
        if (!TryComp<PullableComponent>(ent.Comp.Pulling, out var pullable))
            return;
        args.Handled = TryStopPull(ent.Comp.Pulling.Value, pullable, ent);
    }

    private void OnPullerContainerInsert(Entity<PullerComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        if (ent.Comp.Pulling == null)
            return;

        if (!TryComp(ent.Comp.Pulling.Value, out PullableComponent? pulling))
            return;

        TryStopPull(ent.Comp.Pulling.Value, pulling, ent.Owner);
    }

    private void OnPullableContainerInsert(Entity<PullableComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        TryStopPull(ent.Owner, ent.Comp);
    }

    private void OnModifyUncuffDuration(Entity<PullableComponent> ent, ref ModifyUncuffDurationEvent args)
    {
        if (!ent.Comp.BeingPulled)
            return;

        // We don't care if the person is being uncuffed by someone else
        if (args.User != args.Target)
            return;

        args.Duration *= 2;
    }

    private void OnStopBeingPulledAlert(Entity<PullableComponent> ent, ref StopBeingPulledAlertEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TryStopPull(ent, ent, ent);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        CommandBinds.Unregister<PullingSystem>();
    }

    private void OnPullerUnpaused(EntityUid uid, PullerComponent component, ref EntityUnpausedEvent args)
    {
        component.NextThrow += args.PausedTime;
    }

    private void OnVirtualItemDeleted(EntityUid uid, PullerComponent component, VirtualItemDeletedEvent args)
    {
        // If client deletes the virtual hand then stop the pull.
        if (component.Pulling == null)
            return;

        if (component.Pulling != args.BlockingEntity)
            return;

        if (TryComp(args.BlockingEntity, out PullableComponent? comp))
        {
            TryStopPull(args.BlockingEntity, comp);
        }
    }

    private void AddPullVerbs(EntityUid uid, PullableComponent component, GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        // Are they trying to pull themselves up by their bootstraps?
        if (args.User == args.Target)
            return;

        //TODO VERB ICONS add pulling icon
        if (component.Puller == args.User)
        {
            Verb verb = new()
            {
                Text = Loc.GetString("pulling-verb-get-data-text-stop-pulling"),
                Act = () => TryStopPull(uid, component, user: args.User),
                DoContactInteraction = false // pulling handle its own contact interaction.
            };
            args.Verbs.Add(verb);
        }
        else if (CanPull(args.User, args.Target))
        {
            Verb verb = new()
            {
                Text = Loc.GetString("pulling-verb-get-data-text"),
                Act = () => TryStartPull(args.User, args.Target),
                DoContactInteraction = false // pulling handle its own contact interaction.
            };
            args.Verbs.Add(verb);
        }
    }

    private void OnRefreshMovespeed(EntityUid uid, PullerComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        if (TryComp<HeldSpeedModifierComponent>(component.Pulling, out var heldMoveSpeed) && component.Pulling.HasValue)
        {
            var (walkMod, sprintMod) =
                _clothingMoveSpeed.GetHeldMovementSpeedModifiers(component.Pulling.Value, heldMoveSpeed);
            args.ModifySpeed(walkMod, sprintMod);
            return;
        }

        args.ModifySpeed(component.WalkSpeedModifier, component.SprintSpeedModifier);
    }

    private void OnPullableMoveInput(EntityUid uid, PullableComponent component, ref MoveInputEvent args)
    {
        // If someone moves then break their pulling.
        if (!component.BeingPulled)
            return;

        var entity = args.Entity;

        if (!_blocker.CanMove(entity))
            return;

        TryStopPull(uid, component, user: uid);
    }

    private void OnPullableCollisionChange(EntityUid uid, PullableComponent component, ref CollisionChangeEvent args)
    {
        // IDK what this is supposed to be.
        if (!_timing.ApplyingState && component.PullJointId != null && !args.CanCollide)
        {
            _joints.RemoveJoint(uid, component.PullJointId);
        }
    }

    private void OnJointRemoved(EntityUid uid, PullableComponent component, JointRemovedEvent args)
    {
        // Just handles the joint getting nuked without going through pulling system (valid behavior).

        // Not relevant / pullable state handle it.
        if (component.Puller != args.OtherEntity ||
            args.Joint.ID != component.PullJointId ||
            _timing.ApplyingState)
        {
            return;
        }

        if (args.Joint.ID != component.PullJointId || component.Puller == null)
            return;

        StopPulling(uid, component);
    }

    /// <summary>
    /// Forces pulling to stop and handles cleanup.
    /// </summary>
    private void StopPulling(EntityUid pullableUid, PullableComponent pullableComp)
    {
        if (pullableComp.Puller == null)
            return;

        if (!_timing.ApplyingState)
        {
            // Joint shutdown
            if (pullableComp.PullJointId != null)
            {
                _joints.RemoveJoint(pullableUid, pullableComp.PullJointId);
                pullableComp.PullJointId = null;
            }

            if (TryComp<PhysicsComponent>(pullableUid, out var pullablePhysics))
            {
                _physics.SetFixedRotation(pullableUid, pullableComp.PrevFixedRotation, body: pullablePhysics);
            }
        }

        var oldPuller = pullableComp.Puller;
        if (oldPuller != null)
            RemComp<ActivePullerComponent>(oldPuller.Value);

        pullableComp.PullJointId = null;
        pullableComp.Puller = null;
        Dirty(pullableUid, pullableComp);

        // No more joints with puller -> force stop pull.
        if (TryComp<PullerComponent>(oldPuller, out var pullerComp))
        {
            var pullerUid = oldPuller.Value;
            _alertsSystem.ClearAlert(pullerUid, pullerComp.PullingAlert);
            pullerComp.Pulling = null;
            Dirty(oldPuller.Value, pullerComp);

            // Messaging
            var message = new PullStoppedMessage(pullerUid, pullableUid);
            _modifierSystem.RefreshMovementSpeedModifiers(pullerUid);
            _adminLogger.Add(LogType.Action, LogImpact.Low, $"{ToPrettyString(pullerUid):user} stopped pulling {ToPrettyString(pullableUid):target}");

            RaiseLocalEvent(pullerUid, message);
            RaiseLocalEvent(pullableUid, message);
        }

        _alertsSystem.ClearAlert(pullableUid, pullableComp.PulledAlert);
    }

    public bool IsPulled(EntityUid uid, PullableComponent? component = null)
    {
        return Resolve(uid, ref component, false) && component.BeingPulled;
    }

    public bool IsPulling(EntityUid puller, PullerComponent? component = null)
    {
        return Resolve(puller, ref component, false) && component.Pulling != null;
    }

    public EntityUid? GetPuller(EntityUid puller, PullableComponent? component = null)
    {
        return !Resolve(puller, ref component, false) ? null : component.Puller;
    }

    public EntityUid? GetPulling(EntityUid puller, PullerComponent? component = null)
    {
        return !Resolve(puller, ref component, false) ? null : component.Pulling;
    }

    private void OnReleasePulledObject(ICommonSession? session)
    {
        if (session?.AttachedEntity is not { Valid: true } player)
        {
            return;
        }

        if (!TryComp(player, out PullerComponent? pullerComp) ||
            !TryComp(pullerComp.Pulling, out PullableComponent? pullableComp))
        {
            return;
        }

        TryStopPull(pullerComp.Pulling.Value, pullableComp, user: player);
    }

    public bool CanPull(EntityUid puller, EntityUid pullableUid, PullerComponent? pullerComp = null)
    {
        if (!Resolve(puller, ref pullerComp, false))
        {
            return false;
        }

        if (pullerComp.NeedsHands
            && !_handsSystem.TryGetEmptyHand(puller, out _)
            && pullerComp.Pulling == null)
        {
            return false;
        }

        if (!_blocker.CanInteract(puller, pullableUid))
        {
            return false;
        }

        if (!TryComp<PhysicsComponent>(pullableUid, out var physics))
        {
            return false;
        }

        if (physics.BodyType == BodyType.Static)
        {
            return false;
        }

        if (puller == pullableUid)
        {
            return false;
        }

        if (!_containerSystem.IsInSameOrNoContainer(puller, pullableUid))
        {
            return false;
        }

        var getPulled = new BeingPulledAttemptEvent(puller, pullableUid);
        RaiseLocalEvent(pullableUid, getPulled, true);
        var startPull = new StartPullAttemptEvent(puller, pullableUid);
        RaiseLocalEvent(puller, startPull, true);
        return !startPull.Cancelled && !getPulled.Cancelled;
    }

    public bool TogglePull(Entity<PullableComponent?> pullable, EntityUid pullerUid)
    {
        if (!Resolve(pullable, ref pullable.Comp, false))
            return false;

        if (pullable.Comp.Puller == pullerUid)
        {
            return TryStopPull(pullable, pullable.Comp);
        }

        return TryStartPull(pullerUid, pullable, pullableComp: pullable);
    }

    public bool TogglePull(EntityUid pullerUid, PullerComponent puller)
    {
        if (!TryComp<PullableComponent>(puller.Pulling, out var pullable))
            return false;

        return TogglePull((puller.Pulling.Value, pullable), pullerUid);
    }

    public bool TryStartPull(EntityUid pullerUid, EntityUid pullableUid,
        PullerComponent? pullerComp = null, PullableComponent? pullableComp = null)
    {
        if (!Resolve(pullerUid, ref pullerComp, false) ||
            !Resolve(pullableUid, ref pullableComp, false))
        {
            return false;
        }

        if (pullerComp.Pulling == pullableUid)
            return true;

        if (!CanPull(pullerUid, pullableUid))
            return false;

        if (!TryComp(pullerUid, out PhysicsComponent? pullerPhysics) || !TryComp(pullableUid, out PhysicsComponent? pullablePhysics))
            return false;

        // Ensure that the puller is not currently pulling anything.
        if (TryComp<PullableComponent>(pullerComp.Pulling, out var oldPullable)
            && !TryStopPull(pullerComp.Pulling.Value, oldPullable, pullerUid))
            return false;

        // Stop anyone else pulling the entity we want to pull
        if (pullableComp.Puller != null)
        {
            // We're already pulling this item
            if (pullableComp.Puller == pullerUid)
                return false;

            if (!TryStopPull(pullableUid, pullableComp, pullableComp.Puller))
                return false;
        }

        var pullAttempt = new PullAttemptEvent(pullerUid, pullableUid);
        RaiseLocalEvent(pullerUid, pullAttempt);

        if (pullAttempt.Cancelled)
            return false;

        RaiseLocalEvent(pullableUid, pullAttempt);

        if (pullAttempt.Cancelled)
            return false;

        // Pulling confirmed

        _interaction.DoContactInteraction(pullableUid, pullerUid);

        // Use net entity so it's consistent across client and server.
        pullableComp.PullJointId = $"pull-joint-{GetNetEntity(pullableUid)}";

        EnsureComp<ActivePullerComponent>(pullerUid);
        pullerComp.Pulling = pullableUid;
        pullableComp.Puller = pullerUid;

        // store the pulled entity's physics FixedRotation setting in case we change it
        pullableComp.PrevFixedRotation = pullablePhysics.FixedRotation;

        // joint state handling will manage its own state
        if (!_timing.ApplyingState)
        {
            var joint = _joints.CreateDistanceJoint(pullableUid, pullerUid,
                    pullablePhysics.LocalCenter, pullerPhysics.LocalCenter,
                    id: pullableComp.PullJointId);
            joint.CollideConnected = false;
            // This maximum has to be there because if the object is constrained too closely, the clamping goes backwards and asserts.
            // Internally, the joint length has been set to the distance between the pivots.
            // Add an additional 15cm (pretty arbitrary) to the maximum length for the hard limit.
            joint.MaxLength = joint.Length + 0.15f;
            joint.MinLength = 0f;
            // Set the spring stiffness to zero. The joint won't have any effect provided
            // the current length is beteen MinLength and MaxLength. At those limits, the
            // joint will have infinite stiffness.
            joint.Stiffness = 0f;

            _physics.SetFixedRotation(pullableUid, pullableComp.FixedRotationOnPull, body: pullablePhysics);
        }
        // SS220-MIT-pull-visualization-begin
        var xform = Transform(pullerUid);

        var pullerPos = _transform.GetWorldPosition(xform);
        var pulledPos = _transform.GetWorldPosition(pullableUid);

        var localPos = Vector2.Transform(pulledPos, _transform.GetInvWorldMatrix(xform));
        localPos = xform.LocalRotation.RotateVec(localPos);

        _melee.DoLunge(pullerUid, pullerUid, Angle.Zero, localPos, null);
        _audio.PlayPredicted(pullerComp.PullSound, pullableUid, pullerUid);

        var filter = Filter.Pvs(pullableUid, entityManager: EntityManager).RemoveWhereAttachedEntity(o => o == pullerUid);
        _colorFlash.RaiseEffect(СolorCaptureEffect, new List<EntityUid> { pullableUid }, filter);
        // SS220-MIT-pull-visualization-end

        // Messaging
        var message = new PullStartedMessage(pullerUid, pullableUid);
        _modifierSystem.RefreshMovementSpeedModifiers(pullerUid);
        _alertsSystem.ShowAlert(pullerUid, pullerComp.PullingAlert);
        _alertsSystem.ShowAlert(pullableUid, pullableComp.PulledAlert);

        RaiseLocalEvent(pullerUid, message);
        RaiseLocalEvent(pullableUid, message);

        Dirty(pullerUid, pullerComp);
        Dirty(pullableUid, pullableComp);

        var pullingMessage =
            Loc.GetString("getting-pulled-popup", ("puller", Identity.Entity(pullerUid, EntityManager)));
        _popup.PopupEntity(pullingMessage, pullableUid, pullableUid);

        _adminLogger.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(pullerUid):user} started pulling {ToPrettyString(pullableUid):target}");
        return true;
    }

    public bool TryStopPull(EntityUid pullableUid, PullableComponent pullable, EntityUid? user = null)
    {
        var pullerUidNull = pullable.Puller;

        if (pullerUidNull == null)
            return true;

        var msg = new AttemptStopPullingEvent(user);
        RaiseLocalEvent(pullableUid, ref msg, true);

        if (msg.Cancelled)
            return false;

        StopPulling(pullableUid, pullable);
        return true;
    }

    // SS220-MIT-pull-visualization-begin
    public override void FrameUpdate(float frameTime)
    {
        var query = EntityQueryEnumerator<PullerComponent>();
        while (query.MoveNext(out var uid, out var pullerComponent))
        {
            if (GetPulling(uid, pullerComponent) is not { } pulled)
                continue;

            if (_combatMode.IsInCombatMode(uid))
                continue;

            var pulledPos = _transform.GetMapCoordinates(pulled).Position;
            var pullerPos = _transform.GetMapCoordinates(uid).Position;
            var angle = (pulledPos - pullerPos).ToWorldAngle();
            _rotateTo.TryFaceAngle(uid, angle);
        }
    }
    // SS220-MIT-pull-visualization-end
}
