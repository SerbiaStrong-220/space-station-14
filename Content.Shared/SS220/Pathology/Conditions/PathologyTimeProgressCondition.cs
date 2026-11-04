// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Timing;

namespace Content.Shared.SS220.Pathology.Conditions;

public sealed partial class PathologyTimeProgressCondition : PathologyProgressCondition
{
    [DataField(required: true)]
    public TimeSpan Delay;

    protected override bool Condition(EntityUid uid, PathologyInstanceData instanceData, IEntityManager entityManager)
    {
        var gameTiming = IoCManager.Resolve<IGameTiming>();

        return gameTiming.CurTime > instanceData.StartTime + Delay;
    }
}
