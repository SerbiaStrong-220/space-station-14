// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.SS220.Clothing;
using Content.Shared.SS220.Clothing.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.Clothing;

public sealed class HideEquipmentLayersClothingSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    private const string NeckSlot = "neck";
    private static readonly string[] HiddenSlots = ["back", "suitstorage"];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InventoryComponent, BeforeRenderEquipmentEvent>(OnBeforeRender);
        SubscribeLocalEvent<InventoryComponent, ClothingDidUnequippedEvent>(OnUnequipped);
        SubscribeLocalEvent<EquipmentVisualsUpdatedEvent>(OnVisualsUpdated);
    }

    private void OnBeforeRender(Entity<InventoryComponent> ent, ref BeforeRenderEquipmentEvent args)
    {
        if (HiddenSlots.Contains(args.Slot)
            && _inventory.TryGetSlotEntity(ent, NeckSlot, out var worn, ent)
            && TryGetHide(worn.Value, out var hide)
            && _whitelist.CheckBoth(args.Equipment, hide.Blacklist, hide.Whitelist))
            args.Cancel();
    }

    private void OnUnequipped(Entity<InventoryComponent> ent, ref ClothingDidUnequippedEvent args)
    {
        if (args.Clothing.Comp.InSlot == NeckSlot)
            Rerender(ent);
    }

    // The neck item was redrawn: it was equipped, or a chameleon changed its disguise.
    private void OnVisualsUpdated(EquipmentVisualsUpdatedEvent args)
    {
        if (args.Slot == NeckSlot)
            Rerender(args.Equipee);
    }

    private void Rerender(EntityUid wearer)
    {
        foreach (var slot in HiddenSlots)
        {
            if (_inventory.TryGetSlotEntity(wearer, slot, out var item))
                _item.VisualsChanged(item.Value);
        }
    }

    // A chameleon item hides whatever its disguise would.
    private bool TryGetHide(EntityUid uid, [NotNullWhen(true)] out HideEquipmentLayersClothingComponent? hide)
    {
        if (TryComp(uid, out hide))
            return true;

        return TryComp<ChameleonClothingComponent>(uid, out var chameleon)
               && _prototype.TryIndex(chameleon.Default, out var proto)
               && proto.TryGetComponent(out hide, Factory);
    }
}
