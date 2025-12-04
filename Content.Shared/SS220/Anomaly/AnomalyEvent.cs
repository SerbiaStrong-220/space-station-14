using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Anomaly;

[Serializable, NetSerializable]
public sealed class AnomalyGeneratorEmaggedEventMessage(List<AnomalyGeneratorEmagStruct> beacons) : BoundUserInterfaceMessage
{
    public List<AnomalyGeneratorEmagStruct> Beacons = beacons;
}

[Serializable, NetSerializable]
public record struct AnomalyGeneratorEmagStruct(NetEntity Beacon, string Name);

[Serializable, NetSerializable]
public sealed class AnomalyGeneratorChooseAnomalyPlaceMessage(NetEntity beacon) : BoundUserInterfaceMessage
{
    public NetEntity Beacon = beacon;
}
