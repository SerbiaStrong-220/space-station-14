// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Zones.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Zones.Components;

[AutoGenerateComponentState]
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedZonesSystem))]
public sealed partial class ZonesContainerComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public HashSet<NetEntity> Zones = new();
}
