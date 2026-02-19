// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT
using Content.Shared.Access.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.FCB.AltBlocking;
using Content.Shared.FCB.ArmorBlock;
using Content.Shared.FCB.Mech.Components;
using Content.Shared.FCB.Mech.Equipment.Components;
using Content.Shared.FCB.Mech.Parts.Components;
using Content.Shared.FCB.Weapons.Melee.Events;
using Content.Shared.FCB.Weapons.Ranged.Events;
using Content.Shared.FixedPoint;
using Content.Shared.Flash;
using Content.Shared.Flash.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Mech;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.EntitySystems;
using Content.Shared.Mech.Equipment.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Radio.Components;
using Content.Shared.Random.Helpers;
using Content.Shared.Standing;
using Content.Shared.Storage.Components;
using Content.Shared.Weapons.Melee;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.FCB.Mech.Systems;

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
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly BlindableSystem _blindable = default!;


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

        SubscribeLocalEvent<AltMechComponent, MechPilotRelayedEvent<FlashAttemptEvent>>(OnPilotFlashed);
        SubscribeLocalEvent<AltMechComponent, FlashAttemptEvent>(OnMechFlashed);
        SubscribeLocalEvent<AltMechComponent, GetEyeProtectionEvent>(OnMechGetEyeProtection);

        SubscribeLocalEvent<AltMechPilotComponent, GetMeleeWeaponEvent>(OnGetMeleeWeapon);
        SubscribeLocalEvent<AltMechPilotComponent, AttackAttemptEvent>(OnAttackAttempt);

        SubscribeLocalEvent<AltMechComponent, ProjectileBlockAttemptEvent>(OnProjectileHit, after: [typeof(AltBlockingSystem)]);
        SubscribeLocalEvent<AltMechComponent, HitscanBlockAttemptEvent>(OnHitscan, after: [typeof(AltBlockingSystem)]);
        SubscribeLocalEvent<AltMechComponent, MeleeHitBlockAttemptEvent>(OnMeleeHit, after: [typeof(AltBlockingSystem)]);
        SubscribeLocalEvent<AltMechComponent, ThrowableProjectileBlockAttemptEvent>(OnThrownProjectileHit, after: [typeof(AltBlockingSystem)]);

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

        if (!TryEject(uid, component))
            return;

        var ev = new OnMechExitEvent();
        RaiseLocalEvent(uid, ref ev);
    }

    private void OnPilotFlashed(Entity<AltMechComponent> ent, ref MechPilotRelayedEvent<FlashAttemptEvent> args)
    {
        if (ent.Comp.ContainerDict["head"].ContainedEntity == null)
            return;

        if (TryComp<FlashImmunityComponent>(ent.Comp.ContainerDict["head"].ContainedEntity, out var immunityComp))
            args.Args.Cancelled = true;
    }

    private void OnMechFlashed(Entity<AltMechComponent> ent, ref FlashAttemptEvent args)
    {
        if (ent.Comp.ContainerDict["head"].ContainedEntity == null)
            return;

        if (TryComp<FlashImmunityComponent>(ent.Comp.ContainerDict["head"].ContainedEntity, out var immunityComp))
            args.Cancelled = true;
    }

    private void OnMechGetEyeProtection(Entity<AltMechComponent> ent, ref GetEyeProtectionEvent args)
    {
        if (ent.Comp.ContainerDict["head"].ContainedEntity == null)
            return;

        if (TryComp<EyeProtectionComponent>(ent.Comp.ContainerDict["head"].ContainedEntity, out var immunityComp))
            args.Protection += immunityComp.ProtectionTime;
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

    private void OnStartup(Entity<AltMechComponent> ent, ref ComponentStartup args)
    {
        foreach (var part in ent.Comp.ContainersToCreate)
            ent.Comp.ContainerDict[part] = _container.EnsureContainer<ContainerSlot>(ent.Owner, part);

        ent.Comp.PilotSlot = _container.EnsureContainer<ContainerSlot>(ent.Owner, ent.Comp.PilotSlotId);

        ent.Comp.TankSlot = _container.EnsureContainer<ContainerSlot>(ent.Owner, ent.Comp.TankSlotId);

        ent.Comp.OverallMass += ent.Comp.OwnMass;

        if(TryComp<MovementSpeedModifierComponent>(ent.Owner, out var movementComp))
            _movementSpeedModifier.ChangeBaseSpeed(ent.Owner, ent.Comp.OverallBaseMovementSpeed * 0.5f, ent.Comp.OverallBaseMovementSpeed, ent.Comp.OverallBaseAcceleration, movementComp);

        if(ent.Comp.ContainerDict["head"].ContainedEntity == null && !ent.Comp.Transparent)
        {
            TryComp<BlindableComponent>(ent.Owner, out var blindableComp);
            _blindable.AdjustEyeDamage((ent.Owner, blindableComp), 9); //Mech cannot see anything if it has no eyes
        }

        UpdateAppearance(ent.Owner, ent.Comp);
    }

    public virtual void OnStartupServer(Entity<AltMechComponent> ent)
    {

    }

    private void OnEntityStorageDump(Entity<AltMechComponent> entity, ref EntityStorageIntoContainerAttemptEvent args)
    {
        // There's no reason we should dump into /any/ of the mech's containers.
        args.Cancelled = true;
    }

    private void OnGetAdditionalAccess(Entity<AltMechComponent> ent, ref GetAdditionalAccessEvent args)
    {
        var pilot = ent.Comp.PilotSlot.ContainedEntity;
        if (pilot == null)
            return;

        args.Entities.Add(pilot.Value);
    }

    private void OnProjectileHit(Entity<AltMechComponent> ent, ref ProjectileBlockAttemptEvent args)
    {
        if (args.damage != null)
            AttackHandle(ent, args.damage, ref args.CancelledHit);
    }

    private void OnMeleeHit(Entity<AltMechComponent> ent, ref MeleeHitBlockAttemptEvent args)
    {
        MeleeAttackHandle(ent, ref args.CancelledHit, out var part);

        if(TryGetNetEntity(part, out var NetPart))
            args.blocker = NetPart;
    }

    private void OnHitscan(Entity<AltMechComponent> ent, ref HitscanBlockAttemptEvent args)
    {
        if(args.damage != null)
            AttackHandle(ent, args.damage, ref args.CancelledHit);
    }

    private void OnThrownProjectileHit(Entity<AltMechComponent> ent, ref ThrowableProjectileBlockAttemptEvent args)
    {
        if (args.damage != null)
            AttackHandle(ent, args.damage, ref args.CancelledHit);
    }

    private void AttackHandle(Entity<AltMechComponent> ent, DamageSpecifier damage, ref bool CancelledHit)
    {
        if (!TryGetNetEntity(ent.Owner, out var NetMech))
            return;

        foreach (var part in ent.Comp.ContainerDict)
        {
            if (part.Key == "power" || part.Value == null || part.Value.ContainedEntity == null)
                continue;

            if (!TryGetNetEntity(part.Value.ContainedEntity, out var NetItem))
                continue;

            if (SharedRandomExtensions.PredictedProb(_timing, 0.16f, (NetEntity)NetMech, (NetEntity)NetItem))//this chance is hardcoded because using mech parts as shields is not planned, it's just a patch to make it work untill part damage UI is made 
            {
                _damageable.TryChangeDamage((EntityUid)part.Value.ContainedEntity, damage);
                CancelledHit = true;
                return;
            }
        }
    }

    private void MeleeAttackHandle(Entity<AltMechComponent> ent, ref bool CancelledHit, out EntityUid? targetedPart)
    {
        if (!TryGetNetEntity(ent.Owner, out var NetMech))
        {
            targetedPart = null;
            return;
        }

        foreach (var part in ent.Comp.ContainerDict)
        {
            if (part.Key == "power" || part.Value == null || part.Value.ContainedEntity != null)
                continue;

            if (!TryGetNetEntity(part.Value.ContainedEntity, out var NetItem))
                continue;

            if (SharedRandomExtensions.PredictedProb(_timing, 0.16f, (NetEntity)NetMech, (NetEntity)NetItem))//this chance is hardcoded because using mech parts as shields is not planned, it's just a patch to make it work untill part damage UI is made
            {
                CancelledHit = true;
                targetedPart = part.Value.ContainedEntity;
                return;
            }
        }

        targetedPart = null;
    }

    private void SetupUser(EntityUid mech, EntityUid pilot, AltMechComponent? component = null)
    {
        if (!Resolve(mech, ref component))
            return;

        var rider = EnsureComp<AltMechPilotComponent>(pilot);

        rider.Mech = mech;

        if (_net.IsClient)
            return;

        var ev = new DropHandItemsEvent();
        RaiseLocalEvent(pilot, ref ev);

        if(TryComp<ActiveRadioComponent>(mech, out var mechRadio))
        {
            if(TryComp<InventoryComponent>(pilot, out var pilotInventory) && _inventory.TryGetSlotContainer(pilot, "ears", out var slot, out var def))
            {
                if (!TryComp<ActiveRadioComponent>(slot.ContainedEntity, out var radioComp))
                    return;
                mechRadio.Channels = radioComp.Channels;
            }
            if(TryComp<ActiveRadioComponent>(pilot, out var embeddedRadio))//in case the pilot is a radio himself
            {
                foreach ( var channel in embeddedRadio.Channels)
                    mechRadio.Channels.Add(channel);
            }
        }

        _actions.AddAction(pilot, ref component.MechUiActionEntity, component.MechUiAction, mech);
        _actions.AddAction(pilot, ref component.MechEjectActionEntity, component.MechEjectAction, mech);
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
        AltMechEquipmentComponent? equipmentComponent = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!Resolve(toInsert, ref equipmentComponent))
            return;

        if (component.MaxEquipmentAmount < component.CurrentEquipmentAmount + equipmentComponent.EqipmentSize)
            return;

        if (_whitelistSystem.IsWhitelistFail(component.EquipmentWhitelist, toInsert))
            return;

        component.CurrentEquipmentAmount += equipmentComponent.EqipmentSize;

        AddMass(component, equipmentComponent.OwnMass);

        _container.Insert(toInsert, component.EquipmentContainer);

        equipmentComponent.EquipmentOwner = uid;

        _container.Insert(toInsert, component.EquipmentContainer);
        var ev = new MechEquipmentInsertedEvent(uid);
        RaiseLocalEvent(toInsert, ref ev);
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

        if ((partComponent.slot == "left-arm" || partComponent.slot == "right-arm") && partComponent.OwnMass > component.MaximalArmMass)
        {
            _popup.PopupEntity(Loc.GetString("mech-arm-too-heavy"), uid);
            return;
        }

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
            RaiseNetworkEvent(new MechPartStatusChanged((NetEntity)netMech, (NetEntity)netPart, true, partComponent.slot));
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
        string? slot = null;

        if(partComponent != null)
        {
            slot = partComponent.slot;
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
            RaiseNetworkEvent( new MechPartStatusChanged((NetEntity)netMech, (NetEntity)netPart, false, slot));

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
    public virtual void SetIntegrity(EntityUid uid, FixedPoint2 value, AltMechComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.Integrity = FixedPoint2.Clamp(value, 0, component.MaxIntegrity);

        if (component.Integrity <= 0)
        {

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
    public bool IsEmpty(AltMechComponent component)
    {
        return component.PilotSlot.ContainedEntity == null;
    }

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

        return IsEmpty(component) && !component.Bolted;
    }

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

        var ev = new OnMechEntryEvent();
        RaiseLocalEvent(uid, ref ev);

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
    public virtual bool TryEject(EntityUid uid, AltMechComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (component.PilotSlot.ContainedEntity == null || component.Bolted)
            return false;

        var pilot = component.PilotSlot.ContainedEntity.Value;

        //RemoveUser(uid, pilot);
        if (!RemComp<AltMechPilotComponent>(pilot))
            return false;

        if (TryComp<ActiveRadioComponent>(uid, out var mechRadio))
        {
            mechRadio.Channels.Clear();
        }

        _actions.RemoveProvidedActions(pilot, uid);
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

        _appearance.SetData(uid, MechVisuals.Open, IsEmpty(component), appearance);
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

        args.CanDrop = CanInsert(uid, args.Dragged, component);
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
public readonly record struct OnMechEntryEvent();

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

public sealed class MechPartStatusChanged : EntityEventArgs // not a by ref event because it is made for networking
{
    public NetEntity Mech;
    public NetEntity Part;
    public bool Attached;
    public string? Slot;

    public MechPartStatusChanged(NetEntity mech, NetEntity part, bool attached, string? slot)
    {
        Mech = mech;
        Part = part;
        Attached = attached;
        Slot = slot;
    }
}

[Serializable, NetSerializable]
public sealed partial class InsertPartEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class InsertEquipmentEvent : SimpleDoAfterEvent
{
}

[ByRefEvent]
public readonly record struct MechEquipmentInsertedEvent(EntityUid Mech)
{
    public readonly EntityUid Mech = Mech;
}

[ByRefEvent]
public readonly record struct MechEquipmentRemovedEvent(EntityUid Mech)
{
    public readonly EntityUid Mech = Mech;
}


