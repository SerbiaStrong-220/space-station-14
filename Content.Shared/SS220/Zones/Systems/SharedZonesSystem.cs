// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Maths;
using Content.Shared.SS220.Zones.Components;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Shared.SS220.Zones.Systems;

public abstract partial class SharedZonesSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly INetManager _net = default!;

    public const string ZoneCommandsPrefix = "zones:";

    public const string DefaultZoneProtoId = "ZoneDefault";

    public static readonly ProtoId<EntityCategoryPrototype> ZonesCategoryId = "Zones";

    protected ZonesRelationUpdateData RelationUpdateData = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ZoneComponent, MapInitEvent>(OnZoneMapInit);
        SubscribeLocalEvent<ZoneComponent, ComponentShutdown>(OnZoneShutdown);

        SubscribeLocalEvent<InZoneComponent, MapInitEvent>(OnInZoneMapInit);
        SubscribeLocalEvent<InZoneComponent, ComponentShutdown>(OnInZoneShutdown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateZonesRelations();
    }

    protected virtual void OnZoneMapInit(Entity<ZoneComponent> entity, ref MapInitEvent args)
    {
    }

    protected virtual void OnZoneShutdown(Entity<ZoneComponent> entity, ref ComponentShutdown args)
    {
        foreach (var netUid in entity.Comp.LocatedEntities)
            TryHandleLeaveZone(entity, GetEntity(netUid));
    }

    protected virtual void OnInZoneMapInit(Entity<InZoneComponent> entity, ref MapInitEvent args)
    {
        UpdateInZone(entity);
    }

    protected virtual void OnInZoneShutdown(Entity<InZoneComponent> entity, ref ComponentShutdown args)
    {
        foreach (var netZone in entity.Comp.Zones)
        {
            var zoneUid = GetEntity(netZone);
            if (!TryComp<ZoneComponent>(zoneUid, out var zoneComp))
                continue;

            TryHandleLeaveZone((zoneUid, zoneComp), entity);
        }
    }

    #region Public API
    /// <summary>
    /// Gets all entities located in the <paramref name="zone"/>.
    /// </summary>
    /// <param name="useCache">Should the result be used from the cache, or should be calculated</param>
    public IEnumerable<EntityUid> GetInZoneEntities(Entity<ZoneComponent> zone, bool useCache = true)
    {
        if (useCache)
        {
            foreach (var uid in zone.Comp.LocatedEntities)
                yield return GetEntity(uid);

            yield break;
        }

        var xform = Transform(zone);
        if (!IsValidParent(xform.ParentUid))
            yield break;

        foreach (var bounds in GetWorldArea(zone))
        {
            foreach (var uid in _entityLookup.GetEntitiesIntersecting(xform.MapID, bounds, LookupFlags.All))
            {
                if (InZone(zone, uid, useCache: false))
                    yield return uid;
            }
        }
    }

    /// <summary>
    /// Determines whether the <paramref name="entity"/> is located in the <paramref name="zone"/>.
    /// </summary>
    /// <param name="useCache">Should the result be used from the cache, or should be calculated</param>
    public bool InZone(Entity<ZoneComponent> zone, EntityUid entity, bool useCache = true)
    {
        if (useCache)
            return zone.Comp.LocatedEntities.Contains(GetNetEntity(entity));

        return InZone(zone, _transform.GetMapCoordinates(entity));
    }

    /// <<inheritdoc cref="InZone(Entity{ZoneComponent}, EntityCoordinates)"/>
    public bool InZone(Entity<ZoneComponent> zone, MapCoordinates point)
    {
        var xform = Transform(zone);
        if (xform.MapID != point.MapId)
            return false;

        return InZone(zone, _transform.ToCoordinates((zone, xform), point));
    }

    /// <summary>
    /// Determines whether the <paramref name="point"/> is located in the <paramref name="zone"/>.
    /// </summary>
    public bool InZone(Entity<ZoneComponent> zone, EntityCoordinates point)
    {
        if (point.EntityId != zone.Owner)
            return InZone(zone, _transform.ToMapCoordinates(point));

        foreach (var box in zone.Comp.Area)
        {
            if (box.Contains(point.Position))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Gets all zones containing the <paramref name="uid"/>
    /// </summary>
    /// <param name="useCache">Should the result be used from the cache, or should be calculated</param>
    public IEnumerable<Entity<ZoneComponent>> GetZonesByEntity(EntityUid uid, bool useCache = true)
    {
        if (useCache)
        {
            if (!TryComp<InZoneComponent>(uid, out var inZone))
                yield break;

            foreach (var netZone in inZone.Zones)
            {
                var zoneUid = GetEntity(netZone);
                if (!TryComp<ZoneComponent>(uid, out var zoneComp))
                    continue;

                yield return (zoneUid, zoneComp);
            }

            yield break;
        }

        foreach (var zone in GetZonesByPoint(_transform.GetMapCoordinates(uid)))
            yield return zone;
    }

    /// <summary>
    /// Gets all zones containing the <paramref name="point"/>
    /// </summary>
    public IEnumerable<Entity<ZoneComponent>> GetZonesByPoint(MapCoordinates point)
    {
        var query = AllEntityQuery<ZoneComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (Transform(uid).MapID != point.MapId)
                continue;

            var zone = (uid, comp);
            if (InZone(zone, point))
                yield return zone;
        }
    }

    /// <summary>
    /// Gets the count of all existing zones
    /// </summary>
    public int GetZonesCount()
    {
        var result = 0;
        var query = AllEntityQuery<ZoneComponent>();
        while (query.MoveNext(out _, out _))
            result++;

        return result;
    }

    /// <summary>
    /// Enumerates all <see cref="EntityPrototype"/> with the category <see cref="ZonesCategoryId"/>
    /// </summary>
    public IEnumerable<EntityPrototype> EnumerateZonePrototypes()
    {
        return _prototype.Categories[ZonesCategoryId];
    }

    /// <summary>
    /// Is the <paramref name="parent"> valid for zone attachment
    /// </summary>
    public bool IsValidParent(EntityUid parent)
    {
        return parent.IsValid()
            && Exists(parent)
            && (HasComp<MapGridComponent>(parent) || HasComp<MapComponent>(parent));
    }

    public List<CompletionOption> GetZonesListCompletionOption()
    {
        var result = new List<CompletionOption>();

        var query = AllEntityQuery<ZoneComponent, MetaDataComponent>();
        while (query.MoveNext(out var uid, out _, out var metaDataComp))
        {
            var option = new CompletionOption(GetNetEntity(uid).ToString())
            {
                Hint = metaDataComp.EntityName
            };
            result.Add(option);
        }

        return result;
    }
    #endregion

    #region Boxes API
    public Box2 AttachToGrid(Box2 box, EntityUid parent)
    {
        var gridSize = TryComp<MapGridComponent>(parent, out var mapGrid) ? mapGrid.TileSize : 1f;
        return Box2Helper.AttachToGrid(box, gridSize);
    }

    public List<Box2> AttachToGrid(List<Box2> area, EntityUid parent)
    {
        var gridSize = TryComp<MapGridComponent>(parent, out var mapGrid) ? mapGrid.TileSize : 1f;
        return Box2Helper.AttachToGrid(area, gridSize);
    }

    public List<Box2> RecalculateArea(List<Box2> area, EntityUid parent, bool attachToGrid = false)
    {
        if (attachToGrid)
            area = AttachToGrid(area, parent);

        area = Box2Helper.GetNonOverlappingBoxes(area);
        area = Box2Helper.UnionInEqualSizedBoxes(area);

        return area;
    }

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
    #endregion

    public abstract bool RegisterRelationUpdate(EntityUid uid, object registrator);

    public abstract bool RegisterRelationUpdate(Type componentType, object registrator);

    public virtual bool RegisterRelationUpdate<T>(object registrator) where T : IComponent
    {
        return RegisterRelationUpdate(typeof(T), registrator);
    }

    public abstract bool UnregisterRelationUpdate(EntityUid uid, object registrator);

    public abstract bool UnregisterRelationUpdate(Type componentType, object registrator);

    public virtual bool UnregisterRelationUpdate<T>(object registrator) where T : IComponent
    {
        return UnregisterRelationUpdate(typeof(T), registrator);
    }

    public abstract bool UnregisterRelationUpdateForced(EntityUid uid);

    public abstract bool UnregisterRelationUpdateForced(Type componentType);

    public virtual bool UnregisterRelationUpdateForced<T>()
    {
        return UnregisterRelationUpdateForced(typeof(T));
    }

    protected void UpdateZonesRelations()
    {
        foreach (var uid in RelationUpdateData.EnumerateAll(EntityManager))
            UpdateInZone(uid);
    }

    /// <summary>
    /// Performs checks on entities located in the <paramref name="zone"/>.
    /// Raises the <see cref="EntityLeavedZoneEvent"/> if entity was in the <paramref name="zone"/> before, but now it isn't.
    /// Raises the <see cref="EntityEnteredZoneEvent"/> if entity wasn't in the <paramref name="zone"/> before, but now it is.
    /// </summary>
    protected void UpdateEntitiesInZone(Entity<ZoneComponent> zone)
    {
        // should work only at initialized map
        var map = _transform.GetMap(zone.Owner);
        if (!_map.IsInitialized(map))
            return;

        var entitiesToLeave = zone.Comp.LocatedEntities.Select(GetEntity).ToHashSet();
        foreach (var entity in GetInZoneEntities(zone, useCache: false))
        {
            if (entitiesToLeave.Remove(entity))
                continue;

            TryHandleEnterZone(zone, entity);
        }

        foreach (var entity in entitiesToLeave)
            TryHandleLeaveZone(zone, entity);
    }

    protected void UpdateInZone(EntityUid uid)
    {
        var toLeave = new HashSet<NetEntity>();
        if (TryComp<InZoneComponent>(uid, out var inZone))
            toLeave = [.. inZone.Zones];

        // should work only at initialized map.
        var map = _transform.GetMap(uid);
        if (_map.IsInitialized(map))
        {
            foreach (var zone in GetZonesByEntity(uid, useCache: false))
            {
                TryHandleEnterZone(zone, uid);
                toLeave.Remove(GetNetEntity(zone));
            }
        }

        foreach (var netZone in toLeave)
        {
            var zoneUid = GetEntity(netZone);
            if (!TryComp<ZoneComponent>(zoneUid, out var zoneComp))
                continue;

            TryHandleLeaveZone((zoneUid, zoneComp), uid);
        }
    }

    private bool TryHandleEnterZone(Entity<ZoneComponent> zone, EntityUid uid)
    {
        if (!zone.Comp.LocatedEntities.Add(GetNetEntity(uid)))
            return false;

        var inZone = EnsureComp<InZoneComponent>(uid);
        inZone.Zones.Add(GetNetEntity(zone));

        var ev = new EntityEnteredZoneEvent(zone, uid);
        RaiseLocalEvent(zone, ev);
        RaiseLocalEvent(uid, ev);

        Dirty(zone);
        Dirty(uid, inZone);
        return true;
    }

    private bool TryHandleLeaveZone(Entity<ZoneComponent> zone, EntityUid uid)
    {
        if (!zone.Comp.LocatedEntities.Remove(GetNetEntity(uid)))
            return false;

        if (TryComp<InZoneComponent>(uid, out var inZone))
        {
            inZone.Zones.Remove(GetNetEntity(zone));

            if (_net.IsServer)
            {
                if (inZone.Zones.Count <= 0)
                    RemComp<InZoneComponent>(uid);
                else
                    Dirty(uid, inZone);
            }
        }

        var ev = new EntityLeavedZoneEvent(zone, uid);
        RaiseLocalEvent(zone, ev);
        RaiseLocalEvent(uid, ev);

        Dirty(zone);
        return true;
    }

    protected struct ZonesRelationUpdateData()
    {
        public Dictionary<EntityUid, HashSet<object>> Entities = [];

        public Dictionary<Type, HashSet<object>> Components = [];

        public readonly bool RegisterEntity(EntityUid uid, object registrator)
        {
            if (Entities.TryGetValue(uid, out var regs))
                return regs.Add(registrator);

            Entities.Add(uid, [registrator]);
            return true;
        }

        public readonly bool RegisterComponent(Type componentType, object registrator)
        {
            if (!componentType.IsAssignableTo(typeof(IComponent)))
                return false;

            if (Components.TryGetValue(componentType, out var regs))
                regs.Add(registrator);

            Components.Add(componentType, [registrator]);
            return true;
        }

        public readonly bool UnregisterEntity(EntityUid uid, object registrator)
        {
            if (!Entities.TryGetValue(uid, out var regs))
                return false;

            var result = regs.Remove(registrator);
            if (regs.Count <= 0)
                return Entities.Remove(uid);

            return result;
        }

        public readonly bool UnregisterEntityForced(EntityUid uid)
        {
            return Entities.Remove(uid);
        }

        public readonly bool UnregisterComponent(Type componentType, object registrator)
        {
            if (!Components.TryGetValue(componentType, out var regs))
                return false;

            var result = regs.Remove(registrator);
            if (regs.Count <= 0)
                return Components.Remove(componentType);

            return result;
        }

        public readonly bool UnregisterComponentForced(Type componentType)
        {
            return Components.Remove(componentType);
        }

        public readonly IEnumerable<EntityUid> EnumerateAll(IEntityManager entityManager)
        {
            foreach (var uid in EnumerateEntities())
                yield return uid;

            foreach (var uid in EnumerateComponents(entityManager))
                yield return uid;
        }

        public readonly IEnumerable<EntityUid> EnumerateEntities()
        {
            foreach (var uid in Entities.Keys)
                yield return uid;
        }

        public readonly IEnumerable<EntityUid> EnumerateComponents(IEntityManager entityManager)
        {
            foreach (var type in Components.Keys)
            {
                // AllQuery is used because non-generic overload for EntityQueryEnumerator isn't exist
                var query = entityManager.AllEntityQueryEnumerator(type);
                while (query.MoveNext(out var uid, out _))
                {
                    if (entityManager.Deleted(uid))
                        continue;

                    if (entityManager.IsPaused(uid))
                        continue;

                    yield return uid;
                }
            }

        }
    }
}

/// <summary>
/// An event that rises when the entity appears in the zone
/// </summary>
public sealed partial class EntityEnteredZoneEvent(Entity<ZoneComponent> zone, EntityUid entity) : EntityEventArgs
{
    public readonly Entity<ZoneComponent> Zone = zone;
    public readonly EntityUid Entity = entity;
}

/// <summary>
/// An event that rises when the entity disappears from the zone
/// </summary>
public sealed partial class EntityLeavedZoneEvent(Entity<ZoneComponent> zone, EntityUid entity) : EntityEventArgs
{
    public readonly Entity<ZoneComponent> Zone = zone;
    public readonly EntityUid Entity = entity;
}
