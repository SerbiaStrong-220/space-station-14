namespace Content.Shared.Weapons.Hitscan.Events;

[ByRefEvent]
public record struct AttemptHitscanRaycastHitEvent(EntityUid HitScanEntity, bool Cancelled = false);
