// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Pathology;

[RegisterComponent]
public sealed partial class DiseaseDiagnoserBufferVisualsComponent : Component
{
    /// <summary>RSI state.</summary>
    [DataField]
    public string FillBaseName = "fill";

    /// <summary>How many fill states exist.</summary>
    [DataField]
    public int MaxFillLevels = 3;
}
