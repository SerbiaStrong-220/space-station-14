// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Antag;
using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.MindSlave;

/// <summary>
/// Used to mark an entity as a mind-slave.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class MindSlaveComponent : Component
{
    public ProtoId<FactionIconPrototype> StatusIcon { get; set; } = "MindSlaveIcon";
}
