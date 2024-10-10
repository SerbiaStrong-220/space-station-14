// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Hallucination;

namespace Content.Server.SS220.Hallucination;
/// <summary>
/// Yep
/// </summary>
[RegisterComponent]
public sealed partial class HallucinationComponent : SharedHallucinationComponent
{
    /// <summary>
    /// Used to track when to stop hallucination in the same order as higher
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public List<TimeSpan?> TotalDurationTimeSpans = [];
}
