// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Maths;
using Content.Shared.SS220.Zones.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Shared.SS220.Zones.Systems;

public abstract partial class SharedZonesSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public static readonly ProtoId<EntityCategoryPrototype> ZonesCategoryId = "Zones";
    public const string ZoneCommandsPrefix = "zones:";

    public const string DefaultZoneProtoId = "ZoneDefault";

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ZoneComponent>();
        while (query.MoveNext(out var uid, out var zoneComp))
            UpdateInZoneEntities((uid, zoneComp));
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
        var curEntities = GetInZoneEntities(zone);
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
            RaiseLocalEvent(ev);
        }

        foreach (var entity in entitiesToEnter)
        {
            zone.Comp.EnteredEntities.Add(entity);
            var ev = new EnteredZoneEvent(zone, entity);
            RaiseLocalEvent(ev);
        }
    }

    /// <summary>
    /// Returns entities located in the <paramref name="zone"/>.
    /// The check is performed at the <see cref="TransformComponent.Coordinates"/> of the entities.
    /// </summary>
    public IEnumerable<EntityUid> GetInZoneEntities(Entity<ZoneComponent> zone)
    {
        HashSet<EntityUid> entities = [];

        var xform = Transform(zone);
        if (!IsValidParent(xform.ParentUid))
            return entities;

        foreach (var bounds in GetWorldArea(zone))
        {
            foreach (var uid in _entityLookup.GetEntitiesIntersecting(xform.MapID, bounds, LookupFlags.Dynamic | LookupFlags.Static))
            {
                if (InZone(zone, uid))
                    entities.Add(uid);
            }
        }

        return entities;
    }

    /// <summary>
    /// Determines whether the <paramref name="point"/> is located inside the <paramref name="zone"/>.
    /// </summary>
    public bool InZone(Entity<ZoneComponent> ent, EntityCoordinates point)
    {
        if (point.EntityId != ent.Owner)
        {
            return InZone(ent, _transform.ToMapCoordinates(point));
        }

        foreach (var box in ent.Comp.Area)
        {
            if (box.Contains(point.Position))
                return true;
        }

        return false;
    }


    public bool InZone(Entity<ZoneComponent> zone, MapCoordinates point)
    {
        var xform = Transform(zone);
        if (xform.MapID != point.MapId)
            return false;

        return InZone(zone, _transform.ToCoordinates((zone, xform), point));
    }

    /// <inheritdoc cref="InZone(Entity{ZoneComponent}, MapCoordinates, RegionType)"/>
    public bool InZone(Entity<ZoneComponent> zone, EntityUid entity)
    {
        return InZone(zone, _transform.GetMapCoordinates(entity));
    }

    /// <summary>
    /// Returns zones containing a <paramref name="point"/>
    /// </summary>
    public IEnumerable<Entity<ZoneComponent>> GetZonesByPoint(MapCoordinates point)
    {
        HashSet<Entity<ZoneComponent>> result = [];

        var query = AllEntityQuery<ZoneComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (Transform(uid).MapID != point.MapId)
                continue;

            var zone = (uid, comp);
            if (InZone(zone, point))
                result.Add(zone);
        }

        return result;
    }

    public int GetZonesCount()
    {
        var result = 0;
        var query = AllEntityQuery<ZoneComponent>();
        while (query.MoveNext(out _, out _))
            result++;

        return result;
    }

    public Box2 AttachToLattice(EntityUid parent, Box2 box)
    {
        var latticeSize = TryComp<MapGridComponent>(parent, out var mapGrid) ? mapGrid.TileSize : 1f;
        return MathHelperExtensions.AttachToLattice(box, latticeSize);
    }

    public IEnumerable<Box2> AttachToLattice(EntityUid parent, IEnumerable<Box2> area)
    {
        var latticeSize = TryComp<MapGridComponent>(parent, out var mapGrid) ? mapGrid.TileSize : 1f;
        return MathHelperExtensions.AttachToLattice(area, latticeSize);
    }

    public IEnumerable<Box2> RecalculateArea(IEnumerable<Box2> area, EntityUid parent, bool attachToLattice)
    {
        if (attachToLattice)
            area = AttachToLattice(parent, area);

        area = MathHelperExtensions.GetNonOverlappingBoxes(area);
        area = MathHelperExtensions.UnionInEqualSizedBoxes(area);

        return area;
    }

    //public EntityCoordinates? GetRandomCoordinateInZone(Entity<ZoneComponent> zone, RegionType regionType)
    //{
    //    var region = zone.Comp.ZoneParams.GetRegion(regionType);
    //    if (region.Count <= 0)
    //        return null;

    //    var box = _random.Pick(region);
    //    var x = _random.NextFloat(box.Left, box.Right);
    //    var y = _random.NextFloat(box.Bottom, box.Top);

    //    return new EntityCoordinates(zone.Comp.ZoneParams.Container, x, y);
    //}

    public List<Box2Rotated> GetWorldArea(Entity<ZoneComponent> ent)
    {
        var world = new List<Box2Rotated>();
        var area = ent.Comp.Area;
        if (area.Count <= 0)
            return world;

        var parent = Transform(ent).ParentUid;
        var xformQuery = GetEntityQuery<TransformComponent>();
        var (_, containerRot, matrix) = _transform.GetWorldPositionRotationMatrix(parent, xformQuery);
        foreach (var box in area)
        {
            var worldAABB = matrix.TransformBox(box);
            var worldBounds = new Box2Rotated(worldAABB, containerRot, worldAABB.Center);
            world.Add(worldBounds);
        }

        return world;
    }

    public IEnumerable<EntityPrototype> EnumerateZonePrototypes()
    {
        return _prototype.Categories[ZonesCategoryId];
    }

    public bool IsValidParent(EntityUid parent)
    {
        return parent.IsValid()
            && Exists(parent)
            && (HasComp<MapGridComponent>(parent) || HasComp<MapComponent>(parent));
    }

    public EntityUid GetZoneParent(EntityUid zone)
    {
        return Transform(zone).ParentUid;
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
