using Content.Shared.Actions;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Gravity;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.Movement.Systems;

public abstract class SharedJetpackSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;
    [Dependency] protected readonly SharedContainerSystem Container = default!;
    [Dependency] private readonly SharedMoverController _mover = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly ActionContainerSystem _actionContainer = default!;
    [Dependency] private readonly InventorySystem _inventory = default!; //SS220 Magboots with jet fix
    [Dependency] private readonly SharedBuckleSystem _buckle = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<JetpackComponent, GetItemActionsEvent>(OnJetpackGetAction);
        SubscribeLocalEvent<JetpackComponent, DroppedEvent>(OnJetpackDropped);
        SubscribeLocalEvent<JetpackComponent, ToggleJetpackEvent>(OnJetpackToggle);
        SubscribeLocalEvent<JetpackComponent, CanWeightlessMoveEvent>(OnJetpackCanWeightlessMove);

        SubscribeLocalEvent<JetpackUserComponent, CanWeightlessMoveEvent>(OnJetpackUserCanWeightless);
        SubscribeLocalEvent<JetpackUserComponent, EntParentChangedMessage>(OnJetpackUserEntParentChanged);
        SubscribeLocalEvent<JetpackUserComponent, MagbootsUpdateStateEvent>(OnMagbootsUpdateState); //SS220 Magboots with jet fix

        SubscribeLocalEvent<GravityChangedEvent>(OnJetpackUserGravityChanged);
        SubscribeLocalEvent<JetpackComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, JetpackComponent component, MapInitEvent args)
    {
        _actionContainer.EnsureAction(uid, ref component.ToggleActionEntity, component.ToggleAction);
        Dirty(uid, component);
    }

    private void OnJetpackCanWeightlessMove(EntityUid uid, JetpackComponent component, ref CanWeightlessMoveEvent args)
    {
        args.CanMove = true;
    }

    private void OnJetpackUserGravityChanged(ref GravityChangedEvent ev)
    {
        var gridUid = ev.ChangedGridIndex;
        var jetpackQuery = GetEntityQuery<JetpackComponent>();

        var query = EntityQueryEnumerator<JetpackUserComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var user, out var transform))
        {
            if (transform.GridUid == gridUid && ev.HasGravity &&
                jetpackQuery.TryGetComponent(user.Jetpack, out var jetpack))
            {
                _popup.PopupClient(Loc.GetString("jetpack-to-grid"), uid, uid);

                SetEnabled(user.Jetpack, jetpack, false, uid);
            }
        }
    }

    private void OnJetpackDropped(EntityUid uid, JetpackComponent component, DroppedEvent args)
    {
        SetEnabled(uid, component, false, args.User);
    }

    private void OnJetpackUserCanWeightless(EntityUid uid, JetpackUserComponent component, ref CanWeightlessMoveEvent args)
    {
        args.CanMove = true;
    }

    private void OnJetpackUserEntParentChanged(EntityUid uid, JetpackUserComponent component, ref EntParentChangedMessage args)
    {
        if (TryComp<JetpackComponent>(component.Jetpack, out var jetpack) &&
            !CanEnableOnGrid(args.Transform.GridUid))
        {
            SetEnabled(component.Jetpack, jetpack, false, uid);

            _popup.PopupClient(Loc.GetString("jetpack-to-grid"), uid, uid);
        }
    }

    private void SetupUser(EntityUid user, EntityUid jetpackUid)
    {
        var userComp = EnsureComp<JetpackUserComponent>(user);
        _mover.SetRelay(user, jetpackUid);

        if (TryComp<PhysicsComponent>(user, out var physics))
            _physics.SetBodyStatus(user, physics, BodyStatus.InAir);

        userComp.Jetpack = jetpackUid;
    }

    private void RemoveUser(EntityUid uid)
    {
        if (!RemComp<JetpackUserComponent>(uid))
            return;

        if (TryComp<PhysicsComponent>(uid, out var physics))
            _physics.SetBodyStatus(uid, physics, BodyStatus.OnGround);

        RemComp<RelayInputMoverComponent>(uid);
    }

    private void OnJetpackToggle(EntityUid uid, JetpackComponent component, ToggleJetpackEvent args)
    {
        if (args.Handled)
            return;

        //SS220 Magboots with jet fix begin
        var slotEnumerator = _inventory.GetSlotEnumerator(args.Performer);
        while (slotEnumerator.NextItem(out var item))
        {
            if (HasComp<MagbootsComponent>(item) &&
                TryComp<ItemToggleComponent>(item, out var itemToggle) &&
                itemToggle.Activated)
            {
                _popup.PopupClient(Loc.GetString("jetpack-no-magboots"), uid, args.Performer);
                return;
            }

            // SS220 FIX JETPACK CAMERA START (fix: https://github.com/SerbiaStrong-220/space-station-14/issues/1746)
            if (TryComp<BuckleComponent>(args.Performer, out var buckleComponent) && buckleComponent.BuckledTo != null)
            {
                _buckle.TryUnbuckle(args.Performer, args.Performer, buckleComponent);
            }
            // SS220 FIX JETPACK CAMERA END

            //SS220 Moonboots with jet fix begin
            if (HasComp<AntiGravityClothingComponent>(item))
            {
                SetEnabled(uid, component, !IsEnabled(uid));
                return;
            }
            //SS220 Moonboots with jet fix end
        }
        //SS220 Magboots with jet fix end

        if (TryComp(uid, out TransformComponent? xform) && !CanEnableOnGrid(xform.GridUid))
        {
            _popup.PopupClient(Loc.GetString("jetpack-no-station"), uid, args.Performer);

            return;
        }

        SetEnabled(uid, component, !IsEnabled(uid));
    }

    private bool CanEnableOnGrid(EntityUid? gridUid)
    {
        //return gridUid == null ||
        //       (!HasComp<GravityComponent>(gridUid));  //SS220 Convert to comment

        //SS220 Fix jet in zero gravity begin
        if (gridUid == null || !TryComp<GravityComponent>(gridUid, out var comp))
            return true;

        return !comp.Enabled;
        //SS220 Fix jet in zero gravity end
    }

    private void OnJetpackGetAction(EntityUid uid, JetpackComponent component, GetItemActionsEvent args)
    {
        args.AddAction(ref component.ToggleActionEntity, component.ToggleAction);
    }

    private bool IsEnabled(EntityUid uid)
    {
        return HasComp<ActiveJetpackComponent>(uid);
    }

    public void SetEnabled(EntityUid uid, JetpackComponent component, bool enabled, EntityUid? user = null)
    {
        if (IsEnabled(uid) == enabled ||
            enabled && !CanEnable(uid, component))
        {
            return;
        }

        if (enabled)
        {
            EnsureComp<ActiveJetpackComponent>(uid);
        }
        else
        {
            RemComp<ActiveJetpackComponent>(uid);
        }

        if (user == null)
        {
            Container.TryGetContainingContainer((uid, null, null), out var container);
            user = container?.Owner;
        }

        // Can't activate if no one's using.
        if (user == null && enabled)
            return;

        if (user != null)
        {
            if (enabled)
            {
                SetupUser(user.Value, uid);
            }
            else
            {
                RemoveUser(user.Value);
            }

            _movementSpeedModifier.RefreshMovementSpeedModifiers(user.Value);
        }

        Appearance.SetData(uid, JetpackVisuals.Enabled, enabled);
        Dirty(uid, component);
    }

    public bool IsUserFlying(EntityUid uid)
    {
        return HasComp<JetpackUserComponent>(uid);
    }

    protected virtual bool CanEnable(EntityUid uid, JetpackComponent component)
    {
        return true;
    }

    //SS220 Magboots with jet fix begin
    private void OnMagbootsUpdateState(Entity<JetpackUserComponent> ent, ref MagbootsUpdateStateEvent args)
    {
        if (!args.State)
            return;

        if (TryComp<JetpackComponent>(ent.Comp.Jetpack, out var jetpack))
        {
            SetEnabled(ent.Comp.Jetpack, jetpack, false, ent.Owner);
            _popup.PopupClient(Loc.GetString("jetpack-to-grid"), ent.Comp.Jetpack, ent.Owner);
        }
    }
    //SS220 Magboots with jet fix end
}

[Serializable, NetSerializable]
public enum JetpackVisuals : byte
{
    Enabled,
}
