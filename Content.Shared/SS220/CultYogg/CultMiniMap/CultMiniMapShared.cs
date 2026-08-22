// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Medical.SuitSensor;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.CultYogg.CultMiniMap;

[Serializable, NetSerializable]
public enum CultMiniMapUIKey
{
    Key
}

[Serializable, NetSerializable]
public sealed class CultMiniMapState(List<SuitSensorStatus> sensors) : BoundUserInterfaceState
{
    public List<SuitSensorStatus> Sensors = sensors;
}
