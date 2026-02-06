using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Server.Power.EntitySystems;
using Content.Shared.DoAfter;
using Content.Shared.EntityEffects.Effects.StatusEffects;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Power.Components;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.SS220.Mech.Components;
using Content.Shared.SS220.Mech.Equipment.Components;
using Content.Shared.SS220.Mech.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using static Content.Server.Power.Pow3r.PowerState;

namespace Content.Server.SS220.Mech.Systems;

/// <summary>
/// Handles the insertion of mech equipment into mechs.
/// </summary>
public sealed class MechPartSystem : EntitySystem
{
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly AltMechSystem _mech = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] protected readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly BatterySystem _battery = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<MechPartComponent, AfterInteractEvent>(OnUsed);
        SubscribeLocalEvent<MechPartComponent, InsertPartEvent>(OnInsertPart);

        SubscribeLocalEvent<MechChassisComponent, MechPartInsertedEvent>(OnChassisInserted);
        SubscribeLocalEvent<MechChassisComponent, MechPartRemovedEvent>(OnChassisRemoved);

        SubscribeLocalEvent<BatteryComponent, MechPartInsertedEvent>(OnPowerInserted);
        SubscribeLocalEvent<BatteryComponent, MechPartRemovedEvent>(OnPowerRemoved);

        SubscribeLocalEvent<MechArmComponent, MechPartInsertedEvent>(OnArmInserted);
        SubscribeLocalEvent<MechArmComponent, MechPartRemovedEvent>(OnArmRemoved);

        SubscribeLocalEvent<MechArmComponent, ComponentStartup>(OnProvideItemStartup);
        //SubscribeLocalEvent<MechPartComponent, InsertPartEvent>(OnOpticsInserted);
        //SubscribeLocalEvent<MechPartComponent, InsertPartEvent>(OnArmInserted);
    }

    private void OnUsed(Entity<MechPartComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target == null)
            return;

        var mech = args.Target.Value;
        if (!TryComp<AltMechComponent>(mech, out var mechComp))
            return;

        if (mechComp.Broken)
            return;

        if (args.User == mechComp.PilotSlot.ContainedEntity)
            return;

        if (ent.Comp.EquipmentContainer.ContainedEntities.Count >= ent.Comp.MaxEquipmentAmount)
            return;

        if (_whitelistSystem.IsWhitelistFail(ent.Comp.EquipmentWhitelist, args.Used))
            return;

        _popup.PopupEntity(Loc.GetString("mech-equipment-begin-install", ("item", ent.Owner)), mech);

        var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, ent.Comp.InstallDuration, new InsertPartEvent(), ent.Owner, target: mech, used: ent.Owner)
        {
            BreakOnMove = true,
        };

        _doAfter.TryStartDoAfter(doAfterEventArgs);
        args.Handled = true;
    }

    private void OnProvideItemStartup(EntityUid uid, MechArmComponent component, ComponentStartup args)
    {
        _container.EnsureContainer<Container>(uid, component.HoldingContainer);
    }

    //private void OnArmInserted(EntityUid uid, MechArmComponent component, ref MechPartInsertedEvent args)
    //{
    //    if(!TryComp<MechPartComponent>(uid, out var partComp))
    //        return;

    //    if (partComp.PartOwner == null || !TryComp<AltMechComponent>(uid, out var mechComp))
    //        return;

    //    ProvideItems((EntityUid)partComp.PartOwner, uid, mechComp, component);
    //}

    public void ProvideItems(EntityUid mechUid, EntityUid uid, AltMechComponent? mechComp = null, MechArmComponent? armComp = null)
    {
        if (!Resolve(mechUid, ref mechComp) || !Resolve(uid, ref armComp))
            return;

        if (!_container.TryGetContainer(uid, armComp.HoldingContainer, out var container))
            return;

        if (!TryComp<HandsComponent>((EntityUid)mechUid, out var hands))
            return;

        //EntityUid pilot = (EntityUid)mechComp.PilotSlot.ContainedEntity;

        var xform = Transform(mechUid);

        for (var i = 0; i < armComp.Hands.Count; i++)
        {
            var hand = armComp.Hands[i];
            var handId = $"{uid}-hand-{i}";

            _hands.AddHand(((EntityUid)mechUid, hands), handId, hand.Hand);
            EntityUid? item = null;

            if (armComp.StoredItems is not null)
            {
                if (armComp.StoredItems.TryGetValue(handId, out var storedItem))
                {
                    item = storedItem;
                    _container.Remove(storedItem, container, force: true);
                }
            }
            else if (hand.Item is { } itemProto)
            {
                item = Spawn(itemProto, xform.Coordinates);
            }

            if (item is { } pickUp)
            {
                _hands.DoPickup((EntityUid)mechUid, handId, pickUp, hands);
                if (!hand.ForceRemovable && hand.Hand.Whitelist == null && hand.Hand.Blacklist == null)
                {
                    //EnsureComp<UnremoveableComponent>(pickUp);
                }
            }
        }

        Dirty(uid, armComp);
    }

    public void RemoveProvidedItems(EntityUid mechUid, EntityUid uid, AltMechComponent? mechComp = null, MechArmComponent? component = null)
    {
        if (!Resolve(mechUid, ref mechComp) || !Resolve(uid, ref component))
            return;

        if (!_container.TryGetContainer(uid, component.HoldingContainer, out var container))
            return;

        if (TerminatingOrDeleted(uid))
            return;

        if (!TryComp<HandsComponent>((EntityUid)mechUid, out var hands))
            return;

        component.StoredItems ??= new();

        for (var i = 0; i < component.Hands.Count; i++)
        {
            var handId = $"{uid}-hand-{i}";

            if (_hands.TryGetHeldItem(mechUid, handId, out var held))
            {
                RemComp<UnremoveableComponent>(held.Value);
                _container.Insert(held.Value, container);
                component.StoredItems[handId] = held.Value;
            }
            else
            {
                component.StoredItems.Remove(handId);
            }

            _hands.RemoveHand(mechUid, handId);
        }

        Dirty(uid, component);
    }

    private void OnArmRemoved(Entity<MechArmComponent> ent, ref MechPartRemovedEvent args)
    {
        if (!TryComp<AltMechComponent>(args.Mech, out var mechComp))
            return;

        RemoveProvidedItems(args.Mech, ent.Owner, mechComp, ent.Comp);

        Dirty(ent.Owner, ent.Comp);
        Dirty(args.Mech, mechComp);
    }

    private void OnArmInserted(Entity<MechArmComponent> ent, ref MechPartInsertedEvent args)
    {
        if (!TryComp<AltMechComponent>(args.Mech, out var mechComp))
            return;

        ProvideItems(args.Mech, ent.Owner, mechComp, ent.Comp);

        Dirty(ent.Owner, ent.Comp);
        Dirty(args.Mech, mechComp);
    }

    private void OnChassisInserted(Entity<MechChassisComponent> ent, ref MechPartInsertedEvent args)
    {
        if (!TryComp<AltMechComponent>(args.Mech, out var mechComp))
            return;

        mechComp.OverallBaseMovementSpeed = ent.Comp.BaseMovementSpeed;
        mechComp.OverallBaseAcceleration = ent.Comp.Acceleration;
        mechComp.MaximalMass = ent.Comp.MaximalMass;

        if (!TryComp<FootstepModifierComponent>(args.Mech, out var footstepModifierComp))
            return;

        footstepModifierComp.FootstepSoundCollection = ent.Comp.FootstepSound;

        Dirty(ent.Owner, ent.Comp);
        Dirty(args.Mech, footstepModifierComp);
    }

    private void OnChassisRemoved(Entity<MechChassisComponent> ent, ref MechPartRemovedEvent args)
    {
        if (!TryComp<AltMechComponent>(args.Mech, out var mechComp))
            return;

        mechComp.OverallBaseMovementSpeed = 0;
        mechComp.OverallBaseAcceleration = 0;

        Dirty(ent.Owner, ent.Comp);
    }

    private void OnPowerInserted(Entity<BatteryComponent> ent, ref MechPartInsertedEvent args)
    {
        if (!TryComp<AltMechComponent>(args.Mech, out var mechComp))
            return;

        mechComp.Energy = ent.Comp.CurrentCharge;
        mechComp.MaxEnergy = ent.Comp.MaxCharge;

        _mech.UpdateMechOnlineStatus(args.Mech, ent.Owner);
    }

    private void OnInsertPart(EntityUid uid, MechPartComponent component, InsertPartEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Target == null)
            return;

        _popup.PopupEntity(Loc.GetString("mech-equipment-finish-install", ("item", uid)), args.Args.Target.Value);
        _mech.InsertPart(args.Args.Target.Value, uid);

        if (component.PartOwner != null)
            _mech.UpdateUserInterface((EntityUid)component.PartOwner);

        args.Handled = true;
    }

    private void OnPowerRemoved(Entity<BatteryComponent> ent, ref MechPartRemovedEvent args)
    {
        if (!TryComp<AltMechComponent>(args.Mech, out var mechComp))
            return;

        mechComp.Energy = 0;
        mechComp.MaxEnergy = 1;

        _mech.UpdateMechOnlineStatus(args.Mech, ent.Owner);
    }
}
