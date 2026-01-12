// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Body.Systems;
using Content.Shared.FixedPoint;

namespace Content.Shared.SS220.Pathology.Effects;

public sealed partial class InternalBloodLossEffect : IPathologyEffect
{
    /// <summary>
    /// Blood loss per update interval and per stack
    /// </summary>
    [DataField]
    public FixedPoint2 LossRate = 0.3f;

    public void ApplyEffect(EntityUid uid, PathologyInstanceData data, IEntityManager entityManager)
    {
        var bloodSystem = entityManager.System<SharedBloodstreamSystem>();

        bloodSystem.TryModifyBloodLevel(uid, LossRate * data.StackCount * PathologySystem.UpdateInterval.TotalSeconds);
    }
}
