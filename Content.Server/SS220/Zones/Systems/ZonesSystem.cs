using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.SS220.Zones.Components;
using Content.Shared.SS220.Zones.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using System.Linq;
using System.Numerics;

namespace Content.Server.SS220.Zones.Systems;

public sealed partial class ZonesSystem : SharedZonesSystem
{
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ZonesDataComponent, MapInitEvent>(OnZonesDataInit);
        SubscribeLocalEvent<ZonesDataComponent, ComponentShutdown>(OnZoneDataShutdown);

        SubscribeLocalEvent<ZoneComponent, ComponentShutdown>(OnZoneShutdown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<MapGridComponent, ZonesDataComponent>();
        while (query.MoveNext(out var uid, out var mapGrid, out var zonesData))
        {
            foreach (var (id, zone) in zonesData.Zones)
            {
                foreach (var tile in zone.Tiles)
                {
                    var ents = TurfHelpers.GetEntitiesInTile(_map.GetTileRef((uid, mapGrid), tile), LookupFlags.All);
                    var sss = _turf.IsTileBlocked(uid, tile, CollisionGroup.AllMask, mapGrid);
                }
            }
        }
    }

    private void OnZonesDataInit(Entity<ZonesDataComponent> entity, ref MapInitEvent args)
    {
        if (!TryComp<MapGridComponent>(entity, out var gridComponent))
            return;

        foreach (var zoneId in entity.Comp.Zones.Keys)
            InitializeZone((entity, gridComponent, entity), zoneId);
    }

    private void OnZoneDataShutdown(Entity<ZonesDataComponent> entity, ref ComponentShutdown args)
    {
        foreach (var id in entity.Comp.Zones.Keys)
            DeleteZone(entity, id);
    }

    private void OnZoneShutdown(Entity<ZoneComponent> entity, ref ComponentShutdown args)
    {
        DeleteZone(entity);
    }

    public ZoneData? CreateZone(Entity<MapGridComponent> grid,
        EntityCoordinates point1,
        EntityCoordinates point2)
    {
        var cords = GetTilesCoordinatesInBox(grid, point1, point2);
        return CreateZone(grid, cords);
    }

    public ZoneData? CreateZone(Entity<MapGridComponent> grid, HashSet<EntityCoordinates> cords)
    {
        var tiles = cords
            .Select(c => _map.GetTileRef(grid, c))
            .Where(t => !t.IsSpace())
            .Select(t => t.GridIndices)
            .ToHashSet();

        return CreateZone(grid, tiles);
    }

    public ZoneData? CreateZone(Entity<MapGridComponent> grid, HashSet<Vector2i> tiles)
    {
        if (tiles.Count <= 0)
            return null;

        var zoneData = new ZoneData()
        {
            Tiles = tiles,
        };

        return CreateZone(grid, zoneData);
    }

    public ZoneData? CreateZone(Entity<MapGridComponent> grid, ZoneData zoneData)
    {
        var zonesComp = EnsureComp<ZonesDataComponent>(grid);

        var zoneId = zonesComp.GetFreeZoneId();
        zonesComp.Zones.Add(zoneId, zoneData);

        Dirty(grid, zonesComp);

        var mapId = _transform.GetMapId(grid.Owner);
        if (_map.IsInitialized(mapId))
            InitializeZone((grid, grid, zonesComp), zoneId);

        return zoneData;
    }

    public void DeleteZone(Entity<ZoneComponent> entity)
    {
        var grid = GetEntity(entity.Comp.AttachedGrid);
        if (grid == null ||
            !TryComp<ZonesDataComponent>(grid, out var zonesData))
            return;

        DeleteZone((grid.Value, zonesData), entity);
    }

    public void DeleteZone(Entity<ZonesDataComponent> grid, Entity<ZoneComponent> entity)
    {
        var zoneId = GetZoneId(grid, entity);
        if (zoneId == null)
            return;

        DeleteZone(grid, zoneId.Value);
    }

    public void DeleteZone(Entity<ZonesDataComponent> grid, int zoneId)
    {
        if (!grid.Comp.Zones.TryGetValue(zoneId, out var zone))
            return;

        grid.Comp.Zones.Remove(zoneId);
        var zoneEnt = GetEntity(zone.ZoneEntity);
        QueueDel(zoneEnt);
    }

    private void InitializeZone(Entity<MapGridComponent, ZonesDataComponent> grid, int zoneId)
    {
        if (!grid.Comp2.Zones.TryGetValue(zoneId, out var zoneData))
            return;

        var zone = Spawn(zoneData.EntityId);
        zoneData.ZoneEntity = GetNetEntity(zone);

        var zoneComp = EnsureComp<ZoneComponent>(zone);
        zoneComp.AttachedGrid = GetNetEntity(grid);
        zoneComp.GridZoneId = zoneId;

        Dirty(zone, zoneComp);
        Dirty(grid, grid.Comp2);
    }

    private HashSet<EntityCoordinates> GetTilesCoordinatesInBox(Entity<MapGridComponent> grid, EntityCoordinates point1, EntityCoordinates point2)
    {
        return GetTilesCoordinatesInBox(grid, new Vector2(point1.X, point1.Y), new Vector2(point2.X, point2.Y));
    }

    private HashSet<EntityCoordinates> GetTilesCoordinatesInBox(Entity<MapGridComponent> grid, Vector2 point1, Vector2 point2)
    {
        HashSet<Vector2> array = new();
        var top = Math.Max(point1.Y, point2.Y);
        var right = Math.Max(point1.X, point2.X);
        var bottom = Math.Min(point1.Y, point2.Y);
        var left = Math.Min(point1.X, point2.X);

        var step = grid.Comp.TileSize;
        var endPoint = new Vector2(right, top);
        var curPoint = new Vector2(left, bottom);
        while (curPoint != endPoint)
        {
            array.Add(curPoint);
            if (curPoint.X != right)
                curPoint.X = Math.Min(curPoint.X + step, right);
            else if (curPoint.Y != top)
            {
                curPoint.Y = Math.Min(curPoint.Y + step, top);
                curPoint.X = left;
            }
        }
        array.Add(endPoint);

        return array.Select(v => new EntityCoordinates(grid, v)).ToHashSet();
    }
}
