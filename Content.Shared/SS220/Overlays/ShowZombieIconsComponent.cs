// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Overlays;

/// <summary>
/// Allows ghosts to see zombie and initial infected status icons.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ShowZombieIconsComponent : Component
{
}
