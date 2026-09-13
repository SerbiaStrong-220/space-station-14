// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Teleport.Components;

/// <summary>
///     Spawn something when using a teleport at the location of the teleported object, usually the effect
/// </summary>
[RegisterComponent]
public sealed partial class SpawnBeforeTeleportComponent : Component
{
    /// <summary>
    ///     The entity we spawn
    /// </summary>
    [DataField(required: true)]
    public EntProtoId SpawnedEnt;
}
