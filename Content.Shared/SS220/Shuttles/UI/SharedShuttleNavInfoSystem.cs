// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Weapons.Ranged;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Shuttles.UI;

public abstract class SharedShuttleNavInfoSystem : EntitySystem
{
    public void AddHitscan(MapCoordinates fromCoordinates, float distance, Angle angle, HitscanPrototype prototype)
    {
        if (prototype.ShuttleNavHitscanInfo is not { } info)
            return;

        AddHitscan(fromCoordinates, distance, angle, info);
    }

    public void AddHitscan(MapCoordinates fromCoordinates, float distance, Angle angle, ShuttleNavHitscanInfo info)
    {
        var dir = angle.ToVec();
        var toCoordinates = new MapCoordinates(fromCoordinates.Position + dir * distance, fromCoordinates.MapId);
        AddHitscan(fromCoordinates, toCoordinates, info);
    }

    public void AddHitscan(MapCoordinates fromCoordinates, MapCoordinates toCoordinates, HitscanPrototype prototype)
    {
        if (prototype.ShuttleNavHitscanInfo is not { } info)
            return;

        AddHitscan(fromCoordinates, toCoordinates, info);
    }

    public abstract void AddHitscan(MapCoordinates fromCoordinates, MapCoordinates toCoordinates, ShuttleNavHitscanInfo info);
}

[Serializable, NetSerializable]
public sealed class ShuttleNavInfoAddHitscanMessage(MapCoordinates fromCoordinates, MapCoordinates toCoordinates, ShuttleNavHitscanInfo info) : EntityEventArgs
{
    public MapCoordinates FromCoordinated = fromCoordinates;
    public MapCoordinates ToCoordinated = toCoordinates;
    public ShuttleNavHitscanInfo Info = info;
}

[Serializable, NetSerializable]
public sealed class ShuttleNavInfoUpdateProjectilesMessage(List<(MapCoordinates, ShuttleNavProjectileInfo)> list) : EntityEventArgs
{
    public List<(MapCoordinates CurCoordinates, ShuttleNavProjectileInfo Info)> List = list;
}

[Serializable, NetSerializable]
public sealed class ShuttleNavInfoUpdateForcefieldsMessage(List<ShuttleNavForcefieldInfo> infos) : EntityEventArgs
{
    public List<ShuttleNavForcefieldInfo> Infos = infos;
}
