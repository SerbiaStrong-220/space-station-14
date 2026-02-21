// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.StatusEffect;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.MartialArts.Sequence.Effects;

// TODO: when wizards will fully migrate to new status effects, we have to migrate this too
public sealed partial class ApplyOldStatusEffectCombatEffect : CombatSequenceEffect
{
    [DataField("effect", required: true)]
    public ProtoId<StatusEffectPrototype> StatusEffect;

    [DataField(required: true)]
    public string Component = string.Empty;

    [DataField]
    public TimeSpan Time = TimeSpan.FromSeconds(10);

    [DataField]
    public TimeSpan? TimeLimit = null;

    [DataField]
    public bool Refresh = false;

    public override void Execute(EntityUid user, EntityUid target, MartialArtistComponent artist)
    {
        var status = Entity.System<StatusEffectsSystem>();

        if (Refresh)
        {
            status.TryAddStatusEffect(target, StatusEffect, Time, true, Component);
            return;
        }

        var toAdd = Time;
        if (TimeLimit != null)
        {
            var remaining = TimeSpan.Zero;
            if (status.TryGetTime(target, StatusEffect, out var time))
            {
                remaining = Timing.CurTime < time.Value.Item2 ? time.Value.Item2 - Timing.CurTime : TimeSpan.Zero;
            }

            toAdd = TimeLimit.Value - remaining;
            if (toAdd > Time) toAdd = Time;
        }

        if (toAdd > TimeSpan.Zero)
            status.TryAddStatusEffect(target, StatusEffect, toAdd, false, Component);
    }
}
