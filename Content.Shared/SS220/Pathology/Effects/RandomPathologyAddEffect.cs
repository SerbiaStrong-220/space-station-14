// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Random;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Pathology.Effects;

public sealed partial class RandomPathologyAddEffect : IPathologyEffect
{
    [DataField(required: true)]
    public float Chance;

    [DataField(required: true)]
    public ProtoId<WeightedRandomPrototype> WeightedId = "BrainTraumaPathology";

    public void ApplyEffect(EntityUid uid, PathologyInstanceData data, IEntityManager entityManager)
    {
        var pathologySystem = entityManager.System<PathologySystem>();

        pathologySystem.TryAddRandom(uid, WeightedId, Chance);
    }
}
