
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
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ZonesDataComponent, ComponentShutdown>(OnZoneDataShutdown);

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

    private void OnZoneDataShutdown(Entity<ZonesDataComponent> entity, ref ComponentShutdown args)
    {
        foreach (var zone in entity.Comp.Zones)
            DeleteZone(GetEntity(zone));
    }

    private void OnZoneShutdown(Entity<ZoneComponent> entity, ref ComponentShutdown args)
    {
        DeleteZone(entity.Owner);
    }

    public Entity<ZoneComponent>? CreateZone(EntityUid parent,
        IEnumerable<(EntityCoordinates, EntityCoordinates)> boxCoordinates,
        bool attachToGrid = false,
        EntProtoId<ZoneComponent>? zoneProto = null)
    {
        var vectors = boxCoordinates.Select(e =>
        {
            var p1 = e.Item1;
            var p2 = e.Item2;

            if (p1.EntityId != parent)
                throw new ArgumentException($"Entity {parent} doesn't contains coordinate {p1}");

            if (p2.EntityId != parent)
                throw new ArgumentException($"Entity {parent} doesn't contains coordinate {p2}");

            var v1 = new Vector2(p1.X, p1.Y);
            var v2 = new Vector2(p2.X, p2.Y);
            return (v1, v2);
        });

        return CreateZone(parent, vectors, attachToGrid, zoneProto);
    }

    public Entity<ZoneComponent>? CreateZone(EntityUid parent,
        IEnumerable<(MapCoordinates, MapCoordinates)> boxCoordinates,
        bool attachToGrid = false,
        EntProtoId<ZoneComponent>? zoneProto = null)
    {
        if (!TryComp<MapComponent>(parent, out var mapComponent))
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

        return CreateZone(parent, vectors, attachToGrid, zoneProto);
    }

    public Entity<ZoneComponent>? CreateZone(EntityUid parent,
        IEnumerable<(Vector2, Vector2)> vectors,
        bool attachToGrid = false,
        EntProtoId<ZoneComponent>? zoneProto = null)
    {
        if (attachToGrid)
        {
            var boxes = vectors.Select(e => GetIntegerBox(e.Item1, e.Item2));
            return CreateZone(parent, boxes, zoneProto);
        }
        else
        {
            var boxes = vectors.Select(e => GetBox(e.Item1, e.Item2));
            return CreateZone(parent, boxes, zoneProto);
        }
    }

    public Entity<ZoneComponent>? CreateZone(EntityUid parent, IEnumerable<Box2i> boxes, EntProtoId<ZoneComponent>? zoneProto = null)
    {
        var array = boxes.Select(b => new Box2(b.BottomLeft, b.TopRight));
        return CreateZone(parent, array, zoneProto);
    }

    public Entity<ZoneComponent>? CreateZone(EntityUid parent, IEnumerable<Box2> boxes, EntProtoId<ZoneComponent>? zoneProto = null)
    {
        var boxesHash = boxes.ToHashSet();
        if (boxesHash.Count <= 0)
            return null;

        zoneProto ??= BaseZoneId;
        var zone = Spawn(zoneProto, Transform(parent).Coordinates);
        _transform.AttachToGridOrMap(zone);

        var zoneComp = EnsureComp<ZoneComponent>(zone);
        zoneComp.Parent = GetNetEntity(parent);
        zoneComp.Boxes = boxesHash;
        Dirty(zone, zoneComp);

        var zonesData = EnsureComp<ZonesDataComponent>(parent);
        zonesData.Zones.Add(GetNetEntity(zone));
        Dirty(parent, zonesData);

        return (zone, zoneComp);
    }

    public void DeleteZone(Entity<ZoneComponent?> zone)
    {
        if (!Resolve(zone, ref zone.Comp))
            return;

        if (zone.Comp.Parent is not { } parent)
            return;

        DeleteZone(GetEntity(parent), zone);
    }

    public void DeleteZone(Entity<ZonesDataComponent?> parent, Entity<ZoneComponent?> zone)
    {
        if (!Resolve(parent, ref parent.Comp) ||
            !Resolve(zone, ref zone.Comp))
            return;

        parent.Comp.Zones.Remove(GetNetEntity(zone));
        QueueDel(zone);
    }
}
