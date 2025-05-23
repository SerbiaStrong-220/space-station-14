// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Zones.Components;
using Content.Shared.SS220.Zones.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using System.Linq;
using System.Numerics;

namespace Content.Server.SS220.Zones.Systems;

public sealed partial class ZonesSystem : SharedZonesSystem
{
    [Dependency] private readonly TransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ZonesContainerComponent, ComponentShutdown>(OnZonesContainerShutdown);

        SubscribeLocalEvent<ZoneComponent, ComponentShutdown>(OnZoneShutdown);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ZoneComponent>();
        while (query.MoveNext(out var uid, out var zoneComp))
        {
            var ents = GetEntitiesInZone((uid, zoneComp));
        }
    }

    private void OnZonesContainerShutdown(Entity<ZonesContainerComponent> entity, ref ComponentShutdown args)
    {
        foreach (var zone in entity.Comp.Zones)
            DeleteZone(GetEntity(zone));
    }

    private void OnZoneShutdown(Entity<ZoneComponent> entity, ref ComponentShutdown args)
    {
        DeleteZone(entity.Owner);
    }

    /// <inheritdoc cref="CreateZone(EntityUid, IEnumerable{Box2}, EntProtoId{ZoneComponent}?)"/>
    /// <param name="boxCoordinates">Contains the coordinates for which the boxes will be created</param>
    /// <param name="boundToGrid">Should the coordinates of the box be bound to the grid</param>
    /// <returns></returns>
    public Entity<ZoneComponent>? CreateZone(EntityUid container,
        IEnumerable<(EntityCoordinates, EntityCoordinates)> boxCoordinates,
        bool boundToGrid = false,
        EntProtoId<ZoneComponent>? zoneProto = null)
    {
        var vectors = boxCoordinates.Select(e =>
        {
            var p1 = e.Item1;
            var p2 = e.Item2;

            if (p1.EntityId != container)
                throw new ArgumentException($"Entity {container} doesn't contains coordinate {p1}");

            if (p2.EntityId != container)
                throw new ArgumentException($"Entity {container} doesn't contains coordinate {p2}");

            var v1 = new Vector2(p1.X, p1.Y);
            var v2 = new Vector2(p2.X, p2.Y);
            return (v1, v2);
        });

        return CreateZone(container, vectors, boundToGrid, zoneProto);
    }

    /// <inheritdoc cref="CreateZone(EntityUid, IEnumerable{Box2}, EntProtoId{ZoneComponent}?)"/>
    /// <param name="boxCoordinates">Contains the coordinates for which the boxes will be created</param>
    /// <param name="boundToGrid">Should the coordinates of the box be bound to the grid</param>
    /// <returns></returns>
    public Entity<ZoneComponent>? CreateZone(EntityUid container,
        IEnumerable<(MapCoordinates, MapCoordinates)> boxCoordinates,
        bool boundToGrid = false,
        EntProtoId<ZoneComponent>? zoneProto = null)
    {
        if (!TryComp<MapComponent>(container, out var mapComponent))
            return null;

        var vectors = boxCoordinates.Select(e =>
        {
            var p1 = e.Item1;
            var p2 = e.Item2;

            if (p1.MapId != mapComponent.MapId)
                throw new ArgumentException($"The coordinate was obtained from map {p1.MapId}, when it should be from map {mapComponent.MapId}");

            if (p2.MapId != mapComponent.MapId)
                throw new ArgumentException($"The coordinate was obtained from map {p2.MapId}, when it should be from map {mapComponent.MapId}");

            var v1 = new Vector2(p1.X, p1.Y);
            var v2 = new Vector2(p2.X, p2.Y);
            return (v1, v2);
        });

        return CreateZone(container, vectors, boundToGrid, zoneProto);
    }

    /// <inheritdoc cref="CreateZone(EntityUid, IEnumerable{Box2}, EntProtoId{ZoneComponent}?)"/>
    /// <param name="vectors">Contains the coordinates for which the boxes will be created</param>
    /// <param name="boundToGrid">Should the coordinates of the box be bound to the grid</param>
    /// <returns></returns>
    public Entity<ZoneComponent>? CreateZone(EntityUid container,
        IEnumerable<(Vector2, Vector2)> vectors,
        bool boundToGrid = false,
        EntProtoId<ZoneComponent>? zoneProto = null)
    {
        if (boundToGrid)
        {
            var boxes = vectors.Select(e => GetIntegerBox(e.Item1, e.Item2));
            return CreateZone(container, boxes, zoneProto);
        }
        else
        {
            var boxes = vectors.Select(e => GetBox(e.Item1, e.Item2));
            return CreateZone(container, boxes, zoneProto);
        }
    }

    /// <inheritdoc cref="CreateZone(EntityUid, IEnumerable{Box2}, EntProtoId{ZoneComponent}?)"/>
    public Entity<ZoneComponent>? CreateZone(EntityUid container, IEnumerable<Box2i> boxes, EntProtoId<ZoneComponent>? zoneProto = null)
    {
        var array = boxes.Select(b => new Box2(b.BottomLeft, b.TopRight));
        return CreateZone(container, array, zoneProto);
    }

    /// <summary>
    /// Creates a new zone
    /// </summary>
    public Entity<ZoneComponent>? CreateZone(EntityUid container, IEnumerable<Box2> boxes, EntProtoId<ZoneComponent>? zoneProto = null)
    {
        var boxesHash = boxes.ToHashSet();
        if (boxesHash.Count <= 0)
            return null;

        zoneProto ??= BaseZoneId;
        var zone = Spawn(zoneProto, Transform(container).Coordinates);
        _transform.AttachToGridOrMap(zone);

        var zoneComp = EnsureComp<ZoneComponent>(zone);
        zoneComp.Container = GetNetEntity(container);
        zoneComp.Boxes = boxesHash;
        Dirty(zone, zoneComp);

        var zonesContainer = EnsureComp<ZonesContainerComponent>(container);
        zonesContainer.Zones.Add(GetNetEntity(zone));
        Dirty(container, zonesContainer);

        return (zone, zoneComp);
    }

    /// <inheritdoc cref="DeleteZone(Entity{ZonesContainerComponent?}, Entity{ZoneComponent?})"/>
    public void DeleteZone(Entity<ZoneComponent?> zone)
    {
        if (!Resolve(zone, ref zone.Comp))
            return;

        if (zone.Comp.Container is not { } parent)
            return;

        DeleteZone(GetEntity(parent), zone);
    }

    /// <summary>
    /// Deletes the <paramref name="zone"/>
    /// </summary>
    public void DeleteZone(Entity<ZonesContainerComponent?> container, Entity<ZoneComponent?> zone)
    {
        if (!Resolve(container, ref container.Comp) ||
            !Resolve(zone, ref zone.Comp))
            return;

        container.Comp.Zones.Remove(GetNetEntity(zone));
        QueueDel(zone);
    }
}
