// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Content.Shared.Damage;

[ByRefEvent]
public record struct MeleeAttackerEvent(EntityUid used, EntityUid target, DamageSpecifier damage)
{
    public EntityUid Used = used;
    public EntityUid Target = target;
    public DamageSpecifier Damage = damage;
    public DamageSpecifier ModifiedDamage = new DamageSpecifier();
}
