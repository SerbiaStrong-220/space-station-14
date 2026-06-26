// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT
using Content.Shared.Access.Components;
using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Flash;
using Content.Shared.Flash.Components;
using Content.Shared.Gravity;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Mech;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.EntitySystems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Radio.Components;
using Content.Shared.Random.Helpers;
using Content.Shared.SprayPainter.Components;
using Content.Shared.SS220.AltBlocking;
using Content.Shared.SS220.ArmorBlock;
using Content.Shared.SS220.Mech.Components;
using Content.Shared.SS220.Mech.Equipment.Components;
using Content.Shared.SS220.Mech.Parts.Components;
using Content.Shared.SS220.Weapons.Melee.Events;
using Content.Shared.SS220.Weapons.Ranged.Events;
using Content.Shared.Standing;
using Content.Shared.Storage.Components;
using Content.Shared.Weapons.Melee;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.Mech.Systems;

public abstract partial class SharedAltMechSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly BlindableSystem _blindable = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<AltMechComponent, MechEjectPilotEvent>(OnEjectPilotEvent);

        SubscribeLocalEvent<AltMechComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<AltMechComponent, EntityStorageIntoContainerAttemptEvent>(OnEntityStorageDump);
        SubscribeLocalEvent<AltMechComponent, GetAdditionalAccessEvent>(OnGetAdditionalAccess);
        SubscribeLocalEvent<AltMechComponent, DragDropTargetEvent>(OnDragDrop);
        SubscribeLocalEvent<AltMechComponent, CanDropTargetEvent>(OnCanDragDrop);

        SubscribeLocalEvent<AltMechComponent, MechPilotRelayedEvent<FlashAttemptEvent>>(OnPilotFlashed);
        SubscribeLocalEvent<AltMechComponent, FlashAttemptEvent>(OnMechFlashed);
        SubscribeLocalEvent<AltMechComponent, GetEyeProtectionEvent>(OnMechGetEyeProtection);
        SubscribeLocalEvent<AltMechComponent, AfterInteractUsingEvent>(OnMechInteractedWith);

        SubscribeLocalEvent<AltMechPilotComponent, GetMeleeWeaponEvent>(OnGetMeleeWeapon);
        SubscribeLocalEvent<AltMechPilotComponent, AttackAttemptEvent>(OnAttackAttempt);

        SubscribeLocalEvent<AltMechComponent, IsWeightlessEvent>(OnWeightlessCheck);

        SubscribeLocalEvent<AltMechComponent, ProjectileBlockAttemptEvent>(OnProjectileHit, after: [typeof(SharedAltBlockingSystem)]);
        SubscribeLocalEvent<AltMechComponent, HitscanBlockAttemptEvent>(OnHitscan, after: [typeof(SharedAltBlockingSystem)]);
        SubscribeLocalEvent<AltMechComponent, MeleeHitBlockAttemptEvent>(OnMeleeHit, after: [typeof(SharedAltBlockingSystem)]);
        SubscribeLocalEvent<AltMechComponent, ThrowableProjectileBlockAttemptEvent>(OnThrownProjectileHit, after: [typeof(SharedAltBlockingSystem)]);

        InitializeRelay();
    }

    private void OnEjectPilotEvent(EntityUid uid, AltMechComponent component, MechEjectPilotEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        var ev = new OnMechExitEvent();
        RaiseLocalEvent(uid, ref ev);
    }

    private void OnPilotFlashed(Entity<AltMechComponent> ent, ref MechPilotRelayedEvent<FlashAttemptEvent> args)
    {
        if (TryComp<FlashImmunityComponent>(ent.Owner, out var _))
        {
            args.Args.Cancelled = true;
            return;
        }
        RelayRefToParts(ent, ref args);
        RelayRefToEquipment(ent, ref args);
    }

    private void OnMechFlashed(Entity<AltMechComponent> ent, ref FlashAttemptEvent args)
    {
        if (TryComp<FlashImmunityComponent>(ent.Owner, out var _))
        {
            args.Cancelled = true;
            return;
        }
        RelayRefToParts(ent, ref args);
        RelayRefToEquipment(ent, ref args);
    }

    private void OnMechGetEyeProtection(Entity<AltMechComponent> ent, ref GetEyeProtectionEvent args)
    {
        if (ent.Comp.ContainerDict["head"].ContainedEntity == null)
            return;

        if (TryComp<EyeProtectionComponent>(ent.Comp.ContainerDict["head"].ContainedEntity, out var immunityComp))
            args.Protection += immunityComp.ProtectionTime;
    }

    private void OnStartup(Entity<AltMechComponent> ent, ref ComponentStartup args)
    {
        foreach (var part in ent.Comp.ContainersToCreate)
            ent.Comp.ContainerDict[part] = _container.EnsureContainer<ContainerSlot>(ent.Owner, part);

        ent.Comp.PilotSlot = _container.EnsureContainer<ContainerSlot>(ent.Owner, ent.Comp.PilotSlotId);

        ent.Comp.TankSlot = _container.EnsureContainer<ContainerSlot>(ent.Owner, ent.Comp.TankSlotId);

        ent.Comp.EquipmentContainer = _container.EnsureContainer<Container>(ent.Owner, ent.Comp.EquipmentContainerId);

        ent.Comp.OverallMass += ent.Comp.OwnMass;

        ent.Comp.Integrity = ent.Comp.MaxIntegrity;

        if (TryComp<MovementSpeedModifierComponent>(ent.Owner, out var movementComp))
            _movementSpeedModifier.ChangeBaseSpeed(ent.Owner, ent.Comp.OverallBaseMovementSpeed * 0.5f, ent.Comp.OverallBaseMovementSpeed, ent.Comp.OverallBaseAcceleration, movementComp);

        if (ent.Comp.ContainerDict["head"].ContainedEntity == null && !ent.Comp.Transparent)
        {
            TryComp<BlindableComponent>(ent.Owner, out var blindableComp);
            _blindable.AdjustEyeDamage((ent.Owner, blindableComp), 9); //Mech cannot see anything if it has no eyes
        }

        _actions.AddAction(ent.Owner, ref ent.Comp.MechUiActionEntity, ent.Comp.MechUiAction, ent.Owner);
        _actions.AddAction(ent.Owner, ref ent.Comp.MechEjectActionEntity, ent.Comp.MechEjectAction, ent.Owner);

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

    protected virtual void OnMechInteractedWith(Entity<AltMechComponent> ent, ref AfterInteractUsingEvent args)
    {
        if (!TryComp<SprayPainterComponent>(args.Used, out var painterComp) || painterComp.SelectedDecalColor == null)
            return;

        if (painterComp.SelectedDecalColor != null)
        {
            ent.Comp.ColoredSpriteColor = (Color)painterComp.SelectedDecalColor;
            return;
        }

        if (painterComp.ColorPalette.ContainsKey(painterComp.PickedColor))
            ent.Comp.ColoredSpriteColor = painterComp.ColorPalette[painterComp.PickedColor];
    }

    private void OnProjectileHit(Entity<AltMechComponent> ent, ref ProjectileBlockAttemptEvent args)
    {
        if (args.Damage != null)
            args.Cancelled = AttackHandle(ent, args.Damage);
    }

    private void OnMeleeHit(Entity<AltMechComponent> ent, ref MeleeHitBlockAttemptEvent args)
    {
        if (MeleeAttackHandle(ent, out var part) && part is { Valid: true } partValidated)
        {
            args.Blocker = partValidated;
            args.Cancelled = true;
        }
    }

    private void OnHitscan(Entity<AltMechComponent> ent, ref HitscanBlockAttemptEvent args)
    {
        if (args.Damage != null)
            args.Cancelled = AttackHandle(ent, args.Damage);
    }

    private void OnThrownProjectileHit(Entity<AltMechComponent> ent, ref ThrowableProjectileBlockAttemptEvent args)
    {
        if (args.Damage != null)
            args.Cancelled = AttackHandle(ent, args.Damage);
    }

    private bool AttackHandle(Entity<AltMechComponent> ent, DamageSpecifier damage)
    {
        if (!TryGetNetEntity(ent.Owner, out var NetMech))
            return false;

        foreach (var part in ent.Comp.ContainerDict)
        {
            if (part.Key == "power" || part.Value == null || part.Value.ContainedEntity == null)
                continue;

            if (!TryGetNetEntity(part.Value.ContainedEntity, out var NetItem))
                continue;

            //if (SharedRandomExtensions.PredictedProb(_timing, 0.16f, (NetEntity)NetMech, (NetEntity)NetItem))//this chance is hardcoded because using mech parts as shields is not planned, it's just a patch to make it work untill part damage UI is made

            if (SharedRandomExtensions.PredictedProb(_timing, 0.16f, (NetEntity)NetItem))
            {
                _damageable.TryChangeDamage((EntityUid)part.Value.ContainedEntity, damage);
                return true;
            }
        }

        return false;
    }

    private bool MeleeAttackHandle(Entity<AltMechComponent> ent, out EntityUid? targetedPart)
    {
        if (!TryGetNetEntity(ent.Owner, out var NetMech))
        {
            targetedPart = null;
            return false;
        }

        foreach (var part in ent.Comp.ContainerDict)
        {
            if (part.Key == "power" || part.Value == null || part.Value.ContainedEntity != null)
                continue;

            if (!TryGetNetEntity(part.Value.ContainedEntity, out var NetItem))
                continue;

            //if (SharedRandomExtensions.PredictedProb(_timing, 0.16f, (NetEntity)NetMech, (NetEntity)NetItem))//this chance is hardcoded because using mech parts as shields is not planned, it's just a patch to make it work untill part damage UI is made

            if (SharedRandomExtensions.PredictedProb(_timing, 0.16f, (NetEntity)NetItem))
            {
                targetedPart = part.Value.ContainedEntity;
                return true;
            }
        }

        targetedPart = null;
        return false;
    }


    private void SetupUser(Entity<AltMechComponent> mech, EntityUid pilot)
    {
        var pilotComp = EnsureComp<AltMechPilotComponent>(pilot);

        pilotComp.Mech = mech;

        if (TryComp<BlindableComponent>(pilot, out var blindableCompPilot))
        {
            pilotComp.PilotEyeDamage = blindableCompPilot.EyeDamage;
            _blindable.AdjustEyeDamage(pilot, 9 - blindableCompPilot.EyeDamage);
        }

        if (_net.IsClient)
            return;

        var ev = new DropHandItemsEvent();
        RaiseLocalEvent(pilot, ref ev);

        if (TryComp<ActiveRadioComponent>(mech, out var mechRadio))
        {
            if (TryComp<InventoryComponent>(pilot, out var pilotInventory) && _inventory.TryGetSlotContainer(pilot, "ears", out var slot, out var def))
            {
                if (!TryComp<ActiveRadioComponent>(slot.ContainedEntity, out var radioComp))
                    return;
                mechRadio.Channels = radioComp.Channels;
            }
            if (TryComp<ActiveRadioComponent>(pilot, out var embeddedRadio))//in case the pilot is a radio himself
            {
                foreach (var channel in embeddedRadio.Channels)
                    mechRadio.Channels.Add(channel);
            }
        }

        _actions.AddAction(pilot, ref pilotComp.PilotUiActionEntity, pilotComp.PilotUiAction, mech);
        _actions.AddAction(pilot, ref pilotComp.PilotEjectActionEntity, pilotComp.PilotEjectAction, mech);
    }

    /// <summary>
    /// Destroys the mech, removing the user and ejecting anything contained.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    public virtual void BreakMech(Entity<AltMechComponent> ent)
    {
        TryEject(ent);
        var equipment = new List<EntityUid>(ent.Comp.EquipmentContainer.ContainedEntities);

        ent.Comp.Broken = true;
        UpdateAppearance(ent.Owner, ent.Comp);
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

        equipmentComponent.EquipmentOwner = uid;

        Dirty(uid, component);
        Dirty(toInsert, equipmentComponent);

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
    public void RemoveEquipment(EntityUid uid, EntityUid toRemove, bool forced = false)
    {
        if (!TryComp<AltMechComponent>(uid, out var mechComp))
            return;
        // When forced, we also want to handle the possibility that the "equipment" isn't actually equipment.
        // This /shouldn't/ be possible thanks to OnEntityStorageDump, but there's been quite a few regressions
        // with entities being hardlock stuck inside mechs.
        if (!TryComp<AltMechEquipmentComponent>(toRemove, out var equipmentComponent))
            return;

        if (equipmentComponent.EquipmentOwner != uid)
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

        if (equipmentComponent != null)
        {
            mechComp.CurrentEquipmentAmount -= equipmentComponent.EqipmentSize;
            equipmentComponent.EquipmentOwner = null;
            Dirty(uid, mechComp);
            Dirty(toRemove, equipmentComponent);
        }

        _container.Remove(toRemove, mechComp.EquipmentContainer);
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

        if (TryGetNetEntity(uid, out var netMech) && TryGetNetEntity(toRemove, out var netPart))
            RaiseNetworkEvent(new MechPartStatusChanged((NetEntity)netMech, (NetEntity)netPart, false, slot));
    }

    /// <summary>
    /// Attempts to change the amount of energy in the mech.
    /// </summary>
    /// <param name="uid">The mech itself</param>
    /// <param name="delta">The change in energy</param>
    /// <param name="component"></param>
    /// <returns>If the energy was successfully changed.</returns>
    public virtual bool TryChangeEnergy(Entity<AltMechComponent> ent, FixedPoint2 delta)
    {
        if (!HasComp<AltMechComponent>(ent))
            return false;

        if (ent.Comp.Energy + delta < 0)
            return false;

        ent.Comp.Energy = FixedPoint2.Clamp(ent.Comp.Energy + delta, 0, ent.Comp.MaxEnergy);
        Dirty(ent);
        return true;
    }

    /// <summary>
    /// Sets the integrity of the mech.
    /// </summary>
    /// <param name="uid">The mech itself</param>
    /// <param name="value">The value the integrity will be set at</param>
    /// <param name="component"></param>
    public virtual void SetIntegrity(Entity<AltMechComponent> ent, FixedPoint2 value)
    {
        ent.Comp.Integrity = FixedPoint2.Clamp(value, 0, ent.Comp.MaxIntegrity);

        if (ent.Comp.Integrity <= 0)
        {

        }
        else if (ent.Comp.Broken)
        {
            ent.Comp.Broken = false;
            UpdateAppearance(ent);
        }

        Dirty(ent);
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
    public bool CanInsert(Entity<AltMechComponent> ent, EntityUid toInsert)
    {
        if (!HasComp<AltMechComponent>(ent))
            return false;

        return IsEmpty(ent.Comp) && !ent.Comp.Bolted;
    }

    /// <summary>
    /// Attempts to insert a pilot into the mech.
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="toInsert"></param>
    /// <param name="component"></param>
    /// <returns>Whether or not the entity was inserted</returns>
    public bool TryInsert(Entity<AltMechComponent> ent, EntityUid? toInsert)
    {
        if (toInsert == null || ent.Comp.PilotSlot.ContainedEntity == toInsert)
            return false;

        if (TryComp<InventoryComponent>(toInsert, out var inventoryComp))
        {
            foreach (var slot in ent.Comp.SlotsToDrop)
            {
                _inventory.TryUnequip((EntityUid)toInsert, slot);
            }
        }

        if (!CanInsert(ent, toInsert.Value))
            return false;

        SetupUser(ent, toInsert.Value);
        _container.Insert(toInsert.Value, ent.Comp.PilotSlot);

        var ev = new OnMechEntryEvent();
        RaiseLocalEvent(ent, ref ev);

        if (TryComp<ArmorBlockComponent>(ent, out var blockComp))
            blockComp.User = toInsert;

        UpdateAppearance(ent);
        return true;
    }

    /// <summary>
    /// Attempts to eject the current pilot from the mech
    /// </summary>
    /// <param name="uid"></param>
    /// <param name="component"></param>
    /// <returns>Whether or not the pilot was ejected.</returns>
    public virtual bool TryEject(Entity<AltMechComponent> ent)
    {
        if (ent.Comp.PilotSlot.ContainedEntity == null || (ent.Comp.Bolted && !ent.Comp.BoltsSawed))
            return false;

        var pilot = ent.Comp.PilotSlot.ContainedEntity.Value;

        if (!TryComp<AltMechPilotComponent>(pilot, out var pilotComp))
            return false;

        if (TryComp<ActiveRadioComponent>(ent.Owner, out var mechRadio))
        {
            mechRadio.Channels.Clear();
        }

        if (pilotComp.PilotUiActionEntity != null)
            _actions.RemoveProvidedAction(pilot, ent.Owner, (EntityUid)pilotComp.PilotUiActionEntity);

        if (pilotComp.PilotEjectActionEntity != null)
            _actions.RemoveProvidedAction(pilot, ent.Owner, (EntityUid)pilotComp.PilotEjectActionEntity);

        _container.RemoveEntity(ent.Owner, pilot);

        if (TryComp<BlindableComponent>(pilot, out var blindableCompPilot))
            _blindable.AdjustEyeDamage(pilot, pilotComp.PilotEyeDamage - blindableCompPilot.EyeDamage);

        if (!RemComp<AltMechPilotComponent>(pilot))
            return false;

        if (TryComp<ArmorBlockComponent>(ent.Owner, out var blockComp))
            blockComp.User = null;

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

    public void OnWeightlessCheck(Entity<AltMechComponent> ent, ref IsWeightlessEvent args)
    {
        RelayRefToParts(ent, ref args);
        RelayRefToEquipment(ent, ref args);
    }

    private void OnCanDragDrop(Entity<AltMechComponent> ent, ref CanDropTargetEvent args)
    {
        args.Handled = true;

        args.CanDrop = CanInsert(ent, args.Dragged);
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
public sealed partial class MechBoltsSawedEvent : SimpleDoAfterEvent
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

[ByRefEvent]
public record struct RefreshOpticHudEvent<T>() where T : IComponent
{
    public bool Active = false;
    public List<T> Components = new();
}

public enum MechPartVisualLayers : byte
{
    Core = 0,
    CoreColored = 1,
    Head = 2,
    HeadColored = 3,
    Chassis = 4,
    ChassisColored = 5,
    RightArm = 6,
    RightArmColored = 7,
    LeftArm = 8,
    LeftArmColored = 9,
    Power = 10,
    PowerColored = 11
}
