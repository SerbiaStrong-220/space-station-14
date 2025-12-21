// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;

namespace Content.Shared.SS220.ChemicalAdaptation;

/// <summary>
/// The component is designed to accumulate and retain drug effect modifiers.
/// Made as a dictionary in the hope that it will be used in other places
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ChemicalAdaptationComponent : Component
{
    public Dictionary<string, AdaptationInfo> ChemicalAdaptations = [];
}

public sealed partial class AdaptationInfo(TimeSpan duration, float modifier, bool refresh)
{
    public float Modifier = modifier;

    public TimeSpan Duration = duration;

    public bool Refresh = refresh;
}
