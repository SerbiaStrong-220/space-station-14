// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Pathology.Conditions;

/// <summary>
/// Progresses the pathology once enough stacks are accumulated, e.g. organ damage turning into organ failure.
/// </summary>
public sealed partial class PathologyStackProgressCondition : PathologyProgressCondition
{
    [DataField(required: true)]
    public int StackCount;

    protected override bool Condition(EntityUid uid, PathologyInstanceData instanceData, TimeSpan curTime, IEntityManager entityManager)
    {
        return instanceData.StackCount >= StackCount;
    }
}
