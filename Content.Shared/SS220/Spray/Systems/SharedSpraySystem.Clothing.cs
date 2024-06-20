using System.Diagnostics.CodeAnalysis;
using Content.Shared.Inventory;
using Content.Shared.SS220.Spray;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Shared.SS220.Spray.System;

public partial class SharedSpraySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;

    private void InitializeClothing()
    {
        SubscribeLocalEvent<ClothingSlotAmmoProviderComponent, TakeAmmoEvent>(OnClothingTakeAmmo);
        SubscribeLocalEvent<ClothingSlotAmmoProviderComponent, GetAmmoCountEvent>(OnClothingAmmoCount);
    }

    private void OnClothingTakeAmmo(EntityUid uid, ClothingSlotAmmoProviderComponent component, TakeAmmoEvent args)
    {
        if (!TryGetClothingSlotEntity(uid, component, out var entity))
            return;
        RaiseLocalEvent(entity.Value, args);
    }

    private void OnClothingAmmoCount(EntityUid uid, ClothingSlotAmmoProviderComponent component, ref GetAmmoCountEvent args)
    {
        if (!TryGetClothingSlotEntity(uid, component, out var entity))
            return;
        RaiseLocalEvent(entity.Value, ref args);
    }

    private bool TryGetClothingSlotEntity(EntityUid uid, ClothingSlotAmmoProviderComponent component, [NotNullWhen(true)] out EntityUid? slotEntity)
    {
        slotEntity = null;

        if (!Containers.TryGetContainingContainer(uid, out var container))
            return false;
        var user = container.Owner;

        if (!_inventory.TryGetContainerSlotEnumerator(user, out var enumerator, component.TargetSlot))
            return false;

        while (enumerator.NextItem(out var item))
        {
            if (component.ProviderWhitelist == null || !component.ProviderWhitelist.IsValid(item, EntityManager))
                continue;

            slotEntity = item;
            return true;
        }

        return false;
    }
}

