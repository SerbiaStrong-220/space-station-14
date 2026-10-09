// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Teleport.Components;

/// <summary>
///     Spawns an entity at the target's departure location before teleportation.
/// </summary>
[RegisterComponent]
public sealed partial class SpawnBeforeTeleportComponent : Component
{
    /// <summary>
    ///     The entity to spawn.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId SpawnPrototype;
}
