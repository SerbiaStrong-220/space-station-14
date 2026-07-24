// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Virology.Conditions;

public sealed partial class VirusAnyProgressCondition : VirusProgressCondition
{
    [DataField]
    public VirusProgressCondition[] Conditions = [];

    protected override bool Condition(in VirusProgressArgs args)
    {
        foreach (var condition in Conditions)
        {
            if (condition.CheckCondition(in args))
                return true;
        }

        return false;
    }
}
