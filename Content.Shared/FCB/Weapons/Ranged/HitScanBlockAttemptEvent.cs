// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT
using Content.Shared.Damage;

namespace Content.Shared.FCB.Weapons.Ranged.Events;


[ByRefEvent]
public record struct HitscanBlockAttemptEvent(DamageSpecifier? damage)
{
    public bool CancelledHit = false;
    public DamageSpecifier? Damage = damage;
    public Color? hitColor = Color.Red;
}
