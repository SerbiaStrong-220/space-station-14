
using Content.Shared.SS220.GridZones.Components;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using System.Numerics;

namespace Content.Shared.SS220.GridZones.Systems;

public sealed partial class ZonesSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GridZonesComponent, ComponentShutdown>(OnGridZoneShutdown);
        SubscribeLocalEvent<ZoneComponent, ComponentShutdown>(OnZoneShutdown);
    }

    private void OnGridZoneShutdown(Entity<GridZonesComponent> entity, ref ComponentShutdown args)
    {
        foreach (var (zone, _) in entity.Comp.Zones)
            DeleteZone(GetEntity(zone));
    }

    private void OnZoneShutdown(Entity<ZoneComponent> entity, ref ComponentShutdown args)
    {
        DeleteZone(entity.Owner);
    }

    public void CreateZone(Entity<MapGridComponent> grid, HashSet<Vector2> tiles, EntProtoId<ZoneComponent> zoneProto)
    {
        var zoneEnt = Spawn(zoneProto, Transform(grid).Coordinates);
        var zoneComp = EnsureComp<ZoneComponent>(zoneEnt);

        var gridZones = EnsureComp<GridZonesComponent>(grid);
        gridZones.Zones.Add(GetNetEntity(zoneEnt), tiles);

        zoneComp.AttachedGrid = grid;
    }

    public void DeleteZone(EntityUid uid)
    {
        if (!TryComp<ZoneComponent>(uid, out var zoneComp))
            return;

        DeleteZone((uid, zoneComp));
    }

    public void DeleteZone(Entity<ZoneComponent> zone)
    {
        if (zone.Comp.AttachedGrid is { } grid &&
            TryComp<GridZonesComponent>(grid, out var gridZones))
            gridZones.Zones.Remove(GetNetEntity(zone));

        zone.Comp.AttachedGrid = null;
        QueueDel(zone);
    }
}
