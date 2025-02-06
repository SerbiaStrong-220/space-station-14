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

    [DataField]
    public TimeSpan ChargingTime = TimeSpan.FromSeconds(300);

    [DataField]
    public List<string>? PathDungeon;

    [DataField]
    public GateType GateType;

}
