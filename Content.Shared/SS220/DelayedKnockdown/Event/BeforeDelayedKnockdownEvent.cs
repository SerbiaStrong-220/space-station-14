using Content.Shared.Inventory;

namespace Content.Shared.SS220.DelayedKnockdown;

/// <summary>
/// Raised before delyed knockdown attempt to allow other systems to cancel or modify it.
/// </summary>
[ByRefEvent]
public record struct BeforeDelayedKnockdownEvent(float Value, bool Cancelled = false) : IInventoryRelayEvent
{
    SlotFlags IInventoryRelayEvent.TargetSlots => ~SlotFlags.POCKET;
}
