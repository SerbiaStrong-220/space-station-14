// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;

namespace Content.Shared.SS220.ChemicalAdaptation;

/// <summary>
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ChemicalAdaptationComponent : Component
{
    public Dictionary<string, AdaptationInfo> ChemicalAdaptations;
}
public sealed partial class AdaptationInfo(TimeSpan duration, float modifier)
{
    public float Modifier = modifier;

    public TimeSpan Duration = duration;
}
