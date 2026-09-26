// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT
using Content.Shared.Damage;

namespace Content.Shared.SS220.Weapons.Ranged.Events;

[ByRefEvent]
public record struct ProjectileBlockAttemptEvent(EntityUid ProjUid, DamageSpecifier Damage, bool Cancelled = false)
{
    public DamageSpecifier Damage = Damage; //Yeah-yeah, that COULD be more beutiful, BUT it wouldn't allow me to use it by-ref
    public Color? hitMarkColor = Color.Red;
}
