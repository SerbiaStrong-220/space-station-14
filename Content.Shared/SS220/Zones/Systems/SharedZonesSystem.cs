// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Maths;
using Content.Shared.SS220.Zones.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
using System.Linq;
using System.Numerics;
using static Content.Shared.SS220.Zones.ZoneParams;

namespace Content.Shared.SS220.Zones.Systems;

public abstract partial class SharedZonesSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public const string ZoneCommandsPrefix = "zones:";

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ZoneComponent>();
        while (query.MoveNext(out var uid, out var zoneComp))
        {
            UpdateInZoneEntities((uid, zoneComp));
        }
    }

    /// <summary>
    /// Performs checks on entities located in the <paramref name="zone"/>.
    /// Raises the <see cref="LeavedZoneEvent"/> if entity was in the <paramref name="zone"/> before, but now it isn't.
    /// Raises the <see cref="EnteredZoneEvent"/> if entity wasn't in the <paramref name="zone"/> before, but now it is.
    /// </summary>
    public void UpdateInZoneEntities(Entity<ZoneComponent> zone)
    {
        // shouldn't work on an noninitialized map.
        var map = _transform.GetMap(zone.Owner);
        if (!_map.IsInitialized(map))
            return;

        var entitiesToLeave = zone.Comp.EnteredEntities.ToHashSet();
        var entitiesToEnter = new HashSet<EntityUid>();
        var curEntities = GetInZoneEntities(zone, RegionType.Active);
        foreach (var entity in curEntities)
        {
            if (entitiesToLeave.Remove(entity))
                continue;

            entitiesToEnter.Add(entity);
        }

        foreach (var entity in entitiesToLeave)
        {
            zone.Comp.EnteredEntities.Remove(entity);
            var ev = new LeavedZoneEvent(zone, entity);
            RaiseLocalEvent(zone, ev);
            RaiseLocalEvent(entity, ev);
        }

        foreach (var entity in entitiesToEnter)
        {
            zone.Comp.EnteredEntities.Add(entity);
            var ev = new EnteredZoneEvent(zone, entity);
            RaiseLocalEvent(zone, ev);
            RaiseLocalEvent(entity, ev);
        }
    }

    /// <summary>
    /// Returns entities located in the <paramref name="zone"/>.
    /// The check is performed at the <see cref="TransformComponent.Coordinates"/> of the entities.
    /// </summary>
    public IEnumerable<EntityUid> GetInZoneEntities(Entity<ZoneComponent> zone, RegionType regionType = RegionType.Original)
    {
        HashSet<EntityUid> entities = [];
        var container = zone.Comp.ZoneParams.Container;
        if (!container.IsValid())
            return entities;

        var mapId = Transform(container).MapID;
        foreach (var bounds in GetWorldRegion(zone, regionType))
        {
            foreach (var uid in _entityLookup.GetEntitiesIntersecting(mapId, bounds, LookupFlags.Dynamic | LookupFlags.Static))
            {
                if (InZone(zone, uid, regionType))
                    entities.Add(uid);
            }
        }

        return entities;
    }

    /// <inheritdoc cref="InZone(Entity{ZoneComponent}, MapCoordinates, RegionType)"/>
    public bool InZone(Entity<ZoneComponent> zone, EntityUid entity, RegionType regionType = RegionType.Active)
    {
        return InZone(zone, _transform.GetMapCoordinates(entity), regionType);
    }

    /// <inheritdoc cref="InZone(Entity{ZoneComponent}, MapCoordinates, RegionType)"/>
    public bool InZone(Entity<ZoneComponent> zone, EntityCoordinates point, RegionType regionType = RegionType.Active)
    {
        if (zone.Comp.ZoneParams.Container != point.EntityId)
            return false;

        return InZone(zone, _transform.ToMapCoordinates(point), regionType);
    }

    /// <summary>
    /// Determines whether the <paramref name="point"/> is located inside the <paramref name="zone"/>.
    /// </summary>
    public bool InZone(Entity<ZoneComponent> zone, MapCoordinates point, RegionType regionType = RegionType.Active)
    {
        var container = zone.Comp.ZoneParams.Container;
        if (!container.IsValid())
            return false;

        if (Transform(container).MapID != point.MapId)
            return false;

        var localPos = Vector2.Transform(point.Position, _transform.GetInvWorldMatrix(container));
        foreach (var box in zone.Comp.ZoneParams.GetRegion(regionType))
        {
            if (box.Contains(localPos))
                return true;
        }

        return false;
    }

    /// <inheritdoc cref="GetZonesByPoint(MapCoordinates, RegionType)"/>
    public IEnumerable<Entity<ZoneComponent>> GetZonesByPoint(MapId mapId, Vector2 point, RegionType regionType = RegionType.Active)
    {
        return GetZonesByPoint(new MapCoordinates(point, mapId), regionType);
    }

    /// <inheritdoc cref="GetZonesByPoint(MapCoordinates, RegionType)"/>
    public IEnumerable<Entity<ZoneComponent>> GetZonesByPoint(EntityCoordinates point, RegionType regionType = RegionType.Active)
    {
        return GetZonesByPoint(_transform.ToMapCoordinates(point), regionType);
    }

    /// <summary>
    /// Returns zones containing a <paramref name="point"/>
    /// </summary>
    public IEnumerable<Entity<ZoneComponent>> GetZonesByPoint(MapCoordinates point, RegionType regionType = RegionType.Active)
    {
        HashSet<Entity<ZoneComponent>> result = new();

        var query = AllEntityQuery<ZonesContainerComponent>();
        while (query.MoveNext(out var uid, out var container))
        {
            if (Transform(uid).MapID != point.MapId)
                continue;

            foreach (var zone in GetZonesFromContainer((uid, container)))
            {
                if (InZone(zone, point, regionType))
                    result.Add(zone);
            }
        }

        return result;
    }

    /// <summary>
    /// Removes intersections of boxes and, if possible, unite adjacent boxes (if this does not affect the total area)
    /// </summary>
    public void RecalculateZoneRegions(Entity<ZoneComponent> zone)
    {
        zone.Comp.ZoneParams.RecalculateRegions();
        Dirty(zone, zone.Comp);
    }

    /// <inheritdoc cref="AttachToGrid(EntityUid, Box2)"/>
    public Box2 AttachToGrid(NetEntity container, Box2 box)
    {
        return AttachToGrid(GetEntity(container), box);
    }

    /// <summary>
    /// Creates a new <see cref="Box2"/> based on the <paramref name="box"/>.
    /// It aligns the original box to fit within the grid.
    /// </summary>
    public Box2 AttachToGrid(EntityUid container, Box2 box)
    {
        if (TryComp<MapGridComponent>(container, out var mapGrid))
            return MathHelperExtensions.AttachToGrid(box, mapGrid.TileSize);

        return MathHelperExtensions.AttachToGrid(box);
    }

    /// <inheritdoc cref="AttachToGrid(EntityUid, ref Box2)"/>
    public void AttachToGrid(NetEntity container, ref Box2 box)
    {
        AttachToGrid(GetEntity(container), ref box);
    }

    /// <summary>
    /// Changes the input <paramref name="box"/> by creating a new <see cref="Box2"/> based on the <paramref name="box"/>.
    /// It aligns the original box to fit within the grid.
    /// </summary>
    public void AttachToGrid(EntityUid container, ref Box2 box)
    {
        box = AttachToGrid(container, box);
    }

    /// <inheritdoc cref="AttachToGrid(EntityUid, IEnumerable{Box2})"/>
    public IEnumerable<Box2> AttachToGrid(NetEntity container, IEnumerable<Box2> boxes)
    {
        return AttachToGrid(GetEntity(container), boxes);
    }

    /// <summary>
    /// Creates a new array of <see cref="Box2"/> based on the <paramref name="boxes"/>.
    /// It aligns the original box to fit within the grid.
    /// </summary>
    public IEnumerable<Box2> AttachToGrid(EntityUid container, IEnumerable<Box2> boxes)
    {
        if (TryComp<MapGridComponent>(container, out var mapGrid))
            return MathHelperExtensions.AttachToGrid(boxes, mapGrid.TileSize);

        return MathHelperExtensions.AttachToGrid(boxes);
    }

    /// <inheritdoc cref="AttachToGrid(EntityUid, ref IEnumerable{Box2})"/>
    public void AttachToGrid(NetEntity container, ref IEnumerable<Box2> boxes)
    {
        AttachToGrid(GetEntity(container), ref boxes);
    }

    /// <summary>
    /// Changes the input <paramref name="boxes"/> by creating a new array of <see cref="Box2"/> based on the <paramref name="boxes"/>.
    /// It aligns the original box to fit within the grid.
    /// </summary>
    public void AttachToGrid(EntityUid container, ref IEnumerable<Box2> boxes)
    {
        boxes = AttachToGrid(container, boxes);
    }

    public int GetZonesCount()
    {
        var result = 0;
        var query = AllEntityQuery<ZoneComponent>();
        while (query.MoveNext(out _, out _))
            result++;

        return result;
    }

    /// <inheritdoc cref="CutSpace(Entity{MapGridComponent}, IEnumerable{Box2}, out IEnumerable{Box2})"/>
    public IEnumerable<Box2> CutSpace(NetEntity parent, IEnumerable<Box2> boxes, out IEnumerable<Box2> spaceBoxes)
    {
        return CutSpace(GetEntity(parent), boxes, out spaceBoxes);
    }

    /// <inheritdoc cref="CutSpace(Entity{MapGridComponent}, IEnumerable{Box2}, out IEnumerable{Box2})"/>
    public IEnumerable<Box2> CutSpace(EntityUid parent, IEnumerable<Box2> boxes, out IEnumerable<Box2> spaceBoxes)
    {
        spaceBoxes = [];
        if (!TryComp<MapGridComponent>(parent, out var mapGrid))
            return [];

        return CutSpace((parent, mapGrid), boxes, out spaceBoxes);
    }

    /// <summary>
    /// Cuts out the area located in space from the input <paramref name="boxes"/>
    /// </summary>
    public IEnumerable<Box2> CutSpace(Entity<MapGridComponent> grid, IEnumerable<Box2> boxes, out IEnumerable<Box2> spaceBoxes)
    {
        spaceBoxes = GetSpaceBoxes(grid, boxes);
        return MathHelperExtensions.SubstructBox(boxes, spaceBoxes);
    }

    /// <inheritdoc cref="CutSpace(Entity{MapGridComponent}, IEnumerable{Box2}, out IEnumerable{Box2})"/>
    public void CutSpace(NetEntity parent, ref IEnumerable<Box2> boxes, out IEnumerable<Box2> spaceBoxes)
    {
        CutSpace(GetEntity(parent), ref boxes, out spaceBoxes);
    }

    /// <inheritdoc cref="CutSpace(Entity{MapGridComponent}, IEnumerable{Box2}, out IEnumerable{Box2})"/>
    public void CutSpace(EntityUid parent, ref IEnumerable<Box2> boxes, out IEnumerable<Box2> spaceBoxes)
    {
        spaceBoxes = [];
        if (!TryComp<MapGridComponent>(parent, out var mapGrid))
        {
            boxes = [];
            return;
        }

        CutSpace((parent, mapGrid), ref boxes, out spaceBoxes);
    }

    /// <inheritdoc cref="CutSpace(Entity{MapGridComponent}, IEnumerable{Box2}, out IEnumerable{Box2})"/>
    public void CutSpace(Entity<MapGridComponent> grid, ref IEnumerable<Box2> boxes, out IEnumerable<Box2> spaceBoxes)
    {
        boxes = CutSpace(grid, boxes, out spaceBoxes);
    }

    /// <inheritdoc cref="GetSpaceBoxes(EntityUid, IEnumerable{Box2})"/>
    public IEnumerable<Box2> GetSpaceBoxes(NetEntity parent, IEnumerable<Box2> boxes)
    {
        return GetSpaceBoxes(GetEntity(parent), boxes);
    }

    /// <summary>
    /// Returns the area located in space from the input <paramref name="boxes"/>
    /// </summary>
    public IEnumerable<Box2> GetSpaceBoxes(EntityUid parent, IEnumerable<Box2> boxes)
    {
        if (!TryComp<MapGridComponent>(parent, out var mapGrid))
            return boxes;

        var result = new List<Box2>();
        var gridBoxes = MathHelperExtensions.GetIntersectsGridBoxes(boxes, mapGrid.TileSize);
        foreach (var gridBox in gridBoxes)
        {
            var coords = new EntityCoordinates(parent, gridBox.Center);
            var tileRef = _map.GetTileRef((parent, mapGrid), coords);
            if (tileRef.Tile.IsEmpty)
                result.Add(gridBox);
        }

        var excess = MathHelperExtensions.SubstructBox(result, boxes);
        return MathHelperExtensions.SubstructBox(result, excess);
    }

    public EntityCoordinates? GetRandomCoordinateInZone(Entity<ZoneComponent> zone, RegionType regionType)
    {
        var region = zone.Comp.ZoneParams.GetRegion(regionType);
        if (region.Count <= 0)
            return null;

        var box = _random.Pick(region);
        var x = _random.NextFloat(box.Left, box.Right);
        var y = _random.NextFloat(box.Bottom, box.Top);

        return new EntityCoordinates(zone.Comp.ZoneParams.Container, x, y);
    }

    public bool IsValidContainer(NetEntity netEntity)
    {
        return IsValidContainer(GetEntity(netEntity));
    }

    public bool IsValidContainer(EntityUid uid)
    {
        return uid.IsValid() && (HasComp<MapComponent>(uid) || HasComp<MapGridComponent>(uid));
    }

    public List<Box2Rotated> GetWorldRegion(Entity<ZoneComponent> zone, RegionType regionType)
    {
        var world = new List<Box2Rotated>();
        var local = zone.Comp.ZoneParams.GetRegion(regionType);
        if (local.Count <= 0)
            return world;

        var container = zone.Comp.ZoneParams.Container;
        var xformQuery = GetEntityQuery<TransformComponent>();
        var (_, containerRot, matrix) = _transform.GetWorldPositionRotationMatrix(container, xformQuery);
        foreach (var box in local)
        {
            var worldAABB = matrix.TransformBox(box);
            var worldBounds = new Box2Rotated(worldAABB, containerRot, worldAABB.Center);
            world.Add(worldBounds);
        }

        return world;
    }

    public HashSet<Entity<ZoneComponent>> GetZonesFromContainer(Entity<ZonesContainerComponent> entity)
    {
        HashSet<Entity<ZoneComponent>> result = new();
        foreach (var netUid in entity.Comp.Zones)
        {
            var uid = GetEntity(netUid);
            if (TryComp<ZoneComponent>(uid, out var comp))
                result.Add((uid, comp));
        }

        return result;
    }

    public static bool NeedRecreate(ZoneParams originalParams, ZoneParams newParams)
    {
        return newParams.Container != originalParams.Container ||
            newParams.ProtoID != originalParams.ProtoID;
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
