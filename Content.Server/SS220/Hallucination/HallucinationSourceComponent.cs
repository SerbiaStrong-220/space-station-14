// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Random;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Hallucination;
[RegisterComponent]
public sealed partial class HallucinationSourceComponent : Component
{
    [DataField("range"), ViewVariables(VVAccess.ReadWrite)]
    public float RangeOfHallucinations = 6f;
    [DataField("rangeEnd"), ViewVariables(VVAccess.ReadWrite)]
    public float RangeOfEndHallucinations = 8f;
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool EyeProtectionDependent = false;
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float BetweenHallucinations;
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float HallucinationMinTime;
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float HallucinationMaxTime;
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float TotalDuration;
    [DataField("weightedRandom"), ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<WeightedRandomEntityPrototype> RandomEntitiesProto;
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan NextUpdateTime = default!;

    public (float BetweenHallucinations, float HallucinationMinTime,
            float HallucinationMaxTime, float TotalDuration)? GetTimeParams()
    {
        var timeParams = (BetweenHallucinations, HallucinationMinTime, HallucinationMaxTime, TotalDuration);
        return timeParams;
    }
}
