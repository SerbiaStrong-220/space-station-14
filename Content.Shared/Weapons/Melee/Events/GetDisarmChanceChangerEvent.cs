// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.Weapons.Melee.Events;

/// <summary>
/// Event raised on disarmer amd disarmed entity to get <see cref="Multiplier"/>
/// </summary>
/// <param name="Multiplier">this could like chance to not disarm...</param>
[ByRefEvent]
public record struct GetDisarmChanceMultiplierEvent(EntityUid Disarmer, EntityUid Disarmed, EntityUid? InTargetHand, float Multiplier);
