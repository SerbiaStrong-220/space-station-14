// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
namespace Content.Shared.SS220.Weapons.Ranged.Events;

[ByRefEvent]
public record struct HitscanAttempt(EntityUid User, bool Cancelled = false);
