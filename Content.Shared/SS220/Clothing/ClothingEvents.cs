namespace Content.Shared.SS220.Clothing;

/// <summary>
///     Raised on both the equipee and the equipment before clothing visuals are collected and rendered
///     for a given slot. Cancelling prevents this slot's clothing layers from being rendered at all.
/// </summary>
public sealed class BeforeRenderEquipmentEvent(EntityUid equipee, EntityUid equipment, string slot) : CancellableEntityEventArgs
{
    public readonly EntityUid Equipee = equipee;
    public readonly EntityUid Equipment = equipment;
    public readonly string Slot = slot;
}