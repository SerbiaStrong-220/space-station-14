namespace Content.Shared.Weapons.Melee.Events;

[ByRefEvent]
public record struct AttemptMeleeUserEvent(EntityUid Weapon, bool Cancelled = false, string? Message = null);
