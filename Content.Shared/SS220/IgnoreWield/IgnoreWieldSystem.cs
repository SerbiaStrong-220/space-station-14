// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Clothing.Components;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Containers;

namespace Content.Shared.SS220.IgnoreWield;

public sealed class IgnoreWieldSystem : EntitySystem
{
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<IgnoreWieldOnEquipComponent, GotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<IgnoreWieldOnEquipComponent, GotUnequippedEvent>(OnUnequipped);

        SubscribeLocalEvent<IgnoreWieldComponent, ComponentRemove>(OnCompRemove);
        SubscribeLocalEvent<IgnoreWieldComponent, ComponentInit>(OnCompInit);
        SubscribeLocalEvent<IgnoreWieldComponent, DidEquipHandEvent>(OnDidEquipHand);
        SubscribeLocalEvent<IgnoreWieldComponent, DidUnequipHandEvent>(OnDidUnequipHand);

        SubscribeLocalEvent<GunWieldBonusComponent, EntGotInsertedIntoContainerMessage>(OnEntGotInsertedIntoContainer);
        SubscribeLocalEvent<GunWieldBonusComponent, EntGotRemovedFromContainerMessage>(OnEntGotRemovedFromContainer);
    }

    private void OnEntGotInsertedIntoContainer(Entity<GunWieldBonusComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        if (HasComp<GunComponent>(ent))
            _gun.RefreshModifiers(ent.Owner);
    }

    private void OnEntGotRemovedFromContainer(Entity<GunWieldBonusComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        if (HasComp<GunComponent>(ent))
            _gun.RefreshModifiers(ent.Owner);
    }

    private void OnDidUnequipHand(Entity<IgnoreWieldComponent> ent, ref DidUnequipHandEvent args)
    {
        if (HasComp<GunWieldBonusComponent>(args.Unequipped))
            _gun.RefreshModifiers(args.Unequipped);
    }

    private void OnDidEquipHand(Entity<IgnoreWieldComponent> ent, ref DidEquipHandEvent args)
    {
        if (HasComp<GunWieldBonusComponent>(args.Equipped))
            _gun.RefreshModifiers(args.Equipped);
    }

    private void OnCompInit(Entity<IgnoreWieldComponent> ent, ref ComponentInit args)
    {
        RefreshWieldModifiers(ent);
    }

    private void OnCompRemove(Entity<IgnoreWieldComponent> ent, ref ComponentRemove args)
    {
        RefreshWieldModifiers(ent);
    }

    private void OnUnequipped(Entity<IgnoreWieldOnEquipComponent> ent, ref GotUnequippedEvent args)
    {
        var slots = _inventory.GetSlotEnumerator(args.EquipTarget);
        while (slots.NextItem(out var worn))
        {
            if (worn == ent.Owner)
                continue;

            if (!HasComp<IgnoreWieldOnEquipComponent>(worn))
                continue;

            if (!TryComp<ClothingComponent>(worn, out var otherClothingComp))
                continue;

            if ((args.SlotFlags & otherClothingComp.Slots) == 0)
                continue;

            break;
        }

        if (!TryComp<ClothingComponent>(ent, out var clothingComp))
            return;

        if ((args.SlotFlags & clothingComp.Slots) != 0)
            RemComp<IgnoreWieldComponent>(args.EquipTarget);
    }

    private void OnEquipped(Entity<IgnoreWieldOnEquipComponent> ent, ref GotEquippedEvent args)
    {
        if (!TryComp<ClothingComponent>(ent, out var clothingComp))
            return;

        if ((args.SlotFlags & clothingComp.Slots) == 0)
            return;

        EnsureComp<IgnoreWieldComponent>(args.EquipTarget, out var comp);
        comp.IgnoreWeaponStats = ent.Comp.IgnoreWeaponStats;
    }

    private void RefreshWieldModifiers(EntityUid entity)
    {
        var held = _hands.EnumerateHeld(entity);

        foreach (var item in held)
        {
            if (HasComp<GunWieldBonusComponent>(item))
                _gun.RefreshModifiers(item);
        }
    }
}
