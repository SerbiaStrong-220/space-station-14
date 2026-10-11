namespace Content.Shared.Climbing.Events;

// SS220-felinid-climb
/// <summary>
/// Raised on the climbing user to modify the climb do-after delay.
/// </summary>
[ByRefEvent]
public record struct GetClimbDelayModifierEvent(float Delay);
