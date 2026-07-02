// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Server.SS220.Pathology;

[RegisterComponent]
public sealed partial class VirusOutbreakRuleComponent : Component
{
    /// <summary>Minimum number of crew infected by outbreak.</summary>
    [DataField]
    public int MinVictims = 1;

    /// <summary>Maximum number of crew infected by outbreak.</summary>
    [DataField]
    public int MaxVictims = 2;
}
