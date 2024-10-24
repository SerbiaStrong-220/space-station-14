// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Movement;

// <summary>
// Entities with this component will not be slowdown from onfloor objects.
// For example, it is used by flying entities.
// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class IgnoreOnfloorSlowersComponent : Component
{
}
