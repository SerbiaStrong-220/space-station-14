// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Virology.Conditions;

public sealed partial class VirusTimeProgressCondition : VirusProgressCondition
{
    [DataField(required: true)]
    public TimeSpan Delay;

    protected override bool Condition(in VirusProgressArgs args)
    {
        return args.CurTime > args.Symptom.StageStartTime + Delay;
    }
}
