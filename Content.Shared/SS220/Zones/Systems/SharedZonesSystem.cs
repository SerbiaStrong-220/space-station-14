
using Content.Shared.SS220.Zones.Components;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Prototypes;
using System.Linq;
using System.Numerics;

namespace Content.Shared.SS220.Zones.Systems;

public abstract partial class SharedZonesSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public static EntProtoId<ZoneComponent> BaseZoneId = "BaseZone";

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ZoneComponent>();
        while (query.MoveNext(out var uid, out var zoneComp))
        {
            var map = _transform.GetMap(uid);
            if (!_map.IsInitialized(map))
                continue;

            ProcessZone((uid, zoneComp));
        }
    }

    public void ProcessZone(Entity<ZoneComponent> zone)
    {
        var entitiesToLeave = zone.Comp.Entities;
        var entitiesToEnter = new HashSet<EntityUid>();
        var curEntities = GetEntitiesInZone(zone).ToHashSet();
        foreach (var entity in curEntities)
        {
            if (entitiesToLeave.Remove(entity))
                continue;

            entitiesToEnter.Add(entity);
        }

        foreach (var entity in entitiesToLeave)
        {
            var ev = new LeavedZoneEvent(zone, entity);
            RaiseLocalEvent(zone, ev);
            RaiseLocalEvent(entity, ev);
        }

        foreach (var entity in entitiesToEnter)
        {
            var ev = new EnteredZoneEvent(zone, entity);
            RaiseLocalEvent(zone, ev);
            RaiseLocalEvent(entity, ev);
        }

        zone.Comp.Entities = curEntities;
    }

    public IEnumerable<EntityUid> GetEntitiesInZone(Entity<ZoneComponent> zone)
    {
        var parent = GetEntity(zone.Comp.Parent);
        if (!TryComp<BroadphaseComponent>(parent, out var broadphase))
            return new HashSet<EntityUid>();

        return GetEntitiesInZone((parent.Value, broadphase), zone);
    }

    public IEnumerable<EntityUid> GetEntitiesInZone(
        Entity<BroadphaseComponent> parent,
        Entity<ZoneComponent> zone)
    {
        HashSet<EntityUid> entities = new();
        var lookup = parent.Comp;
        var state = (entities, zone);

        foreach (var box in zone.Comp.Boxes)
        {
            lookup.DynamicTree.QueryAabb(ref state, ZoneQueryCallback, box, true);
            lookup.StaticTree.QueryAabb(ref state, ZoneQueryCallback, box, true);
            lookup.SundriesTree.QueryAabb(ref state, ZoneQueryCallback, box, true);
            lookup.StaticSundriesTree.QueryAabb(ref state, ZoneQueryCallback, box, true);
        }

        return state.entities;
    }

    private bool ZoneQueryCallback(ref (HashSet<EntityUid> Processed, Entity<ZoneComponent> Zone) state, in EntityUid uid)
    {
        if (InZone(state.Zone, uid))
        {
            return state.Processed.Add(uid);
        }

        return false;
    }

    private bool ZoneQueryCallback(ref (HashSet<EntityUid> Processed, Entity<ZoneComponent> Zone) state, in FixtureProxy proxy)
    {
        return ZoneQueryCallback(ref state, proxy.Entity);
    }

    public static Box2 GetBox(EntityCoordinates point1, EntityCoordinates point2)
    {
        return GetBox(point1.Position, point2.Position);
    }

    public static Box2 GetBox(MapCoordinates point1, MapCoordinates point2)
    {
        return GetBox(point1.Position, point2.Position);
    }

    public static Box2 GetBox(Vector2 point1, Vector2 point2)
    {
        var left = Math.Min(point1.X, point2.X);
        var bottom = Math.Min(point1.Y, point2.Y);
        var right = Math.Max(point1.X, point2.X);
        var top = Math.Max(point1.Y, point2.Y);

        var bottomLeft = new Vector2(left, bottom);
        var topRight = new Vector2(right, top);
        return new Box2(bottomLeft, topRight);
    }

    public static Box2i GetIntegerBox(Box2 box)
    {
        return GetIntegerBox(box.BottomLeft, box.TopRight);
    }

    public static Box2i GetIntegerBox(TileRef tile1, TileRef tile2)
    {
        return GetIntegerBox(tile1.GridIndices, tile2.GridIndices);
    }

    public static Box2i GetIntegerBox(MapCoordinates point1, MapCoordinates point2)
    {
        return GetIntegerBox(point1.Position, point2.Position);
    }

    public static Box2i GetIntegerBox(EntityCoordinates point1, EntityCoordinates point2)
    {
        return GetIntegerBox(point1.Position, point2.Position);
    }

    public static Box2i GetIntegerBox(Vector2 point1, Vector2 point2)
    {
        var left = (int)Math.Floor(Math.Min(point1.X, point2.X));
        var bottom = (int)Math.Floor(Math.Min(point1.Y, point2.Y));
        var right = (int)Math.Floor(Math.Max(point1.X, point2.X)) + 1;
        var top = (int)Math.Floor(Math.Max(point1.Y, point2.Y)) + 1;

        var bottomLeft = new Vector2i(left, bottom);
        var topRight = new Vector2i(right, top);
        return new Box2i(bottomLeft, topRight);
    }

    public bool InZone(Entity<ZoneComponent> zone, EntityUid uid)
    {
        return InZone(zone, Transform(uid).Coordinates);
    }

    public bool InZone(Entity<ZoneComponent> zone, MapCoordinates point)
    {
        if (GetEntity(zone.Comp.Parent) != _map.GetMap(point.MapId))
            return false;

        return InZone(zone, point.Position);
    }

    public bool InZone(Entity<ZoneComponent> zone, EntityCoordinates point)
    {
        if (GetEntity(zone.Comp.Parent) != point.EntityId)
            return false;

        return InZone(zone, point.Position);
    }

    public static bool InZone(Entity<ZoneComponent> zone, Vector2 point)
    {
        foreach (var box in zone.Comp.Boxes)
        {
            if (box.Contains(point))
                return true;
        }

        return false;
    }

    public IEnumerable<Entity<ZoneComponent>> GetZonesByPoint(MapCoordinates point)
    {
        List<Entity<ZoneComponent>> zones = new();
        var uid = _map.GetMap(point.MapId);
        if (!TryComp<ZonesDataComponent>(uid, out var zonesData))
            return zones;

        return GetZonesByPoint((uid, zonesData), point.Position);
    }

    public IEnumerable<Entity<ZoneComponent>> GetZonesByPoint(EntityCoordinates point)
    {
        List<Entity<ZoneComponent>> zones = new();
        if (!TryComp<ZonesDataComponent>(point.EntityId, out var zonesData))
            return zones;

        return GetZonesByPoint((point.EntityId, zonesData), point.Position);
    }

    public IEnumerable<Entity<ZoneComponent>> GetZonesByPoint(Entity<ZonesDataComponent> parent, Vector2 point)
    {
        List<Entity<ZoneComponent>> zones = new();
        foreach (var zoneNet in parent.Comp.Zones)
        {
            var zone = GetEntity(zoneNet);
            if (!TryComp<ZoneComponent>(zone, out var zoneComp))
                continue;

            if (InZone((zone, zoneComp), point))
                zones.Add((zone, zoneComp));
        }

        return zones;
    }
}

public sealed partial class EnteredZoneEvent(Entity<ZoneComponent> zone, EntityUid entity) : EntityEventArgs
{
    public readonly Entity<ZoneComponent> Zone = zone;
    public readonly EntityUid Entity = entity;
}

public sealed partial class LeavedZoneEvent(Entity<ZoneComponent> zone, EntityUid entity) : EntityEventArgs
{
    public readonly Entity<ZoneComponent> Zone = zone;
    public readonly EntityUid Entity = entity;
}
