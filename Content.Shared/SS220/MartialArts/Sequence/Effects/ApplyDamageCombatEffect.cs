// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage;

namespace Content.Shared.SS220.MartialArts.Sequence.Effects;

public sealed partial class ApplyDamageCombatEffect : CombatSequenceEffect
{
    [DataField(required: true)]
    public DamageSpecifier Damage = default!;

    public override void Execute(EntityUid user, EntityUid target, MartialArtistComponent artist)
    {
        var damageable = Entity.System<DamageableSystem>();
        damageable.TryChangeDamage(target, Damage, origin: user);
    }
}
