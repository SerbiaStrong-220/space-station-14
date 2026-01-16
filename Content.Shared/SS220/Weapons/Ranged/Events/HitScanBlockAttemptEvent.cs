// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Damage;
using Content.Shared.FixedPoint;

namespace Content.Shared.Weapons.Ranged.Events;


[ByRefEvent]
public record struct HitscanBlockAttemptEvent(DamageSpecifier? damage)
{
    public bool CancelledHit = false;
    public DamageSpecifier? Damage = damage;
    public Color? hitColor = Color.Red;
}
