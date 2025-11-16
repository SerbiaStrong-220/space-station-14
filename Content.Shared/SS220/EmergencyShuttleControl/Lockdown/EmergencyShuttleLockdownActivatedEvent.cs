// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.EmergencyShuttleControl.Lockdown;

/// <summary>
///     This is invoked when the <see cref="EmergencyShuttleLockdownComponent"/> has been activated by EntitySystem.
/// </summary>
[ByRefEvent]
public record struct EmergencyShuttleLockdownActivatedEvent;
