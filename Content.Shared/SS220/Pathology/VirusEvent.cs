// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Inventory;

namespace Content.Shared.SS220.Pathology;

[ByRefEvent]
public record struct VirusAddedAttempt(EntityUid Entity, VirusTransmissionVector Vector, bool Cancelled = false) : IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.WITHOUT_POCKET;
}
