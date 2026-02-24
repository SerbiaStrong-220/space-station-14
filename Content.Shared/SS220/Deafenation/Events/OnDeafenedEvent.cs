using Content.Shared.Inventory;

namespace Content.Shared.SS220.Deafenation;

[ByRefEvent]
public record struct OnDeafenedEvent(float Range) : IInventoryRelayEvent
{
    public float SuppressionRange = Range;

    SlotFlags IInventoryRelayEvent.TargetSlots => SlotFlags.EARS | SlotFlags.HEAD;
}

public sealed class AreaNoiseEvent(float range, float distance, EntityUid target) : EntityEventArgs
{
    public float Range = range;       // Maximum range of the source event
    public float Distance = distance; // Distance from the source to the target
    public EntityUid Target = target; // The entity being processed
}
