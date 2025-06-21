// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Zones.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Zones.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedZonesSystem))]
public sealed partial class ZonesContainerComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public HashSet<NetEntity> Zones = new();
}

[Serializable, NetSerializable]
public sealed class ZonesContainerComponentState(HashSet<NetEntity> zones) : IComponentState
{
    public HashSet<NetEntity> Zones = zones;
}
