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

        var toAdd = Time;

        if (status.TryGetTime(target, StatusEffect, out var effect))
        {
            var (_, endTime, _) = effect;

            if (endTime != null && TimeLimit != null)
            {
                var remaining = Timing.CurTime < endTime.Value ? endTime.Value - Timing.CurTime : TimeSpan.Zero;

                toAdd = TimeLimit.Value - remaining;
                if (toAdd > Time) toAdd = Time;
            }
        }

        if (toAdd > TimeSpan.Zero)
            status.TryAddStatusEffectDuration(target, StatusEffect, toAdd);
    }
}
