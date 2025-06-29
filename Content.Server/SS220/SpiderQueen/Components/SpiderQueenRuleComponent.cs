// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Whitelist;

namespace Content.Server.SS220.SpiderQueen.Components;

[RegisterComponent]
public sealed partial class SpiderQueenRuleComponent : Component
{
    /// <summary>
    /// Spawn on a random entity that passed whitelist.
    /// If null - spawn on a random tile.
    /// </summary>
    [DataField]
    public EntityWhitelist? MarkersWhitelist;
}
