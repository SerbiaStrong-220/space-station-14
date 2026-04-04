// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT
using Content.Shared.Damage;
using Content.Shared.Projectiles;

namespace Content.Shared.FCB.Weapons.Ranged.Events;

[ByRefEvent]
public record struct ProjectileBlockAttemptEvent(EntityUid ProjUid, ProjectileComponent Component, bool Cancelled, DamageSpecifier damage)
{
    public bool CancelledHit = false;

    public Color? hitMarkColor = Color.Red;

    public DamageSpecifier? Damage = damage;
}
