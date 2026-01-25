namespace Content.Shared.Weapons.Melee.Events;

/// <summary>
/// Raised directed on a weapon when attempt a melee attack.
/// </summary>
[ByRefEvent]
public record struct AttemptMeleeEvent(EntityUid User, bool Cancelled = false, string? Message = null);

// SS220-Extend Weapon Logic-Start
[ByRefEvent]
public record struct AttemptMeleeUserEvent(EntityUid Weapon, bool Cancelled = false, string? Message = null);
// SS220-Extend Weapon Logic-End
