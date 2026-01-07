using Content.Shared.Damage;

namespace Content.Shared.Weapons.Ranged.Events;


[ByRefEvent]
public record struct ThrowableProjectileBlockAttemptEvent(DamageSpecifier? damage)
{
    public bool Cancelled = false;
    public DamageSpecifier? Damage = damage;
}
