// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Virology;

[DataDefinition]
public sealed partial class VirusSymptomDetection
{
    /// <summary>Text a scanner shows for this symptom once decoded.</summary>
    [DataField(required: true)]
    public LocId Description;
}
