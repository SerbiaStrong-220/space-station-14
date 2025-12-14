// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Zones.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class InZoneComponent : Component
{
    /// <summary>
    /// An array of zones where our entity located
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public HashSet<NetEntity> Zones = [];
}
