// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT
using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Server.Power.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Power.Components;
using Content.Shared.FCB.Mech.Components;
using Content.Shared.FCB.Mech.Parts.Components;
using Content.Shared.FCB.Mech.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;

namespace Content.Server.FCB.Mech.Systems;

/// <summary>
/// Handles the insertion of mech equipment into mechs.
/// </summary>
public sealed class MechPartSystem : EntitySystem
{
    [Dependency] private readonly AltMechSystem _mech = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly BlindableSystem _blindable = default!;

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
        SubscribeLocalEvent<MechOpticsComponent, MechPartInsertedEvent>(OnOpticsInserted);
        SubscribeLocalEvent<MechOpticsComponent, MechPartRemovedEvent>(OnOpticsRemoved);

        SubscribeLocalEvent<MechPartComponent, DestructionEventArgs>(OnPartDestroyed);
        //SubscribeLocalEvent<MechPartComponent, InsertPartEvent>(OnArmInserted);
    }

    private void OnPartDestroyed(Entity<MechPartComponent> ent, ref DestructionEventArgs args)
    {
        if (!TryComp<AltMechComponent>(ent.Comp.PartOwner, out var mechComp))
            return;

        var equip = (mechComp.ContainerDict[ent.Comp.slot].ContainedEntity);

        if (!Exists(equip) || Deleted(equip))
            return;

        _mech.RemovePart((EntityUid)ent.Comp.PartOwner, ent.Owner);
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

        if (!mechComp.MaintenanceMode)
        {
            _popup.PopupEntity(Loc.GetString("mech-maintenance-offline"), mech);
            return;
        }

        if (!mechComp.ContainerDict.ContainsKey(ent.Comp.slot) || mechComp.ContainerDict[ent.Comp.slot].ContainedEntity != null)
        {
            _popup.PopupEntity(Loc.GetString("mech-part-slot-occupied"), mech);
            return;
        }

        if (args.User == mechComp.PilotSlot.ContainedEntity)
            return;

        _popup.PopupEntity(Loc.GetString("mech-equipment-begin-install", ("item", ent.Owner)), mech);

        var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, ent.Comp.InstallDuration, new InsertPartEvent(), ent.Owner, target: mech, used: ent.Owner)
        {
            BreakOnMove = true,
        };

        _doAfter.TryStartDoAfter(doAfterEventArgs);
        args.Handled = true;
    }

    private void OnOpticsInserted(Entity<MechOpticsComponent> ent, ref MechPartInsertedEvent args)
    {
        if (!TryComp<AltMechComponent>(args.Mech, out var mechComp))
            return;

        if (mechComp.PilotSlot.ContainedEntity == null)
            return;

        if (!TryComp<BlindableComponent>(ent.Owner, out var blindableCompMech))
            return;

        if (TryComp<BlindableComponent>(mechComp.PilotSlot.ContainedEntity, out var blindableComp))
        {
            _blindable.AdjustEyeDamage((args.Mech, blindableCompMech), (-blindableCompMech.EyeDamage + blindableComp.EyeDamage));
            return;
        }

        _blindable.AdjustEyeDamage((args.Mech, blindableCompMech), -blindableCompMech.EyeDamage);
    }

    private void OnOpticsRemoved(Entity<MechOpticsComponent> ent, ref MechPartRemovedEvent args)
    {
        if (!TryComp<AltMechComponent>(args.Mech, out var mechComp))
            return;

        if (mechComp.Transparent)
            return;

        if (!TryComp<BlindableComponent>(args.Mech, out var blindableCompMech))
            return;

        _blindable.AdjustEyeDamage((args.Mech, blindableCompMech), (9 - blindableCompMech.EyeDamage)); //Mech cannot see anything if it has no eyes
    }

    private void OnProvideItemStartup(EntityUid uid, MechArmComponent component, ComponentStartup args)
    {
        _container.EnsureContainer<Container>(uid, component.HoldingContainer);
    }

    public void ProvideItems(EntityUid mechUid, EntityUid uid, AltMechComponent? mechComp = null, MechArmComponent? armComp = null)
    {
        if (!Resolve(mechUid, ref mechComp) || !Resolve(uid, ref armComp))
            return;

        if (!_container.TryGetContainer(uid, armComp.HoldingContainer, out var container))
            return;

        if (!TryComp<HandsComponent>(mechUid, out var hands))
            return;

        var xform = Transform(mechUid);

        for (var i = 0; i < armComp.Hands.Count; i++)
        {
            var hand = armComp.Hands[i];
            var handId = $"{uid}-hand-{i}";

            _hands.AddHand((mechUid, hands), handId, hand.Hand);
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
                _hands.DoPickup(mechUid, handId, pickUp, hands);
                EnsureComp<UnremoveableComponent>(pickUp);
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

        if (!TryComp<HandsComponent>(mechUid, out var hands))
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

        mechComp.Energy = ent.Comp.LastCharge;
        mechComp.MaxEnergy = ent.Comp.MaxCharge;

        _mech.UpdateMechOnlineStatus(args.Mech, ent.Owner);
    }

    private void OnInsertPart(Entity<MechPartComponent> ent, ref InsertPartEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Target == null)
            return;

        _popup.PopupEntity(Loc.GetString("mech-equipment-finish-install", ("item", ent.Owner)), args.Args.Target.Value);
        _mech.InsertPart(args.Args.Target.Value, ent.Owner);

        if (ent.Comp.PartOwner != null)
            _mech.UpdateUserInterface((EntityUid)ent.Comp.PartOwner);

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
