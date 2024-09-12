// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Whitelist;

namespace Content.Server.SS220.SpiderQueen;

[RegisterComponent]
public sealed partial class SpiderQueenRuleComponent : Component
{
    /// <summary>
    /// ID of the spawner of this antagonist
    /// </summary>
    [DataField]
    public string SpawnerID = "SpawnPointGhostSpaceQueen";

    /// <summary>
    /// Spawn on a random entity that passed whitelist.
    /// If null - spawn on a random tile.
    /// </summary>
    [DataField]
    public EntityWhitelist? MarkersWhitelist;
}
