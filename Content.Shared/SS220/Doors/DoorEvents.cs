// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Doors;

/// <summary>
/// Use this event to cancel sending message to every receiver
/// </summary>
[ByRefEvent]
public record struct DoorAccesAttemptEvent(EntityUid? User)
{
    public readonly EntityUid? User = User;
    public bool Cancelled = false;
}
