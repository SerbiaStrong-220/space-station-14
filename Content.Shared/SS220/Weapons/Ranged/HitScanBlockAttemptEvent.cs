// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT
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
