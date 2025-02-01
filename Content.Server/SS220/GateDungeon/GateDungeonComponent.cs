// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Map;

namespace Content.Server.SS220.GateDungeon;

/// <summary>
/// This handles creates a new map from the list and connects them with teleports.
/// </summary>
[RegisterComponent]
public sealed partial class GateDungeonComponent : Component
{
    public bool IsCharging = true;

    public MapId MapId;

    public TimeSpan ChargingTime = TimeSpan.FromMinutes(5);

    [DataField]
    public List<string>? PathDungeon;

    public List<EntityUid>? GateStart = new();
    public List<EntityUid>? GateMedium = new();
    public List<EntityUid>? GateEnd = new();
    public List<EntityUid>? GateEndToStation = new();
}
