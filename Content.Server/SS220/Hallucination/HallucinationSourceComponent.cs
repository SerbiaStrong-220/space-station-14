// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Hallucination;

// TODO HallucinationParams -> struct
namespace Content.Server.SS220.Hallucination;
[RegisterComponent]
public sealed partial class HallucinationSourceComponent : Component
{
    /// <summary>
    /// So i kinda need to write !HallucinationSetting params
    /// </summary>
    [DataField]
    public HallucinationSetting Hallucination;
    /// <summary>
    /// In that range hallucination will be applied to entities
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("range")]
    public float RangeOfHallucinations = 1f;
    /// <summary>
    /// Next time after which we will check entities to send them hallucination
    /// </summary>
    [ViewVariables]
    public TimeSpan NextUpdateTime = default!;
}
