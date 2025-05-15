using Content.Shared.SS220.Zones.Components;
using Content.Shared.SS220.Zones.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Map.Components;

namespace Content.Server.SS220.Zones.Systems;

public sealed partial class ZonesSystem : SharedZonesSystem
{
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ZonesDataComponent, MapInitEvent>(OnZonesDataInit);
        SubscribeLocalEvent<ZonesDataComponent, ComponentShutdown>(OnZoneDataShutdown);

        SubscribeLocalEvent<ZoneComponent, ComponentShutdown>(OnZoneShutdown);
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

    public ZoneData CreateZone(Entity<MapGridComponent> grid,
        HashSet<Vector2i> tiles,
        string? name = null,
        Color? color = null)
    {
        var zoneData = new ZoneData()
        {
            Tiles = tiles,
            Name = name ?? string.Empty,
            Color = color ?? Color.Gray
        };
        return CreateZone(grid, zoneData);
    }

    public ZoneData CreateZone(Entity<MapGridComponent> grid, ZoneData zoneData)
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
}
