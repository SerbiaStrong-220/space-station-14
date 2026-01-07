using Content.Shared.Damage;
using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Ranged.Events;


[ByRefEvent]
public record struct HitscanBlockAttemptEvent(DamageSpecifier? damage)
{
    public bool CancelledHit = false;
    public DamageSpecifier? Damage = damage;
    public Color? hitColor = Color.Red;
}
