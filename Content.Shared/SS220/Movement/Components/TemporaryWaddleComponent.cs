// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Movement.Components;

/// <summary>
///     Exists for use as a status effect.
///     Entity with this component will be funny to waddle.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class TemporaryWaddleComponent : Component
{
}
