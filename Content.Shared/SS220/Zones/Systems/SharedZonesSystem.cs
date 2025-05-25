// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Maths;
using Content.Shared.SS220.Zones.Components;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using System.Linq;
using System.Numerics;

namespace Content.Shared.SS220.Zones.Systems;

public abstract partial class SharedZonesSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public const string ZoneCommandsPrefix = "zones:";

    public static EntProtoId<ZoneComponent> BaseZoneId = "BaseZone";
    public static Color DefaultColor = Color.Gray;

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

    /// <summary>
    /// Performs checks on entities located in the <paramref name="zone"/>.
    /// Raises the <see cref="LeavedZoneEvent"/> if entity was in the <paramref name="zone"/> before, but now it isn't.
    /// Raises the <see cref="EnteredZoneEvent"/> if entity wasn't in the <paramref name="zone"/> before, but now it is.
    /// </summary>
    public void ProcessZone(Entity<ZoneComponent> zone)
    {
        var entitiesToLeave = zone.Comp.Entities.ToHashSet();
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
            zone.Comp.Entities.Remove(entity);
            var ev = new LeavedZoneEvent(zone, entity);
            RaiseLocalEvent(zone, ev);
            RaiseLocalEvent(entity, ev);
        }

        foreach (var entity in entitiesToEnter)
        {
            zone.Comp.Entities.Add(entity);
            var ev = new EnteredZoneEvent(zone, entity);
            RaiseLocalEvent(zone, ev);
            RaiseLocalEvent(entity, ev);
        }
    }

    /// <inheritdoc cref="GetEntitiesInZone(Entity{BroadphaseComponent}, Entity{ZoneComponent})"/>
    public IEnumerable<EntityUid> GetEntitiesInZone(Entity<ZoneComponent> zone)
    {
        var container = GetEntity(zone.Comp.ZoneParams?.Container);
        if (!TryComp<BroadphaseComponent>(container, out var broadphase))
            return new HashSet<EntityUid>();

        return GetEntitiesInZone((container.Value, broadphase), zone);
    }

    /// <summary>
    /// Returns entities located in the <paramref name="zone"/>.
    /// The check is performed at the <see cref="TransformComponent.Coordinates"/> of the entities.
    /// </summary>
    public IEnumerable<EntityUid> GetEntitiesInZone(
        Entity<BroadphaseComponent> container,
        Entity<ZoneComponent> zone)
    {
        HashSet<EntityUid> entities = new();
        if (zone.Comp.ZoneParams?.Boxes is not { } boxes)
            return entities;

        var lookup = container.Comp;
        var state = (entities, zone);

        foreach (var box in boxes)
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

    /// <inheritdoc cref="GetBox(Vector2, Vector2)"/>
    public static Box2 GetBox(EntityCoordinates point1, EntityCoordinates point2)
    {
        return GetBox(point1.Position, point2.Position);
    }

    /// <inheritdoc cref="GetBox(Vector2, Vector2)"/>
    public static Box2 GetBox(MapCoordinates point1, MapCoordinates point2)
    {
        return GetBox(point1.Position, point2.Position);
    }

    /// <summary>
    /// Creates a box between two points
    /// </summary>
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

    /// <inheritdoc cref="GetIntegerBox(Vector2, Vector2)"/>
    public static Box2i GetIntegerBox(Box2 box)
    {
        return GetIntegerBox(box.BottomLeft, box.TopRight);
    }

    /// <inheritdoc cref="GetIntegerBox(Vector2, Vector2)"/>
    public static Box2i GetIntegerBox(TileRef tile1, TileRef tile2)
    {
        return GetIntegerBox(tile1.GridIndices, tile2.GridIndices);
    }

    /// <inheritdoc cref="GetIntegerBox(Vector2, Vector2)"/>
    public static Box2i GetIntegerBox(MapCoordinates point1, MapCoordinates point2)
    {
        return GetIntegerBox(point1.Position, point2.Position);
    }

    /// <inheritdoc cref="GetIntegerBox(Vector2, Vector2)"/>
    public static Box2i GetIntegerBox(EntityCoordinates point1, EntityCoordinates point2)
    {
        return GetIntegerBox(point1.Position, point2.Position);
    }

    /// <summary>
    /// Creates a box between two points with integer coordinates
    /// </summary>
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

    /// <summary>
    /// Determines whether the <paramref name="entity"/> is located inside the <paramref name="zone"/>.
    /// The check is performed at the <see cref="TransformComponent.Coordinates"/> of <paramref name="entity"/>
    /// </summary>
    public bool InZone(Entity<ZoneComponent> zone, EntityUid entity)
    {
        return InZone(zone, Transform(entity).Coordinates);
    }

    /// <inheritdoc cref="InZone(Entity{ZoneComponent}, Vector2)"/>
    public bool InZone(Entity<ZoneComponent> zone, MapCoordinates point)
    {
        if (GetEntity(zone.Comp.ZoneParams?.Container) != _map.GetMap(point.MapId))
            return false;

        return InZone(zone, point.Position);
    }

    /// <inheritdoc cref="InZone(Entity{ZoneComponent}, Vector2)"/>
    public bool InZone(Entity<ZoneComponent> zone, EntityCoordinates point)
    {
        if (GetEntity(zone.Comp.ZoneParams?.Container) != point.EntityId)
            return false;

        return InZone(zone, point.Position);
    }

    /// <summary>
    /// Determines whether the <paramref name="point"/> is located inside the <paramref name="zone"/>.
    /// </summary>
    public static bool InZone(Entity<ZoneComponent> zone, Vector2 point)
    {
        if (zone.Comp.ZoneParams?.Boxes is not { } boxes)
            return false;

        foreach (var box in boxes)
        {
            if (box.Contains(point))
                return true;
        }

        return false;
    }

    /// <inheritdoc cref="GetZonesByPoint(Entity{ZonesContainerComponent}, Vector2)"/>
    public IEnumerable<Entity<ZoneComponent>> GetZonesByPoint(MapCoordinates point)
    {
        List<Entity<ZoneComponent>> zones = new();
        var uid = _map.GetMap(point.MapId);
        if (!TryComp<ZonesContainerComponent>(uid, out var zonesContainer))
            return zones;

        return GetZonesByPoint((uid, zonesContainer), point.Position);
    }

    /// <inheritdoc cref="GetZonesByPoint(Entity{ZonesContainerComponent}, Vector2)"/>
    public IEnumerable<Entity<ZoneComponent>> GetZonesByPoint(EntityCoordinates point)
    {
        List<Entity<ZoneComponent>> zones = new();
        if (!TryComp<ZonesContainerComponent>(point.EntityId, out var zonesContainer))
            return zones;

        return GetZonesByPoint((point.EntityId, zonesContainer), point.Position);
    }

    /// <summary>
    /// Returns zones containing a <paramref name="point"/>
    /// </summary>
    public IEnumerable<Entity<ZoneComponent>> GetZonesByPoint(Entity<ZonesContainerComponent> container, Vector2 point)
    {
        List<Entity<ZoneComponent>> zones = new();
        foreach (var zoneNet in container.Comp.Zones)
        {
            var zone = GetEntity(zoneNet);
            if (!TryComp<ZoneComponent>(zone, out var zoneComp))
                continue;

            if (InZone((zone, zoneComp), point))
                zones.Add((zone, zoneComp));
        }

        return zones;
    }

    /// <summary>
    /// Removes intersections of boxes and, if possible, unite adjacent boxes (if this does not affect the total area)
    /// </summary>
    public void RecalculateZoneBoxes(Entity<ZoneComponent> zone)
    {
        if (zone.Comp.ZoneParams is not { } @params)
            return;

        var newboxes = MathHelperExtensions.GetNonOverlappingBoxes(@params.Boxes);
        newboxes = MathHelperExtensions.UnionInEqualSizedBoxes(newboxes);
        @params.Boxes = newboxes.ToHashSet();
        Dirty(zone, zone.Comp);
    }

    public IEnumerable<Box2> GetAttachedToGridBoxes(IEnumerable<Box2> boxes, float gridSize = 1f)
    {
        var attachedBoxes = new List<Box2>();
        foreach (var box in boxes)
            attachedBoxes.AddRange(MathHelperExtensions.GetIntersectsGridBoxes(box, gridSize));

        attachedBoxes = MathHelperExtensions.GetNonOverlappingBoxes(attachedBoxes).ToList();
        attachedBoxes = MathHelperExtensions.UnionInEqualSizedBoxes(attachedBoxes).ToList();
        return attachedBoxes;
    }

    public int GetZonesCount()
    {
        var result = 0;
        var query = EntityQueryEnumerator<ZoneComponent>();
        while (query.MoveNext(out _, out _))
            result++;

        return result;
    }

    public ZoneParamsState GetZoneParams(Entity<ZoneComponent> zone)
    {
        var @params = zone.Comp.ZoneParams;
        var meta = MetaData(zone);
        return new ZoneParamsState
        {
            Container = @params?.Container ?? NetEntity.Invalid,
            ProtoId = @params?.ProtoId ?? BaseZoneId,
            Name = @params?.Name ?? meta.EntityName,
            Color = @params?.Color ?? DefaultColor,
            Boxes = @params?.Boxes ?? new HashSet<Box2>(),
            AttachToGrid = @params?.AttachToGrid ?? false,
        };
    }
}

/// <summary>
/// An event that rises when the entity appears in the zone
/// </summary>
public sealed partial class EnteredZoneEvent(Entity<ZoneComponent> zone, EntityUid entity) : EntityEventArgs
{
    public readonly Entity<ZoneComponent> Zone = zone;
    public readonly EntityUid Entity = entity;
}

/// <summary>
/// An event that rises when the entity disappears from the zone
/// </summary>
public sealed partial class LeavedZoneEvent(Entity<ZoneComponent> zone, EntityUid entity) : EntityEventArgs
{
    public readonly Entity<ZoneComponent> Zone = zone;
    public readonly EntityUid Entity = entity;
}

[Serializable, NetSerializable]
public partial struct ZoneParamsState()
{
    /// <summary>
    /// The entity that this zone is assigned to.
    /// Used to determine local coordinates
    /// </summary>
    public NetEntity Container;

    public string Name = string.Empty;

    public string ProtoId = SharedZonesSystem.BaseZoneId;

    /// <summary>
    /// Current color of the zone
    /// </summary>
    public Color Color = SharedZonesSystem.DefaultColor;

    public bool AttachToGrid = false;

    /// <summary>
    /// Boxes in local coordinates (attached to <see cref="Container"/>) that determine the size of the zone
    /// </summary>
    public HashSet<Box2> Boxes = new();
}
