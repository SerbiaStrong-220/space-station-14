// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.StatusEffect;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.MartialArts.Sequence.Effects;

// TODO: when wizards will fully migrate to new status effects, we have to migrate this too
public sealed partial class ApplyStatusEffectCombatEffect : CombatSequenceEffect
{
    [DataField("effect", required: true)]
    public ProtoId<StatusEffectPrototype> StatusEffect;

    [DataField]
    public TimeSpan Time = TimeSpan.FromSeconds(10);

    [DataField]
    public TimeSpan? TimeLimit = null;

    [DataField]
    public bool Refresh = false;

    public override void Execute(EntityUid user, EntityUid target, MartialArtistComponent artist)
    {
        var status = Entity.System<StatusEffectsSystem>();

        var targetTime = Time;

        if (TimeLimit != null && status.TryGetTime(target, StatusEffect, out var time))
        {
            var curTime = time.Value.Item2 - time.Value.Item1;
            targetTime = curTime + Time < TimeLimit.Value ? curTime + Time : TimeLimit.Value;
        }

        status.TryAddStatusEffect(target, StatusEffect, targetTime, Refresh);
    }
}
