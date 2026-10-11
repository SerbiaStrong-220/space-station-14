namespace Content.Shared.Spillable;

// SS220-felinid-pipecrawl
/// <summary>
/// Raised on the entity a puddle would come from: the user spilling a container, or a bleeding entity.
/// </summary>
[ByRefEvent]
public record struct SpillAttemptEvent(bool Cancelled);
