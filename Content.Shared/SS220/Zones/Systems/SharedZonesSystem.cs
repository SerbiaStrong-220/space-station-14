
using Content.Shared.SS220.Zones.Components;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Prototypes;
using System.Numerics;

namespace Content.Shared.SS220.Zones.Systems;

public abstract partial class SharedZonesSystem : EntitySystem
{
    public static EntProtoId<ZoneComponent> BaseZoneId = "BaseZone";

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
        var state = entities;

        foreach (var box in zone.Comp.Boxes)
        {
            lookup.DynamicTree.QueryAabb(ref state, ZoneQueryCallback, box, true);
            lookup.StaticTree.QueryAabb(ref state, ZoneQueryCallback, box, true);
            lookup.SundriesTree.QueryAabb(ref state, ZoneQueryCallback, box, true);
            lookup.StaticSundriesTree.QueryAabb(ref state, ZoneQueryCallback, box, true);
        }

        return state;
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
}
