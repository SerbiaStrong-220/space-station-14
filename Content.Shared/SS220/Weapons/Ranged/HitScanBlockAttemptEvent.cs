// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT
using Content.Shared.Damage;

namespace Content.Shared.SS220.Weapons.Ranged.Events;


[ByRefEvent]
public record struct HitscanBlockAttemptEvent(DamageSpecifier? damage, Angle HitAngle)
{
    public bool CancelledHit = false;

    public DamageSpecifier? Damage = damage;

    public Color? hitColor = Color.Red;

    public Angle HitScanRotation = HitAngle;
}
