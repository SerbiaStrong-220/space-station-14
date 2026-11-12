// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.Clothing;
using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Inventory;
using Content.Shared.SS220.Clothing;
using Content.Shared.SS220.Clothing.Components;

namespace Content.Client.SS220.Clothing;

public sealed class HideEquipmentLayersClothingSystem : EntitySystem
{
    [Dependency] private readonly ClientClothingSystem _clothing = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InventoryComponent, BeforeRenderEquipmentEvent>(OnBeforeRender);
        SubscribeLocalEvent<HideEquipmentLayersClothingComponent, ClothingGotEquippedEvent>(OnEquipped);
        SubscribeLocalEvent<HideEquipmentLayersClothingComponent, ClothingGotUnequippedEvent>(OnUnequipped);
    }

    private void OnBeforeRender(Entity<InventoryComponent> ent, ref BeforeRenderEquipmentEvent args)
    {
        if (args.Cancelled)
            return;

        var enumerator = _inventory.GetSlotEnumerator((ent.Owner, ent.Comp));
        while (enumerator.NextItem(out var item, out var slot))
        {
            // Only counts when actually worn (e.g. a cloak in a pocket shouldn't hide anything).
            if (item != args.Equipment
                && TryComp<HideEquipmentLayersClothingComponent>(item, out var hide)
                && hide.Slots.Contains(args.Slot)
                && TryComp<ClothingComponent>(item, out var clothing)
                && (clothing.Slots & slot.SlotFlags) != SlotFlags.NONE)
            {
                args.Cancel();
                return;
            }
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
