using Content.Shared.Combat;
using Content.Shared.DoAfter;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.MouseRotator;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Events;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.Grab;

// Baby steps for a bigger system to come
// This is a system separate from PullingSystem due to their different purposes: PullingSystem is meant just to pull things around and GrabSystem is designed for combat
// Current hacks:
// - The control flow comes from PullingSystem 'cuz of input handling
public sealed partial class GrabSystem : EntitySystem
{
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
        SubscribeLocalEvent<GrabberComponent, PreventCollideEvent>(OnPreventCollide);

        SubscribeLocalEvent<GrabberComponent, VirtualItemDeletedEvent>(OnVirtualItemDeleted);
    }

    #region Events Handling

    private void OnGrabDoAfter(Entity<GrabberComponent> grabber, ref GrabDoAfterEvent ev)
    {
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
    private void OnPreventCollide(Entity<GrabberComponent> ent, ref PreventCollideEvent ev)
    {
        if (!TryComp<GrabbableComponent>(ev.OtherEntity, out var grabbable))
            return;

        if (!grabbable.Grabbed)
            return;

        if (grabbable.GrabbedBy != ent)
            return;

        ev.Cancelled = true;
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

    #endregion

    #region Public API
    public bool TryDoGrab(Entity<GrabberComponent?> grabber, Entity<GrabbableComponent?> grabbable)
    {
        if (!Resolve(grabber, ref grabber.Comp))
            return false;
        if (!Resolve(grabbable, ref grabbable.Comp))
            return false;

        if (!CanGrab(grabber, grabbable, checkCanPull: false)) // the control flow comes from pulling system after pull checks
            return false;

        if (grabbable.Comp.GrabStage >= GrabStage.Last)
            return true;

        if (_timing.IsFirstTimePredicted)
        {
            var grabberMeta = MetaData(grabber);
            var grabbableMeta = MetaData(grabbable);

            var msg = grabbable.Comp.GrabStage == GrabStage.None
                ? Loc.GetString("grabber-component-new-grab-popup", ("grabber", grabberMeta.EntityName), ("grabbable", grabbableMeta.EntityName))
                : Loc.GetString("grabber-component-grab-upgrade-popup", ("grabber", grabberMeta.EntityName), ("grabbable", grabbableMeta.EntityName));

            _popup.PopupPredicted(msg, grabber, grabber);
        }

        var args = new DoAfterArgs(EntityManager, user: grabber, grabber.Comp.GrabDelay, new GrabDoAfterEvent(), eventTarget: grabber, target: grabbable)
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

        if (checkCanPull)
            return _pulling.CanPull(grabber, grabbable, ignoreHands: true);

        return true;
    }

    public void ChangeGrabStage(Entity<GrabberComponent> grabber, Entity<GrabbableComponent> grabbable, GrabStage newStage)
    {
        grabbable.Comp.GrabStage = newStage;
        RefreshGrabResistance((grabbable, grabbable.Comp));
        _movementSpeed.RefreshMovementSpeedModifiers(grabber);

        var ev = new GrabStageChangeEvent(grabber, grabbable, newStage);
        RaiseLocalEvent(grabber, ev);
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

        if (!_virtualItem.TrySpawnVirtualItemInHand(grabbable, grabber, out var virtualItem))
            return;

        if (!_virtualItem.TrySpawnVirtualItemInHand(grabbable, grabber, out var virtualItem2))
            return;

        grabber.Comp.Grabbing = grabbable;
        grabbable.Comp.GrabbedBy = grabber;

        _transform.SetParent(grabbable, grabber);
        _transform.SetLocalPosition(grabbable, grabber.Comp.GrabOffset);
        _transform.SetLocalRotation(grabbable, Angle.Zero);

        ChangeGrabStage(grabber, grabbable, grabStage);
    }

    private void UpgradeGrab(Entity<GrabberComponent> grabber, Entity<GrabbableComponent> grabbable)
    {
        ChangeGrabStage(grabber, grabbable, grabbable.Comp.GrabStage + 1);
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
        _transform.DropNextTo(grabbable.Owner, grabbable.Owner);
        _popup.PopupPredicted(Loc.GetString("grabbable-component-break-free", ("grabbable", MetaData(grabbable).EntityName)), grabbable, grabbable);

        _virtualItem.DeleteInHandsMatching(grabber, grabbable);
    }

    #endregion
}
