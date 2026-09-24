using Content.Shared.Emp;
using Content.Shared.Kitchen;
using Content.Shared.Power;
using Content.Shared.PowerCell.Components;
using Content.Shared.Rejuvenate;
using Robust.Shared.Containers; //SS220-IPC

namespace Content.Shared.PowerCell;

public sealed partial class PowerCellSystem
{
    [Dependency] private SharedContainerSystem _container = default!; //SS220-IPC

    public void InitializeRelay()
    {
        SubscribeLocalEvent<PowerCellSlotComponent, BeingMicrowavedEvent>(RelayToCell);
        SubscribeLocalEvent<PowerCellSlotComponent, RejuvenateEvent>(RelayToCell);
        SubscribeLocalEvent<PowerCellSlotComponent, GetChargeEvent>(RelayToCell);
        SubscribeLocalEvent<PowerCellSlotComponent, ChangeChargeEvent>(RelayToCell);

        SubscribeLocalEvent<PowerCellComponent, EmpAttemptEvent>(RelayToCellSlot); // Prevent the ninja from EMPing its own battery
        SubscribeLocalEvent<PowerCellComponent, ChargeChangedEvent>(RelayToCellSlot);
        SubscribeLocalEvent<PowerCellComponent, BatteryStateChangedEvent>(RelayToCellSlot); // For shutting down devices if the battery is empty
        SubscribeLocalEvent<PowerCellComponent, RefreshChargeRateEvent>(RelayToCellSlot); // Allow devices to charge/drain inserted batteries
    }

    private void RelayToCell<T>(Entity<PowerCellSlotComponent> ent, ref T args) where T : notnull
    {
        if (!_itemSlots.TryGetSlot(ent.Owner, ent.Comp.CellSlotId, out var slot) || !slot.Item.HasValue)
            return;

        // Relay the event to the power cell.
        RaiseLocalEvent(slot.Item.Value, ref args);
    }

    private void RelayToCellSlot<T>(Entity<PowerCellComponent> ent, ref T args) where T : notnull
    {
        //SS220-IPC begin
        if (!_container.TryGetContainingContainer((ent.Owner, null, null), out var container))
            return;

        if (!TryComp<PowerCellSlotComponent>(container.Owner, out var slotComp))
            return;

        // Ensure that the battery is placed specifically in the device's power slot, 
        // rather than in some other container (such as a pocket or bag).
        if (container.ID != slotComp.CellSlotId)
            return;
        //SS220-IPC end

        RaiseLocalEvent(container.Owner, ref args);
    }
}
