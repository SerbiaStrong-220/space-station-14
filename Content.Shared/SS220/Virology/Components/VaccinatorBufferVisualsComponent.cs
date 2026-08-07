// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Virology;

[RegisterComponent]
public sealed partial class VaccinatorBufferVisualsComponent : Component
{
    /// <summary>RSI state.</summary>
    [DataField]
    public string FillBaseName = "fill";

    /// <summary>How many fill states exist.</summary>
    [DataField]
    public int MaxFillLevels = 9;
}
