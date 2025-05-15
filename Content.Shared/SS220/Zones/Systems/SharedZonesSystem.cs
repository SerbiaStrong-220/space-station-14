
using Content.Shared.SS220.Zones.Components;

namespace Content.Shared.SS220.Zones.Systems;

public abstract partial class SharedZonesSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

    }

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
}
