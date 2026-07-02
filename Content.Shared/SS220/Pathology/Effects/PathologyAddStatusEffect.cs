// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.PathologyStatusEffects;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Pathology.Effects;

public sealed partial class PathologyAddStatusEffect : IPathologyEffect
{
    [DataField(required: true)]
    public EntProtoId StatusEffect;

    public void ApplyEffect(in PathologyEffectArgs args)
    {
        var statusEffects = args.EntityManager.System<StatusEffectsSystem>();

        if (!statusEffects.TryUpdateStatusEffectDuration(args.Target, StatusEffect, out var effect, null, null))
            return;

        if (!args.EntityManager.TryGetComponent<PathologyStatusEffectStackableComponent>(effect, out var stackableComponent))
            return;

        stackableComponent.StackCount = args.Data.StackCount;
    }
}
