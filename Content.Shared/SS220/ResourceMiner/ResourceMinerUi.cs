// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Robust.Shared.Serialization;

namespace Content.Shared.SS220.ResourceMiner;

[Serializable, NetSerializable]
public enum ResourceMinerSettings
{
    Key
}

[Serializable, NetSerializable]
public sealed class RequestAvailableSilos() : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class SetResourceMinerSilo(NetEntity silo) : BoundUserInterfaceMessage
{
    public NetEntity Silo = silo;
}

[Serializable, NetSerializable]
public sealed class AvailableSilosMiner(HashSet<NetEntity> silos) : BoundUserInterfaceState
{
    public HashSet<NetEntity> Silos = silos;
}
