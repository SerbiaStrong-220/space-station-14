// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;

namespace Content.Shared.SS220.MindSlave;

/// <summary>
/// Used to mark an entity as a mind-slave.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MindSlaveComponent : Component
{
}
