// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Server.SS220.RandomTeleport;

/// <summary>
/// If you want to teleport to a random entity with a specific component
/// </summary>
[RegisterComponent]
public sealed partial class RandomTeleportComponent : Component
{
    [DataField(required: true)]
    public string? TargetsComponent;
}
