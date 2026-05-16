// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT
using Content.Shared.Damage;
using Content.Shared.Projectiles;

namespace Content.Shared.SS220.Weapons.Ranged.Events;

[ByRefEvent]
public record struct ProjectileBlockAttemptEvent(EntityUid ProjUid, bool Cancelled = false, DamageSpecifier damage)
{
    public Color? hitMarkColor = Color.Red;
}
