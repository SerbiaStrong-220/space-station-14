// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.EntityEffects.Effects;

/// <summary>
/// This is used for displaying grilling smoke
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class GrillingVisualComponent : Component
{
    [DataField]
    public string GrillingLayer = "grilling-layer";
}
