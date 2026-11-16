// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Pathology.Effects;

public sealed partial class PathologyAddStatusEffect : IPathologyEffect
{
    [DataField(required: true)]
    public EntProtoId StatusEffect;

    public void ApplyEffect(EntityUid uid, PathologyInstanceData data, IEntityManager entityManager)
    {
        if (!entityManager.TryGetComponent<PathologyHolderComponent>(uid, out var holder))
            return;

        entityManager.System<SharedPathologySystem>().UpdateStackableStatusEffect((uid, holder), StatusEffect);
    }
}
