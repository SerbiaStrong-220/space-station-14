// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.DeviceLinking;
using Content.Shared.Shuttles.BUIStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.WeaponControl;

[Serializable, NetSerializable]

public sealed class WeaponControlBoundUserInterfaceState(NavInterfaceState navState, WeaponControlInterfaceState weaponsState) : BoundUserInterfaceState
{
    public NavInterfaceState NavState = navState;
    public WeaponControlInterfaceState WeaponsState = weaponsState;
}

public sealed class WeaponControlInterfaceState( Dictionary<EntityUid, HashSet<(ProtoId<SourcePortPrototype> Source, ProtoId<SinkPortPrototype> Sink)>> ports)
{
    public Dictionary<EntityUid, HashSet<(ProtoId<SourcePortPrototype> Source, ProtoId<SinkPortPrototype> Sink)>> LinkedPorts = ports;
}
