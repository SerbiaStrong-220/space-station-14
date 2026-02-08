// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Access.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mech;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.EntitySystems;
using Content.Shared.Mech.Equipment.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.SS220.ArmorBlock;
using Content.Shared.SS220.Mech.Components;
using Content.Shared.SS220.Mech.Equipment.Components;
using Content.Shared.SS220.MechRobot;
using Content.Shared.Standing;
using Content.Shared.Storage.Components;
using Content.Shared.Weapons.Melee;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Content.Shared.Inventory;
using Content.Shared.Inventory;

namespace Content.Shared.SS220.Mech.Systems;

/// <summary>
/// Handles all of the interactions, UI handling, and items shennanigans for <see cref="MechComponent"/>
/// </summary>
public abstract partial class SharedAltMechSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedMoverController _mover = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;


    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<AltMechComponent, MechToggleEquipmentEvent>(OnToggleEquipmentAction);
        SubscribeLocalEvent<AltMechComponent, MechEjectPilotEvent>(OnEjectPilotEvent);
        SubscribeLocalEvent<AltMechComponent, UserActivateInWorldEvent>(RelayInteractionEvent);
        SubscribeLocalEvent<AltMechComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<AltMechComponent, EntityStorageIntoContainerAttemptEvent>(OnEntityStorageDump);
        SubscribeLocalEvent<AltMechComponent, GetAdditionalAccessEvent>(OnGetAdditionalAccess);
        SubscribeLocalEvent<AltMechComponent, DragDropTargetEvent>(OnDragDrop);
        SubscribeLocalEvent<AltMechComponent, CanDropTargetEvent>(OnCanDragDrop);

        SubscribeLocalEvent<AltMechPilotComponent, GetMeleeWeaponEvent>(OnGetMeleeWeapon);
        SubscribeLocalEvent<AltMechPilotComponent, CanAttackFromContainerEvent>(OnCanAttackFromContainer);
        SubscribeLocalEvent<AltMechPilotComponent, AttackAttemptEvent>(OnAttackAttempt);

        InitializeRelay();
    }

    private void OnToggleEquipmentAction(EntityUid uid, AltMechComponent component, MechToggleEquipmentEvent args)
    {
        if (args.Handled)
            return;
        args.Handled = true;
    }

    private void OnEjectPilotEvent(EntityUid uid, AltMechComponent component, MechEjectPilotEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var ev = new OnMechExitEvent();
        RaiseLocalEvent(uid, ref ev);

        TryEject(uid, component);
    }

    private void RelayInteractionEvent(EntityUid uid, AltMechComponent component, UserActivateInWorldEvent args)
    {
        var pilot = component.PilotSlot.ContainedEntity;
        if (pilot == null)
            return;

        // TODO why is this being blocked?
        if (!_timing.IsFirstTimePredicted)
            return;

        //if (component.CurrentSelectedEquipment != null)
        //{
        //    RaiseLocalEvent(component.CurrentSelectedEquipment.Value, args);
        //}
    }
    //SS220-AddMechToClothing-start
    /// <summary>
    /// Separates mech-robot and mech-clothing
    /// </summary>
    private void OnStartup(Entity<AltMechComponent> ent, ref ComponentStartup args)
    {
        foreach (var part in ent.Comp.ContainersToCreate)
            ent.Comp.ContainerDict[part] = _container.EnsureContainer<ContainerSlot>(ent.Owner, part);

        //ent.Comp.ContainerDict["power"] = _container.EnsureContainer<ContainerSlot>(ent.Owner, ent.Comp.ContainerDict["power"]);

        //SS220-MechClothingInHandsFix
        ent.Comp.PilotSlot = _container.EnsureContainer<ContainerSlot>(ent.Owner, ent.Comp.PilotSlotId);

        ent.Comp.TankSlot = _container.EnsureContainer<ContainerSlot>(ent.Owner, ent.Comp.TankSlotId);

        ent.Comp.OverallMass += ent.Comp.OwnMass;

        if(TryComp<MovementSpeedModifierComponent>(ent.Owner, out var movementComp))
            _movementSpeedModifier.ChangeBaseSpeed(ent.Owner, ent.Comp.OverallBaseMovementSpeed * 0.5f, ent.Comp.OverallBaseMovementSpeed, ent.Comp.OverallBaseAcceleration, movementComp);

        UpdateAppearance(ent.Owner, ent.Comp);
    }

    public virtual void OnStartupServer(Entity<AltMechComponent> ent)
    {

    }
    //SS220-AddMechToClothing-end

    private void OnEntityStorageDump(Entity<AltMechComponent> entity, ref EntityStorageIntoContainerAttemptEvent args)
    {
        // There's no reason we should dump into /any/ of the mech's containers.
        args.Cancelled = true;
    }

    private void OnGetAdditionalAccess(EntityUid uid, AltMechComponent component, ref GetAdditionalAccessEvent args)
    {
        var pilot = component.PilotSlot.ContainedEntity;
        if (pilot == null)
            return;

        args.Entities.Add(pilot.Value);
    }

    private void SetupUser(EntityUid mech, EntityUid pilot, AltMechComponent? component = null)
    {
        if (!Resolve(mech, ref component))
            return;

        var rider = EnsureComp<AltMechPilotComponent>(pilot);

        // Warning: this bypasses most normal interaction blocking components on the user, like drone laws and the like.
        //var irelay = EnsureComp<InteractionRelayComponent>(pilot);

        //_mover.SetRelay(pilot, mech);
        //_interaction.SetRelay(pilot, mech, irelay);
        rider.Mech = mech;
        //Dirty(pilot, rider);

        //var ev = new DropHandItemsEvent();
        //RaiseLocalEvent(pilot, ref ev);

        if (_net.IsClient)
            return;

        var ev = new DropHandItemsEvent();
        RaiseLocalEvent(pilot, ref ev);

        //if (!TryComp<HandsComponent>(pilot, out var handsComp))
        //    return;

        //foreach (var hand in handsComp.Hands)
        //{
        //    if (hand.Value.Location == HandLocation.Right)
        //    {
        //        _hands.TrySetActiveHand((pilot, handsComp), hand.Key);
        //        break;
        //    }
        //}

        //foreach (var hand in handsComp.Hands)
        //    component.Hands.Add(hand.Key,hand.Value);

        //foreach (var hand in handsComp.Hands )
        //{
        //    _hands.RemoveHands((pilot, handsComp));
        //}

        //_actions.AddAction(pilot, ref component.MechCycleActionEntity, component.MechCycleAction, mech);
        _actions.AddAction(pilot, ref component.MechUiActionEntity, component.MechUiAction, mech);
        _actions.AddAction(pilot, ref component.MechEjectActionEntity, component.MechEjectAction, mech);
    }

    private void RemoveUser(EntityUid mech, EntityUid pilot)
    {
        if (!RemComp<AltMechPilotComponent>(pilot))
            return;
        //RemComp<RelayInputMoverComponent>(pilot);
        //RemComp<InteractionRelayComponent>(pilot);

        _actions.RemoveProvidedActions(pilot, mech);

        if (_net.IsClient)
            return;

        if (!TryComp<AltMechComponent>(mech, out var mechComp))
            return;

        if (!TryComp<HandsComponent>(pilot, out var handsComp))
            return;

        foreach (var hand in mechComp.Hands)
        {
            _hands.AddHand((pilot,handsComp),hand.Key,hand.Value);
        }

        mechComp.Hands.Clear();
    }

    /// <summary>
    /// Destroys the mech, removing the user and ejecting anything contained.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    public virtual void BreakMech(EntityUid uid, AltMechComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        TryEject(uid, component);
        var equipment = new List<EntityUid>(component.EquipmentContainer.ContainedEntities);
        foreach (var ent in equipment)
        {
           // RemoveEquipment(uid, ent, partComp, forced: true);
        }

        component.Broken = true;
        UpdateAppearance(uid, component);
    }

    /// <summary>
    /// Inserts an equipment item into the mech.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="toInsert"></param>
    /// <param name="component"></param>
    /// <param name="equipmentComponent"></param>
    public void InsertEquipment(EntityUid uid, EntityUid toInsert, AltMechComponent? component = null,
        MechEquipmentComponent? equipmentComponent = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!Resolve(toInsert, ref equipmentComponent))
            return;

        //if (component.EquipmentContainer.ContainedEntities.Count >= component.MaxEquipmentAmount)
        //    return;

        if (_whitelistSystem.IsWhitelistFail(component.EquipmentWhitelist, toInsert))
            return;

        equipmentComponent.EquipmentOwner = uid;
        //_container.Insert(toInsert, component.EquipmentContainer);
        var ev = new MechEquipmentInsertedEvent(uid);
        RaiseLocalEvent(toInsert, ref ev);
        //UpdateUserInterface(uid, mechComp);
    }

    public void InsertPart(EntityUid uid, EntityUid toInsert)
    {
        if (!TryComp<AltMechComponent>(uid,out var component))
            return;

        if (!component.MaintenanceMode)
            return;

        if (!TryComp<MechPartComponent>(toInsert, out var partComponent))
            return;

        if (!component.ContainerDict.ContainsKey(partComponent.slot) || component.ContainerDict[partComponent.slot].ContainedEntity != null)
            return;

        partComponent.PartOwner = uid;
        _container.Insert(toInsert, component.ContainerDict[partComponent.slot]);

        AddMass(component, partComponent.OwnMass);

        Dirty(uid, component);
        Dirty(toInsert, partComponent);

        var ev = new MechPartInsertedEvent(uid);
        RaiseLocalEvent(toInsert, ref ev);

        var massEv = new MassChangedEvent();
        RaiseLocalEvent(uid, ref massEv);

        if (TryGetNetEntity(uid, out var netMech) && TryGetNetEntity(toInsert, out var netPart))
            RaiseNetworkEvent(new MechPartStatusChanged((NetEntity)netMech, (NetEntity)netPart, true));

        //UpdateUserInterface(uid, component);
    }

    public void AddMass(AltMechComponent mechComp, FixedPoint2 Value)
    {
        mechComp.OverallMass += Value;
    }

    public void RemoveMass(AltMechComponent mechComp, FixedPoint2 Value)
    {
        mechComp.OverallMass -= Value;
    }

    /// <summary>
    /// Removes an equipment item from a mech.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="toRemove"></param>
    /// <param name="component"></param>
    /// <param name="equipmentComponent"></param>
    /// <param name="forced">
    ///     Whether or not the removal can be cancelled, and if non-mech equipment should be ejected.
    /// </param>
    public void RemoveEquipment(EntityUid uid, EntityUid toRemove, MechPartComponent? component = null,
        MechEquipmentComponent? equipmentComponent = null, bool forced = false)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.PartOwner == null || !TryComp<AltMechComponent>(component.PartOwner, out var mechComp))
            return;
        // When forced, we also want to handle the possibility that the "equipment" isn't actually equipment.
        // This /shouldn't/ be possible thanks to OnEntityStorageDump, but there's been quite a few regressions
        // with entities being hardlock stuck inside mechs.
        if (!Resolve(toRemove, ref equipmentComponent) && !forced)
            return;

        if (!forced)
        {
            var attemptev = new AttemptRemoveMechEquipmentEvent();
            RaiseLocalEvent(toRemove, ref attemptev);
            if (attemptev.Cancelled)
                return;
        }

        var ev = new MechEquipmentRemovedEvent(uid);
        RaiseLocalEvent(toRemove, ref ev);

        if (forced && equipmentComponent != null)
            equipmentComponent.EquipmentOwner = null;

        //_container.Remove(toRemove, component.EquipmentContainer);
        //UpdateUserInterface(uid, mechComp);
    }

    /// <summary>
    /// Removes an equipment item from a mech.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="toRemove"></param>
    /// <param name="component"></param>
    /// <param name="partComponent"></param>
    /// <param name="forced">
    ///     Whether or not the removal can be cancelled, and if non-mech equipment should be ejected.
    /// </param>
    public void RemovePart(EntityUid uid, EntityUid toRemove, bool forced = true)
    {
        if (!TryComp<AltMechComponent>(uid, out var component))
            return;

        // When forced, we also want to handle the possibility that the "equipment" isn't actually equipment.
        // This /shouldn't/ be possible thanks to OnEntityStorageDump, but there's been quite a few regressions
        // with entities being hardlock stuck inside mechs.
        if (!TryComp<MechPartComponent>(toRemove, out var partComponent))
            return;

        if (partComponent == null)
            return;

        if (!component.ContainerDict.ContainsKey(partComponent.slot) || component.ContainerDict[partComponent.slot].ContainedEntity == null)
            return;

        if (!forced)
        {
            var attemptev = new AttemptRemoveMechEquipmentEvent();
            RaiseLocalEvent(toRemove, ref attemptev);
            if (attemptev.Cancelled)
                return;
        }

        //if (forced && partComponent != null)
        //    partComponent.PartOwner = null;

        if(partComponent != null)
        {
            partComponent.PartOwner = null;
            RemoveMass(component, partComponent.OwnMass);

            _container.Remove(toRemove, component.ContainerDict[partComponent.slot]);

            var ev = new MechPartRemovedEvent(uid);
            RaiseLocalEvent(toRemove, ref ev);

            var massEv = new MassChangedEvent();
            RaiseLocalEvent(uid, ref massEv);

            Dirty(toRemove, partComponent);
        }

        Dirty(uid, component);

        if(TryGetNetEntity(uid, out var netMech) && TryGetNetEntity(toRemove, out var netPart))
            RaiseNetworkEvent( new MechPartStatusChanged((NetEntity)netMech, (NetEntity)netPart, false));

       // UpdateUserInterface(uid, component);
    }

    /// <summary>
    /// Attempts to change the amount of energy in the mech.
    /// </summary>
    /// <param name="uid">The mech itself</param>
    /// <param name="delta">The change in energy</param>
    /// <param name="component"></param>
    /// <returns>If the energy was successfully changed.</returns>
    public virtual bool TryChangeEnergy(EntityUid uid, FixedPoint2 delta, AltMechComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (component.Energy + delta < 0)
            return false;

        component.Energy = FixedPoint2.Clamp(component.Energy + delta, 0, component.MaxEnergy);
        Dirty(uid, component);
        //UpdateUserInterface(uid, component);
        return true;
    }

    /// <summary>
    /// Sets the integrity of the mech.
    /// </summary>
    /// <param name="uid">The mech itself</param>
    /// <param name="value">The value the integrity will be set at</param>
    /// <param name="component"></param>
    public void SetIntegrity(EntityUid uid, FixedPoint2 value, AltMechComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.Integrity = FixedPoint2.Clamp(value, 0, component.MaxIntegrity);

        if (component.Integrity <= 0)
        {
            BreakMech(uid, component);
        }
        else if (component.Broken)
        {
            component.Broken = false;
            UpdateAppearance(uid, component);
        }

        Dirty(uid, component);
        //UpdateUserInterface(uid, component);
    }

    /// <summary>
    /// Checks if the pilot is present
    /// </summary>
    /// <param name="component"></param>
    /// <param name="uid"></param>
    /// <returns>Whether or not the pilot is present</returns>

    //SS220-AddMechToClothing-start
    public bool IsEmpty(AltMechComponent component, EntityUid uid)
    {
        if (HasComp<MechRobotComponent>(uid))
            return component.PilotSlot.ContainedEntity == null;

        return true;
    }
    //SS220-AddMechToClothing-end

    /// <summary>
    /// Checks if an entity can be inserted into the mech.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="toInsert"></param>
    /// <param name="component"></param>
    /// <returns></returns>
    public bool CanInsert(EntityUid uid, EntityUid toInsert, AltMechComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        return IsEmpty(component, uid) && _actionBlocker.CanMove(toInsert); //SS220-AddMechToClothing
    }

    /// <summary>
    /// Updates the user interface
    /// </summary>
    /// <remarks>
    /// This is defined here so that UI updates can be accessed from shared.
    /// </remarks>
    //public virtual void UpdateUserInterface(EntityUid uid, AltMechComponent? component = null)
    //{
    //}

    /// <summary>
    /// Attempts to insert a pilot into the mech.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="toInsert"></param>
    /// <param name="component"></param>
    /// <returns>Whether or not the entity was inserted</returns>
    public bool TryInsert(EntityUid uid, EntityUid? toInsert, AltMechComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (toInsert == null || component.PilotSlot.ContainedEntity == toInsert)
            return false;

        if (TryComp<InventoryComponent>(toInsert, out var inventoryComp))
        {
            foreach (var slot in component.SlotsToDrop)
            {
                _inventory.TryUnequip((EntityUid)toInsert, slot);
            }    
        }

        if (!CanInsert(uid, toInsert.Value, component))
            return false;

        SetupUser(uid, toInsert.Value);
        _container.Insert(toInsert.Value, component.PilotSlot);

        if (TryComp<ArmorBlockComponent>(uid, out var blockComp))
            blockComp.Owner = toInsert;

        UpdateAppearance(uid, component);
        return true;
    }

    /// <summary>
    /// Attempts to eject the current pilot from the mech
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    /// <returns>Whether or not the pilot was ejected.</returns>
    public bool TryEject(EntityUid uid, AltMechComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (component.PilotSlot.ContainedEntity == null)
            return false;

        var pilot = component.PilotSlot.ContainedEntity.Value;

        RemoveUser(uid, pilot);
        _container.RemoveEntity(uid, pilot);

        if (TryComp<ArmorBlockComponent>(uid, out var blockComp))
            blockComp.Owner = null;

        return true;
    }

    private void OnGetMeleeWeapon(EntityUid uid, AltMechPilotComponent component, GetMeleeWeaponEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<MechComponent>(component.Mech, out var mech))
            return;

        var weapon = mech.CurrentSelectedEquipment ?? component.Mech;
        args.Weapon = weapon;
        args.Handled = true;
    }

    private void OnCanAttackFromContainer(EntityUid uid, AltMechPilotComponent component, CanAttackFromContainerEvent args)
    {
        args.CanAttack = true;
    }

    private void OnAttackAttempt(EntityUid uid, AltMechPilotComponent component, AttackAttemptEvent args)
    {
        if (args.Target == component.Mech)
            args.Cancel();
    }

    private void UpdateAppearance(EntityUid uid, AltMechComponent? component = null,
        AppearanceComponent? appearance = null)
    {
        if (!Resolve(uid, ref component, ref appearance, false))
            return;

        _appearance.SetData(uid, MechVisuals.Open, IsEmpty(component, uid), appearance); //SS220-AddMechToClothing
        _appearance.SetData(uid, MechVisuals.Broken, component.Broken, appearance);
    }

    private void OnDragDrop(EntityUid uid, AltMechComponent component, ref DragDropTargetEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var doAfterEventArgs = new DoAfterArgs(EntityManager, args.Dragged, component.EntryDelay, new MechEntryEvent(), uid, target: uid)
        {
            BreakOnMove = true,
        };

        _doAfter.TryStartDoAfter(doAfterEventArgs);
    }

    private void OnCanDragDrop(EntityUid uid, AltMechComponent component, ref CanDropTargetEvent args)
    {
        args.Handled = true;

        //SS220-AddMechToClothing-start
        if (!HasComp<MechRobotComponent>(uid))
            return;
        //SS220-AddMechToClothing-end

        args.CanDrop |= !component.Broken && CanInsert(uid, args.Dragged, component);
    }

}

[ByRefEvent]
public readonly record struct MechPartInsertedEvent(EntityUid Mech)
{
    public readonly EntityUid Mech = Mech;
}

[Serializable, NetSerializable]
public sealed partial class MechPartInsertedDoAfterEvent : SimpleDoAfterEvent
{

}

[ByRefEvent]
public readonly record struct MechPartRemovedEvent(EntityUid Mech)
{
    public readonly EntityUid Mech = Mech;
}

[ByRefEvent]
public readonly record struct MechSpeedModifiedEvent(EntityUid Mech)
{
    public readonly EntityUid Mech = Mech;
}

[ByRefEvent]
public readonly record struct OnMechExitEvent();

[ByRefEvent]
public readonly record struct MassChangedEvent();

public enum PartSlot : byte
{
    Core = 0,
    Head = 1,
    RightArm = 2,
    LeftArm = 3,
    Chassis = 4,
    Power = 5
}

[Serializable, NetSerializable]
public sealed class MechPartStatusChanged : EntityEventArgs
{
    public NetEntity Mech;
    public NetEntity Part;
    public bool Attached;

    public MechPartStatusChanged(NetEntity mech, NetEntity part, bool attached)
    {
        Mech = mech;
        Part = part;
        Attached = attached;
    }
}

