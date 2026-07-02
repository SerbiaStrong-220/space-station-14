// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Pathology;

[DataDefinition]
public sealed partial class SymptomDetection
{
    // Comp so we can expand later
    /// <summary>Text shown when virus detected.</summary>
    [DataField]
    public LocId? Description;
}
