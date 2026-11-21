// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Body;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;

namespace Content.Shared.SS220.Body;

public sealed partial class LimbSystem
{
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedItemSystem _item = default!;

    private static readonly LocId MissingOrganEquipReason = "limb-equip-missing-organ";

    private void InitializeWornSlots()
    {
        SubscribeLocalEvent<OrganWornSlotsComponent, OrganRemovedFromEvent>(OnWornSlotOrganRemoved);
        SubscribeLocalEvent<OrganWornSlotsComponent, OrganInsertedIntoEvent>(OnWornSlotOrganInserted);
        SubscribeLocalEvent<OrganWornSlotsComponent, IsEquippingTargetAttemptEvent>(OnEquipAttempt);
    }

    /// <summary>
    /// Slots whose organs are all missing. A slot with any of its organs left keeps the clothes, they are drawn without the missing part.
    /// </summary>
    private SlotFlags GetLostSlots(Entity<OrganWornSlotsComponent> ent)
    {
        var worn = SlotFlags.NONE;
        var present = SlotFlags.NONE;
        foreach (var (category, slots) in ent.Comp.Slots)
        {
            worn |= slots;
            if (_body.HasOrganCategory(ent.Owner, category))
                present |= slots;
        }

        return worn & ~present;
    }

    private void OnWornSlotOrganRemoved(Entity<OrganWornSlotsComponent> ent, ref OrganRemovedFromEvent args)
    {
        if (TerminatingOrDeleted(ent) || !TryGetOrganSlots(ent, args.Organ, out var slots))
            return;

        var lost = slots & GetLostSlots(ent);
        if (!_net.IsClient && lost != SlotFlags.NONE && _inventory.TryGetContainerSlotEnumerator(ent.Owner, out var enumerator, lost))
        {
            while (enumerator.NextItem(out _, out var slot))
            {
                _inventory.TryUnequip(ent, slot.Name, silent: true, force: true);
            }
        }

        RefreshWornVisuals(ent, slots);
    }

    private void OnWornSlotOrganInserted(Entity<OrganWornSlotsComponent> ent, ref OrganInsertedIntoEvent args)
    {
        if (TryGetOrganSlots(ent, args.Organ, out var slots))
            RefreshWornVisuals(ent, slots);
    }

    private void OnEquipAttempt(Entity<OrganWornSlotsComponent> ent, ref IsEquippingTargetAttemptEvent args)
    {
        if (args.Cancelled || (args.SlotFlags & GetLostSlots(ent)) == SlotFlags.NONE)
            return;

        args.Reason = MissingOrganEquipReason;
        args.Cancel();
    }

    private bool TryGetOrganSlots(Entity<OrganWornSlotsComponent> ent, EntityUid organ, out SlotFlags slots)
    {
        slots = SlotFlags.NONE;
        return TryComp<OrganComponent>(organ, out var organComp)
            && organComp.Category is { } category
            && ent.Comp.Slots.TryGetValue(category, out slots);
    }

    // clothes left on the remaining organ are redrawn with or without the missing part mask
    private void RefreshWornVisuals(Entity<OrganWornSlotsComponent> ent, SlotFlags slots)
    {
        if (!_inventory.TryGetContainerSlotEnumerator(ent.Owner, out var enumerator, slots))
            return;

        while (enumerator.NextItem(out var item))
        {
            _item.VisualsChanged(item);
        }
    }
}
