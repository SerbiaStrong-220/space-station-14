namespace Content.Server.Shuttles.Events;

/// <summary>
/// Raised before an early emergency shuttle launch is authorized.
/// </summary>
[ByRefEvent]
public record struct EmergencyShuttleEarlyLaunchAttemptEvent(bool Cancelled)
{
}
