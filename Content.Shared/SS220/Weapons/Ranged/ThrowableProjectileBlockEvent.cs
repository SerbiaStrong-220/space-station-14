// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT
using Content.Shared.Damage;

namespace Content.Shared.SS220.Weapons.Ranged.Events;


[ByRefEvent]
public record struct ThrowableProjectileBlockAttemptEvent(DamageSpecifier? Damage, EntityUid DamageDealer)
{
    public bool CancelledHit = false;

    public DamageSpecifier? Damage = Damage;
}
