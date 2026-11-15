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

public sealed partial class HideEquipmentLayersClothingSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedItemSystem _item = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    private const string NeckSlot = "neck";
    private static readonly string[] CoveredSlots = ["back", "suitstorage"];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InventoryComponent, BeforeRenderEquipmentEvent>(OnBeforeRenderEquipment);
        SubscribeLocalEvent<InventoryComponent, ClothingDidUnequippedEvent>(OnClothingUnequipped);
        SubscribeLocalEvent<EquipmentVisualsUpdatedEvent>(OnEquipmentVisualsUpdated);
    }

    private void OnBeforeRenderEquipment(Entity<InventoryComponent> wearer, ref BeforeRenderEquipmentEvent args) // чем гуще лес, тем if else if else
    {
        if (!CoveredSlots.Contains(args.Slot))
            return;

        if (!_inventory.TryGetSlotEntity(wearer, NeckSlot, out var neckItem, wearer))
            return;

        if (!TryGetHidingRules(neckItem.Value, out var hidingRules))
            return;

        if (!_whitelist.CheckBoth(
                args.Equipment,
                blacklist: hidingRules.Blacklist,
                whitelist: hidingRules.Whitelist))
            return;

        args.Cancel();
    }

    private void OnClothingUnequipped(Entity<InventoryComponent> wearer, ref ClothingDidUnequippedEvent args)
    {
        if (args.Clothing.Comp.InSlot == NeckSlot)
            RefreshCoveredEquipmentVisuals(wearer);
    }


    private void OnEquipmentVisualsUpdated(EquipmentVisualsUpdatedEvent args)
    {
        if (args.Slot == NeckSlot)
            RefreshCoveredEquipmentVisuals(args.Equipee);
    }

    private void RefreshCoveredEquipmentVisuals(EntityUid wearer)
    {
        foreach (var slot in CoveredSlots)
        {
            if (_inventory.TryGetSlotEntity(wearer, slot, out var item))
                _item.VisualsChanged(item.Value);
        }
    }

    private bool TryGetHidingRules(EntityUid neckItem, [NotNullWhen(true)] out HideEquipmentLayersClothingComponent? hidingRules)
    {
        if (TryComp(neckItem, out hidingRules))
            return true;

        if (!TryComp<ChameleonClothingComponent>(neckItem, out var chameleon))
            return false;

        if (!_prototype.TryIndex(chameleon.Default, out var disguisePrototype))
            return false;

        return disguisePrototype.TryGetComponent(out hidingRules, Factory);
    }
}
