// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Pathology.Conditions;

public sealed partial class PathologyTimeProgressCondition : PathologyProgressCondition
{
    [DataField(required: true)]
    public TimeSpan Delay;

    protected override bool Condition(in PathologyEffectArgs args)
    {
        return args.CurTime > args.Data.StageStartTime + Delay;
    }
}
