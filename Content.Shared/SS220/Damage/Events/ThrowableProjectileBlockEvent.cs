// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Damage;

namespace Content.Shared.Weapons.Ranged.Events;


[ByRefEvent]
public record struct ThrowableProjectileBlockAttemptEvent(DamageSpecifier? damage)
{
    public bool Cancelled = false;

    public DamageSpecifier? Damage = damage;
}
