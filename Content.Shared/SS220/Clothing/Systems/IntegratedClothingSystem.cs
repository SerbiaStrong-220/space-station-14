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

public sealed partial class IntegratedClothingSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedContainerSystem _containerSystem = default!;
    [Dependency] private InventorySystem _inventorySystem = default!;
    [Dependency] private SharedPopupSystem _popupSystem = default!;

    private static readonly LocId CannotPutIntegratedClothingOn = "integrated-clothing-cannot-put-on";
    private static readonly LocId MustRemoveClothingFirst = "toggleable-clothing-remove-first";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IntegratedClothingComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<IntegratedClothingComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<IntegratedClothingComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<IntegratedClothingComponent, BeingEquippedAttemptEvent>(OnEquipAttempt);
        SubscribeLocalEvent<IntegratedClothingComponent, ClothingGotEquippedEvent>(OnGotEquipped);
        SubscribeLocalEvent<IntegratedClothingComponent, GotUnequippedEvent>(OnIntegratedUnequip);

        SubscribeLocalEvent<IntegratedToClothingComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<IntegratedToClothingComponent, GotUnequippedEvent>(OnAttachedUnequip);
        SubscribeLocalEvent<IntegratedToClothingComponent, ComponentRemove>(OnRemoveAttached);
        SubscribeLocalEvent<IntegratedToClothingComponent, BeingUnequippedAttemptEvent>(OnAttachedUnequipAttempt);
    }

    private void OnEquipAttempt(Entity<IntegratedClothingComponent> ent, ref BeingEquippedAttemptEvent args)
    {
        foreach (var slot in ent.Comp.Slots)
        {
            if (!_inventorySystem.TryGetSlotEntity(args.EquipTarget, slot, out var wornEnt) || wornEnt == null)
                continue;

            _popupSystem.PopupClient(Loc.GetString(CannotPutIntegratedClothingOn, ("entity", wornEnt)), args.User);
            args.Cancel();
            return;
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

    private void OnIntegratedUnequip(Entity<IntegratedClothingComponent> ent, ref GotUnequippedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        foreach (var slot in ent.Comp.Slots)
        {
            if (!ent.Comp.Containers.TryGetValue(slot, out var containerSlot))
                continue;

            if (containerSlot != null && containerSlot.ContainedEntity == null && ent.Comp.ClothingUids.ContainsKey(slot))
                _inventorySystem.TryUnequip(args.EquipTarget, slot, force: true, triggerHandContact: true);
        }
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
        if (!TryComp<ClothingComponent>(ent.Owner, out var clothingComp) || clothingComp.InSlot == null)
            return;

        if (_timing.ApplyingState)
            return;

        if (ent.Comp.LifeStage > ComponentLifeStage.Running)
            return;

        if (!TryComp(ent.Comp.AttachedUid, out IntegratedClothingComponent? integratedComp))
            return;

        if (LifeStage(ent.Comp.AttachedUid) > EntityLifeStage.MapInitialized)
            return;

        if (integratedComp.ClothingUids.TryGetValue(clothingComp.InSlot, out var entUid) && integratedComp.Containers.TryGetValue(clothingComp.InSlot, out var container))
            _containerSystem.Insert(entUid, container);
    }

    private void ToggleClothing(EntityUid user, Entity<IntegratedClothingComponent> ent)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        foreach (var slot in ent.Comp.Slots)
        {
            if (!ent.Comp.Containers.TryGetValue(slot, out var containerSlot) || !ent.Comp.ClothingUids.TryGetValue(slot, out var uid))
                continue;

            if (containerSlot.ContainedEntity == null)
            {
                _inventorySystem.TryUnequip(user, user, slot, force: true);
                continue;
            }

            if (_inventorySystem.TryGetSlotEntity(user, slot, out var existing))
            {
                _popupSystem.PopupClient(Loc.GetString(MustRemoveClothingFirst, ("entity", user)), user, user);
                continue;
            }

            _inventorySystem.TryEquip(user, user, uid, slot, triggerHandContact: true, force: true);
        }
    }

    private void OnInit(Entity<IntegratedClothingComponent> ent, ref ComponentInit args)
    {
        foreach (var key in ent.Comp.Slots)
            ent.Comp.Containers.Add(key, _containerSystem.EnsureContainer<ContainerSlot>(ent.Owner, ent.Comp.ContainerId + "-" + key));
    }

    private void OnShutdown(Entity<IntegratedClothingComponent> ent, ref ComponentShutdown args)
    {
        foreach (var (key, value) in ent.Comp.ClothingUids)
            PredictedQueueDel(value);
    }

    private void OnMapInit(Entity<IntegratedClothingComponent> ent, ref MapInitEvent args)
    {
        foreach (var slot in ent.Comp.Slots)
        {
            if (ent.Comp.Containers[slot]!.ContainedEntity is { } entity)
            {
                DebugTools.Assert(ent.Comp.ClothingUids[slot] == entity, "Unexpected entity present inside of a integrated clothing container.");
                return;
            }

            if (ent.Comp.ClothingUids.TryGetValue(slot, out var entInSlot))
            {
                DebugTools.Assert(Exists(entInSlot), "Integrated clothing is missing expected entity.");
                DebugTools.Assert(TryComp(entInSlot, out IntegratedToClothingComponent? comp), "Integrated clothing is missing an attached component");
                DebugTools.Assert(comp?.AttachedUid == ent.Owner, "Integrated clothing uid mismatch");
            }
            else
            {
                var xform = Transform(ent.Owner);

                ent.Comp.ClothingUids.Add(slot, Spawn(ent.Comp.ClothingPrototypes[slot], xform.Coordinates));
                var attachedClothing = EnsureComp<IntegratedToClothingComponent>(ent.Comp.ClothingUids[slot]);

                attachedClothing.AttachedUid = ent.Owner;
                Dirty(ent.Comp.ClothingUids[slot], attachedClothing);

                _containerSystem.Insert(ent.Comp.ClothingUids[slot], ent.Comp.Containers[slot], containerXform: xform);
                Dirty(ent);
            }
        }
    }
}
