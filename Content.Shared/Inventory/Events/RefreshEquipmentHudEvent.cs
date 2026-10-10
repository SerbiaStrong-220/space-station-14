using Content.Shared.SS220.SiliconComponents;

namespace Content.Shared.Inventory.Events;

[ByRefEvent]
public record struct RefreshEquipmentHudEvent<T>(SlotFlags TargetSlots) : IInventoryRelayEvent, ISiliconPartRelayEvent //SS220 add synthetic
    where T : IComponent
{
    public SlotFlags TargetSlots { get; } = TargetSlots;
    public bool Active = false;
    public List<T> Components = new();

    PartType ISiliconPartRelayEvent.Parts => PartType.Optics; //SS220 add synthetic
}
