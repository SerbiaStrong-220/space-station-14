// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT
using Content.Shared.Clothing;
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
    private static readonly string CannotPutIntegratedClothingOn = "integrated-clothing-cannot-put-on";
    private static readonly string MustRemoveClothingFirst = "toggleable-clothing-remove-first";

    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IntegratedClothingComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<IntegratedClothingComponent, ComponentShutdown>(OnShutdown);
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
        {
            _popupSystem.PopupClient(Loc.GetString(CannotPutIntegratedClothingOn, ("entity", wornEnt)),
                args.User);
            args.Cancel();
        }
    }

    private void OnGotEquipped(Entity<IntegratedClothingComponent> ent, ref ClothingGotEquippedEvent args)
    {
        ToggleClothing(args.Wearer, ent);
    }

    private void OnInteractHand(Entity<IntegratedToClothingComponent> ent, ref InteractHandEvent args)
    {
        args.Handled = true;
    }

    private void OnToggleableUnequip(Entity<IntegratedClothingComponent> ent, ref GotUnequippedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if (ent.Comp.Container != null && ent.Comp.Container.ContainedEntity == null && ent.Comp.ClothingUid != null)
            _inventorySystem.TryUnequip(args.EquipTarget, ent.Comp.Slot, force: true, triggerHandContact: true);
    }

    private void OnAttachedUnequipAttempt(Entity<IntegratedToClothingComponent> ent, ref BeingUnequippedAttemptEvent args)
    {
        args.Cancel();
    }

    private void OnRemoveAttached(Entity<IntegratedToClothingComponent> ent, ref ComponentRemove args)
    {
        if (!TryComp(ent.Comp.AttachedUid, out IntegratedClothingComponent? toggleComp))
            return;

        if (toggleComp.LifeStage > ComponentLifeStage.Running)
            return;

        RemComp(ent.Comp.AttachedUid, toggleComp);
    }


    private void OnAttachedUnequip(Entity<IntegratedToClothingComponent> ent, ref GotUnequippedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if (ent.Comp.LifeStage > ComponentLifeStage.Running)
            return;

        if (!TryComp(ent.Comp.AttachedUid, out IntegratedClothingComponent? toggleComp))
            return;

        if (LifeStage(ent.Comp.AttachedUid) > EntityLifeStage.MapInitialized)
            return;

        if (toggleComp.ClothingUid != null && toggleComp.Container != null)
            _containerSystem.Insert(toggleComp.ClothingUid.Value, toggleComp.Container);
    }

    private void ToggleClothing(EntityUid user, Entity<IntegratedClothingComponent> ent)
    {
        if (ent.Comp.Container == null || ent.Comp.ClothingUid == null)
            return;

        if (ent.Comp.Container.ContainedEntity == null)
        {
            _inventorySystem.TryUnequip(user, user, ent.Comp.Slot, force: true);
            return;
        }

        if (_inventorySystem.TryGetSlotEntity(user, ent.Comp.Slot, out var existing))
        {
            _popupSystem.PopupClient(Loc.GetString(MustRemoveClothingFirst, ("entity", user)),
                user, user);
            return;
        }

        _inventorySystem.TryEquip(user, user, ent.Comp.ClothingUid.Value, ent.Comp.Slot, triggerHandContact: true);
    }

    private void OnInit(Entity<IntegratedClothingComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Container = _containerSystem.EnsureContainer<ContainerSlot>(ent.Owner, ent.Comp.ContainerId);
    }

    private void OnShutdown(Entity<IntegratedClothingComponent> ent, ref ComponentShutdown args)
    {
        QueueDel(ent.Comp.ClothingUid);
    }

    private void OnMapInit(Entity<IntegratedClothingComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.Container!.ContainedEntity is { } entity)
        {
            DebugTools.Assert(ent.Comp.ClothingUid == entity, "Unexpected entity present inside of a integrated clothing container.");
            return;
        }

        if (ent.Comp.ClothingUid != null)
        {
            DebugTools.Assert(Exists(ent.Comp.ClothingUid), "Integrated clothing is missing expected entity.");
            DebugTools.Assert(TryComp(ent.Comp.ClothingUid, out IntegratedToClothingComponent? comp), "Integrated clothing is missing an attached component");
            DebugTools.Assert(comp?.AttachedUid == ent.Owner, "Integrated clothing uid mismatch");
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
