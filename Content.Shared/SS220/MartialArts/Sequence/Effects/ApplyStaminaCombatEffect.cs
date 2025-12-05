// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage.Systems;

namespace Content.Shared.SS220.MartialArts.Sequence.Effects;

public sealed partial class ApplyStaminaCombatEffect : CombatSequenceEffect
{
    [DataField(required: true)]
    public float Damage;

    [DataField]
    public bool IgnoreResist = false;

    public override void Execute(EntityUid user, EntityUid target, MartialArtistComponent artist)
    {
        var stamina = Entity.System<SharedStaminaSystem>();

        stamina.TakeStaminaDamage(target, Damage, source: user, visual: false, ignoreResist: IgnoreResist);
    }
}
