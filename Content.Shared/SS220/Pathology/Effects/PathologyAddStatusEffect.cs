// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.PathologyStatusEffects;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Pathology.Effects;

public sealed partial class PathologyAddStatusEffect : IPathologyEffect
{
    [DataField(required: true)]
    public EntProtoId StatusEffect;

    public void ApplyEffect(EntityUid uid, PathologyInstanceData data, IEntityManager entityManager)
    {
        var statusEffects = entityManager.System<StatusEffectsSystem>();

        if (!statusEffects.TryUpdateStatusEffectDuration(uid, StatusEffect, out var effect, null, null))
            return;

        if (!entityManager.TryGetComponent<PathologyStatusEffectStackableComponent>(effect, out var stackableComponent))
            return;

        stackableComponent.StackCount = data.StackCount;
    }
}
