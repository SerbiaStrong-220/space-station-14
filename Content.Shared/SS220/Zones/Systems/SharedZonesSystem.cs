
using Content.Shared.Maps;
using Content.Shared.SS220.Zones.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;

namespace Content.Shared.SS220.Zones.Systems;

public abstract partial class SharedZonesSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _map = default!;

    public int? GetZoneId(Entity<ZoneComponent> zone)
    {
        var grid = GetEntity(zone.Comp.AttachedGrid);
        if (grid == null ||
            !TryComp<ZonesDataComponent>(grid, out var zonesData))
            return null;

        return GetZoneId((grid.Value, zonesData), zone);
    }

    public int? GetZoneId(Entity<ZonesDataComponent> grid, Entity<ZoneComponent> zone)
    {
        foreach (var (id, zoneData) in grid.Comp.Zones)
        {
            if (zoneData.ZoneEntity == GetNetEntity(zone))
                return id;
        }

        return null;
    }

    public bool InZone(EntityUid entity, EntityUid zoneUid)
    {
        var zones = ZonesByCoordinates(Transform(entity).Coordinates);
        foreach (var zone in zones)
        {
            if (zone.ZoneEntity == GetNetEntity(zoneUid))
                return true;
        }

        return false;
    }

    public IEnumerable<ZoneData> ZonesByCoordinates(EntityCoordinates coordinates)
    {
        List<ZoneData> zones = new();
        var grid = coordinates.EntityId;
        if (!TryComp<MapGridComponent>(grid, out var mapGrid) ||
            !TryComp<ZonesDataComponent>(grid, out var zoneData))
            return zones;

        return ZonesByCoordinates((grid, mapGrid, zoneData), coordinates);
    }

    public IEnumerable<ZoneData> ZonesByCoordinates(Entity<MapGridComponent?, ZonesDataComponent?> grid, EntityCoordinates coordinates)
    {
        List<ZoneData> zones = new();
        if (!Resolve(grid, ref grid.Comp1) ||
            !Resolve(grid, ref grid.Comp2))
            return zones;

        var tileRef = _map.GetTileRef((grid, grid.Comp1), coordinates);
        if (tileRef.IsSpace())
            return zones;

        foreach (var zone in grid.Comp2.Zones.Values)
        {
            if (zone.Tiles.Contains(tileRef.GridIndices))
                zones.Add(zone);
        }

        return zones;
    }

    public ZoneData? GetZoneData(Entity<ZoneComponent> zone)
    {
        var grid = GetEntity(zone.Comp.AttachedGrid);
        if (grid == null ||
            !TryComp<ZonesDataComponent>(grid, out var zonesData))
            return null;

        return GetZoneData((grid.Value, zonesData), zone);
    }

    public ZoneData? GetZoneData(Entity<ZonesDataComponent> grid, Entity<ZoneComponent> zone)
    {
        var zoneId = GetZoneId(grid, zone);
        if (zoneId == null)
            return null;

        return GetZoneData(grid, zoneId.Value);
    }

    public ZoneData? GetZoneData(Entity<ZonesDataComponent> grid, int zoneId)
    {
        grid.Comp.Zones.TryGetValue(zoneId, out var zoneData);
        return zoneData;
    }

    public IEnumerable<EntityUid> GetEntitiesInZone(Entity<MapGridComponent, ZonesDataComponent> grid, int zoneId)
    {
        HashSet<EntityUid> entities = new();
        if (!TryComp<BroadphaseComponent>(grid, out var broadphase))
            return entities;

        var zone = GetZoneData((grid, grid), zoneId);
        if (zone == null)
            return entities;

        foreach (var tile in zone.Tiles)
            GetEntitiesInTile((grid, broadphase, grid), tile, ref entities);

        return entities;
    }

    private void GetEntitiesInTile(
        Entity<BroadphaseComponent, MapGridComponent> grid,
        Vector2i tile,
        ref HashSet<EntityUid> processed)
    {
        var lookup = grid.Comp1;
        var size = grid.Comp2.TileSize;
        var tileBox = new Box2(tile * size, (tile + 1) * size);

        var state = processed;

        lookup.DynamicTree.QueryAabb(ref state, ZoneQueryCallback, tileBox, true);
        lookup.StaticTree.QueryAabb(ref state, ZoneQueryCallback, tileBox, true);
        lookup.SundriesTree.QueryAabb(ref state, ZoneQueryCallback, tileBox, true);
        lookup.StaticSundriesTree.QueryAabb(ref state, ZoneQueryCallback, tileBox, true);
    }

    private static bool ZoneQueryCallback(ref HashSet<EntityUid> processed, in EntityUid uid)
    {
        processed.Add(uid);
        return true;
    }

    private static bool ZoneQueryCallback(ref HashSet<EntityUid> processed, in FixtureProxy proxy)
    {
        return ZoneQueryCallback(ref processed, proxy.Entity);
    }
}
