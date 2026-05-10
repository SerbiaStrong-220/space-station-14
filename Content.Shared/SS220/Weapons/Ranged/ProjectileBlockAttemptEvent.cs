// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT
using Content.Shared.Damage;
using Content.Shared.Projectiles;

namespace Content.Shared.SS220.Weapons.Ranged.Events;

[ByRefEvent]
public record struct ProjectileBlockAttemptEvent(EntityUid ProjUid, bool Cancelled, DamageSpecifier damage, Angle ProjAngle)
{
    public bool CancelledHit = false;

    public Color? hitMarkColor = Color.Red;

    public DamageSpecifier? Damage = damage;

    public Angle ProjectileRotation = ProjAngle;
}
