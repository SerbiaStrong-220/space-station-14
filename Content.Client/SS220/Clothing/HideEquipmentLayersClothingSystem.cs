// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.Clothing;
using Content.Shared.Clothing;
using Content.Shared.Inventory;
using Content.Shared.SS220.Clothing;
using Content.Shared.SS220.Clothing.Components;
using Content.Shared.Whitelist;

namespace Content.Client.SS220.Clothing;

public sealed class HideEquipmentLayersClothingSystem : EntitySystem
{
    [Dependency] private readonly ClientClothingSystem _clothing = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InventoryComponent, BeforeRenderEquipmentEvent>(OnBeforeRender);
        SubscribeLocalEvent<HideEquipmentLayersClothingComponent, ClothingGotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<HideEquipmentLayersClothingComponent, ClothingGotUnequippedEvent>(OnUnequipped);
    }

    private void OnBeforeRender(Entity<InventoryComponent> ent, ref BeforeRenderEquipmentEvent args)
    {
        // A cloak in a pocket hides nothing.
        var worn = _inventory.GetSlotEnumerator(ent.AsNullable(), SlotFlags.WITHOUT_POCKET);
        while (worn.NextItem(out var item))
        {
            if (TryComp<HideEquipmentLayersClothingComponent>(item, out var hide)
                && hide.Slots.Contains(args.Slot)
                && _whitelist.CheckBoth(args.Equipment, hide.Blacklist, hide.Whitelist))
                args.Cancel();
        }
    }

    private void OnEquipped(Entity<HideEquipmentLayersClothingComponent> ent, ref ClothingGotEquippedEvent args)
    {
        Rerender(args.Wearer);
    }

    private void OnUnequipped(Entity<HideEquipmentLayersClothingComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        Rerender(args.Wearer);
    }

    private void Rerender(EntityUid wearer)
    {
        if (TryComp<InventoryComponent>(wearer, out var inventory))
            _clothing.InitClothing(wearer, inventory);
    }
}
