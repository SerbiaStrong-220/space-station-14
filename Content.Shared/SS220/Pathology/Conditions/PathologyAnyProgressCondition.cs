// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Pathology.Conditions;

public sealed partial class PathologyAnyProgressCondition : PathologyProgressCondition
{
    [DataField(required: true)]
    public PathologyProgressCondition[] Conditions = Array.Empty<PathologyProgressCondition>();

    protected override bool Condition(in PathologyEffectArgs args)
    {
        foreach (var condition in Conditions)
        {
            if (condition.CheckCondition(in args))
                return true;
        }

        return false;
    }
}
