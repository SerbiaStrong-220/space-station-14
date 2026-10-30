// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Clothing.Events;

/// <summary>
/// Raised on an item before applying an optical effect or eye coverage.
/// Cancel to prevent the item from contributing that effect.
/// </summary>
[ByRefEvent]
public record struct GlassesEffectAttemptEvent(bool Cancelled = false);
