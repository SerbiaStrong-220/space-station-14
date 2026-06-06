using Content.Shared.Chat;

namespace Content.Shared.SS220.Body.Events;

/// <summary>
/// Raised on an entity to determine their hearing range multiplier.
/// </summary>
[ByRefEvent]
public record struct GetHearingRangeMultiplierEvent(InGameICChatType ChatType)
{
    public float Multiplier = 1f;
}
