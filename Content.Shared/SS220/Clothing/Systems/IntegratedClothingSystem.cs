// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT
using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Popups;
using Content.Shared.SS220.Clothing.Components;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.Clothing.Systems;

public sealed class IntegratedClothingSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IntegratedClothingComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<IntegratedClothingComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<IntegratedClothingComponent, BeingEquippedAttemptEvent>(OnEquipAttempt);
        SubscribeLocalEvent<IntegratedClothingComponent, ClothingGotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<IntegratedClothingComponent, GotUnequippedEvent>(OnToggleableUnequip);

        SubscribeLocalEvent<IntegratedToClothingComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<IntegratedToClothingComponent, GotUnequippedEvent>(OnAttachedUnequip);
        SubscribeLocalEvent<IntegratedToClothingComponent, ComponentRemove>(OnRemoveAttached);
        SubscribeLocalEvent<IntegratedToClothingComponent, BeingUnequippedAttemptEvent>(OnAttachedUnequipAttempt);
    }

    private void OnEquipAttempt(Entity<IntegratedClothingComponent> ent, ref BeingEquippedAttemptEvent args)
    {
        if (_inventorySystem.TryGetSlotEntity(args.EquipTarget, ent.Comp.Slot, out var wornEnt) && wornEnt != null)
            args.Cancel();
    }

    private void OnGotEquipped(Entity<IntegratedClothingComponent> ent, ref ClothingGotEquippedEvent args)
    {
        ToggleClothing(args.Wearer, ent);
    }

    private void OnInteractHand(Entity<IntegratedToClothingComponent> ent, ref InteractHandEvent args)
    {
        args.Handled = true;
    }

    /// <summary>
    ///     Called when the suit is unequipped, to ensure that the helmet also gets unequipped.
    /// </summary>
    private void OnToggleableUnequip(Entity<IntegratedClothingComponent> ent, ref GotUnequippedEvent args)
    {
        // If it's a part of PVS departure then don't handle it.
        if (_timing.ApplyingState)
            return;

        // If the attached clothing is not currently in the container, this just assumes that it is currently equipped.
        // This should maybe double check that the entity currently in the slot is actually the attached clothing, but
        // if its not, then something else has gone wrong already...
        if (ent.Comp.Container != null && ent.Comp.Container.ContainedEntity == null && ent.Comp.ClothingUid != null)
            _inventorySystem.TryUnequip(args.EquipTarget, ent.Comp.Slot, force: true, triggerHandContact: true);
    }

    private void OnAttachedUnequipAttempt(Entity<IntegratedToClothingComponent> ent, ref BeingUnequippedAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnRemoveAttached(Entity<IntegratedToClothingComponent> ent, ref ComponentRemove args)
    {
        // if the attached component is being removed (maybe entity is being deleted?) we will just remove the
        // toggleable clothing component. This means if you had a hard-suit helmet that took too much damage, you would
        // still be left with a suit that was simply missing a helmet. There is currently no way to fix a partially
        // broken suit like this.

        if (!TryComp(ent.Comp.AttachedUid, out IntegratedClothingComponent? toggleComp))
            return;

        if (toggleComp.LifeStage > ComponentLifeStage.Running)
            return;

        RemComp(ent.Comp.AttachedUid, toggleComp);
    }

    /// <summary>
    ///     Called if the helmet was unequipped, to ensure that it gets moved into the suit's container.
    /// </summary>
    private void OnAttachedUnequip(Entity<IntegratedToClothingComponent> ent, ref GotUnequippedEvent args)
    {
        // Let containers worry about it.
        if (_timing.ApplyingState)
            return;

        if (ent.Comp.LifeStage > ComponentLifeStage.Running)
            return;

        if (!TryComp(ent.Comp.AttachedUid, out IntegratedClothingComponent? toggleComp))
            return;

        if (LifeStage(ent.Comp.AttachedUid) > EntityLifeStage.MapInitialized)
            return;

        // As unequipped gets called in the middle of container removal, we cannot call a container-insert without causing issues.
        // So we delay it and process it during a system update:
        if (toggleComp.ClothingUid != null && toggleComp.Container != null)
            _containerSystem.Insert(toggleComp.ClothingUid.Value, toggleComp.Container);
    }

    private void ToggleClothing(EntityUid user, Entity<IntegratedClothingComponent> ent)
    {
        if (ent.Comp.Container == null || ent.Comp.ClothingUid == null)
            return;

        var parent = Transform(ent.Owner).ParentUid;
        if (ent.Comp.Container.ContainedEntity == null)
            _inventorySystem.TryUnequip(user, parent, ent.Comp.Slot, force: true);
        else if (_inventorySystem.TryGetSlotEntity(parent, ent.Comp.Slot, out var existing))
        {
            _popupSystem.PopupClient(Loc.GetString("toggleable-clothing-remove-first", ("entity", existing)),
                user, user);
        }
        else
            _inventorySystem.TryEquip(user, parent, ent.Comp.ClothingUid.Value, ent.Comp.Slot, triggerHandContact: true);
    }

    private void OnInit(Entity<IntegratedClothingComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Container = _containerSystem.EnsureContainer<ContainerSlot>(ent.Owner, ent.Comp.ContainerId);
    }

    /// <summary>
    ///     On map init, either spawn the appropriate entity into the suit slot, or if it already exists, perform some
    ///     sanity checks. Also updates the action icon to show the toggled-entity.
    /// </summary>
    private void OnMapInit(Entity<IntegratedClothingComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.Container!.ContainedEntity is {} entity)
        {
            DebugTools.Assert(ent.Comp.ClothingUid == entity, "Unexpected entity present inside of a toggleable clothing container.");
            return;
        }

        if (ent.Comp.ClothingUid != null)
        {
            DebugTools.Assert(Exists(ent.Comp.ClothingUid), "Toggleable clothing is missing expected entity.");
            DebugTools.Assert(TryComp(ent.Comp.ClothingUid, out IntegratedToClothingComponent? comp), "Toggleable clothing is missing an attached component");
            DebugTools.Assert(comp?.AttachedUid == ent.Owner, "Toggleable clothing uid mismatch");
        }
        else
        {
            var xform = Transform(ent.Owner);
            ent.Comp.ClothingUid = Spawn(ent.Comp.ClothingPrototype, xform.Coordinates);
            var attachedClothing = EnsureComp<IntegratedToClothingComponent>(ent.Comp.ClothingUid.Value);
            attachedClothing.AttachedUid = ent.Owner;
            Dirty(ent.Comp.ClothingUid.Value, attachedClothing);
            _containerSystem.Insert(ent.Comp.ClothingUid.Value, ent.Comp.Container, containerXform: xform);
            Dirty(ent);
        }
    }
}
