// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Zones.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Zones;

[Serializable, NetSerializable]
public sealed class CreateZoneRequestMessage(
    NetEntity parent,
    EntProtoId<ZoneComponent> protoId,
    List<Box2> area,
    string? name = null,
    Color? color = null,
    bool attachToLattice = false) : EntityEventArgs
{
    public NetEntity Parent = parent;

    public EntProtoId<ZoneComponent> ProtoId = protoId;

    public List<Box2> Area = area;

    public string? Name = name;

    public Color? Color = color;

    public bool AttachToLattice = attachToLattice;
}

[Serializable, NetSerializable]
public sealed class ChangeZoneRequestMessage(
    NetEntity zone,
    NetEntity? parent = null,
    EntProtoId<ZoneComponent>? protoId = null,
    List<Box2>? area = null,
    string? name = null,
    Color? color = null,
    bool? attachToLattice = null) : EntityEventArgs
{
    public NetEntity Zone = zone;

    public NetEntity? Parent = parent;

    public EntProtoId<ZoneComponent>? ProtoId = protoId;

    public List<Box2>? Area = area;

    public string? Name = name;

    public Color? Color = color;

    public bool? AttachToLattice = attachToLattice;
}

[Serializable, NetSerializable]
public sealed class DeleteZoneRequestMessage(NetEntity zone) : EntityEventArgs
{
    public NetEntity Zone = zone;
}
