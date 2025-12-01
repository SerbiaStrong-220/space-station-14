using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.MartialArts.Sequence.Effects;

public sealed partial class ApplyStatusEffectCombatEffect : CombatSequenceEffect
{
    [DataField("effect", required: true)]
    public EntProtoId StatusEffect;

    [DataField]
    public TimeSpan Time = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Ignored if refresh is set to true
    /// </summary>
    [DataField]
    public TimeSpan? TimeLimit = null;

    [DataField]
    public bool Refresh = true;

    public override void Execute(EntityUid user, EntityUid target, MartialArtistComponent artist)
    {
        var status = Entity.System<StatusEffectsSystem>();

        if (Refresh)
        {
            status.TrySetStatusEffectDuration(target, StatusEffect, Time);
            return;
        }

        var targetTime = Time;

        if (status.TryGetTime(target, StatusEffect, out var effect))
        {
            var (_, endTime, startTime) = effect;

            if (endTime == null)
                return;

            if (startTime == null)
                return;

            if (TimeLimit != null && !Refresh)
            {
                var curTime = endTime.Value - startTime.Value;

                targetTime = curTime + Time < TimeLimit.Value ? curTime + Time : TimeLimit.Value;
            }
        }

        status.TryAddStatusEffectDuration(target, StatusEffect, targetTime);
    }
}
