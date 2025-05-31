// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Zones.Components;
using Content.Shared.SS220.Zones.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using System.Linq;
using System.Numerics;

namespace Content.Server.SS220.Zones.Systems;

public sealed partial class ZonesSystem : SharedZonesSystem
{
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly MapSystem _map = default!;

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
        ClearZonesContainer(entity);
    }

    private void OnZoneShutdown(Entity<ZoneComponent> entity, ref ComponentShutdown args)
    {
        DeleteZone(entity.Owner);
    }

    /// <inheritdoc cref="CreateZone(ZoneParamsState)"/>
    public Entity<ZoneComponent>? CreateZone(
        IEnumerable<(EntityCoordinates, EntityCoordinates)> boxCoordinates,
        string? protoId = null,
        string? name = null,
        Color? color = null,
        bool attachToGrid = false,
        bool cropToParentSize = false)
    {
        EntityUid? container = null;
        var vectors = boxCoordinates.Select(e =>
        {
            var p1 = e.Item1;
            var p2 = e.Item2;

            container ??= p1.EntityId;
            if (container != p1.EntityId || container != p2.EntityId)
                throw new Exception($"An attempt to create a zone for coordinates from different parents. parent1: {p1.EntityId}, parent2: {p2.EntityId}; expected: {container}");

            var v1 = new Vector2(p1.X, p1.Y);
            var v2 = new Vector2(p2.X, p2.Y);
            return (v1, v2);
        });

        if (container == null)
            return null;

        return CreateZone(GetNetEntity(container.Value), vectors, protoId, name, color, attachToGrid, cropToParentSize);
    }

    /// <inheritdoc cref="CreateZone(ZoneParamsState)"/>
    public Entity<ZoneComponent>? CreateZone(
        IEnumerable<(MapCoordinates, MapCoordinates)> boxCoordinates,
        string? protoId = null,
        string? name = null,
        Color? color = null,
        bool attachToGrid = false,
        bool cropToParentSize = false)
    {
        EntityUid? container = null;
        var vectors = boxCoordinates.Select(e =>
        {
            var p1 = e.Item1;
            var p2 = e.Item2;

            var map1 = _map.GetMap(p1.MapId);
            var map2 = _map.GetMap(p2.MapId);

            container ??= map1;
            if (container != map1 || container != map2)
                throw new Exception($"An attempt to create a zone for coordinates from different maps. map1: {map1}, map2: {map2}; expected: {container}");

            var v1 = new Vector2(p1.X, p1.Y);
            var v2 = new Vector2(p2.X, p2.Y);
            return (v1, v2);
        });

        if (container == null)
            return null;

        return CreateZone(GetNetEntity(container.Value), vectors, protoId, name, color, attachToGrid, cropToParentSize);
    }

    /// <inheritdoc cref="CreateZone(ZoneParamsState)"/>
    public Entity<ZoneComponent>? CreateZone(
        NetEntity container,
        IEnumerable<(Vector2,Vector2)> points,
        string? protoId = null,
        string? name = null,
        Color? color = null,
        bool attachToGrid = false,
        bool cropToParentSize = false)
    {
        var boxes = points.Select(p => Box2.FromTwoPoints(p.Item1, p.Item2));
        return CreateZone(container, boxes, protoId, name, color, attachToGrid, cropToParentSize);
    }

    /// <inheritdoc cref="CreateZone(ZoneParamsState)"/>
    public Entity<ZoneComponent>? CreateZone(
        NetEntity container,
        IEnumerable<Box2> boxes,
        string? protoId = null,
        string? name = null,
        Color? color = null,
        bool attachToGrid = false,
        bool cropToParentSize = false)
    {
        return CreateZone(new ZoneParamsState()
        {
            Container = container,
            Boxes = boxes.ToList(),
            ProtoId = protoId ?? string.Empty,
            Name = name ?? string.Empty,
            Color = color ?? DefaultColor,
            AttachToGrid = attachToGrid,
            CutSpace = cropToParentSize
        });
    }

    /// Creates new zone
    public Entity<ZoneComponent>? CreateZone(ZoneParamsState @params)
    {
        if (@params.Boxes.Count <= 0 || !@params.Container.IsValid())
            return null;

        var container = GetEntity(@params.Container);
        if (!IsValidContainer(container))
            return null;

        if (string.IsNullOrEmpty(@params.Name))
            @params.Name = $"Zone {GetZonesCount() + 1}";

        if (string.IsNullOrEmpty(@params.ProtoId))
            @params.ProtoId = BaseZoneId;

        @params.RecalculateBoxes();

        var zone = Spawn(@params.ProtoId, Transform(container).Coordinates);
        _transform.AttachToGridOrMap(zone);

        var zoneComp = EnsureComp<ZoneComponent>(zone);
        zoneComp.ZoneParams.HandleState(@params);
        Dirty(zone, zoneComp);

        var zoneContainer = EnsureComp<ZonesContainerComponent>(container);
        zoneContainer.Zones.Add(GetNetEntity(zone));
        Dirty(container, zoneContainer);

        return (zone, zoneComp);
    }

    public void ChangeZone(Entity<ZoneComponent> zone, ZoneParamsState newParams)
    {
        if (!newParams.Container.IsValid())
            return;

        if (zone.Comp.ZoneParams?.Container != newParams.Container ||
            zone.Comp.ZoneParams?.ProtoId != newParams.ProtoId)
        {
            DeleteZone((zone, zone));
            CreateZone(newParams);
            return;
        }

        zone.Comp.ZoneParams.HandleState(newParams);
        Dirty(zone);
    }

    /// <inheritdoc cref="DeleteZone(Entity{ZonesContainerComponent?}, Entity{ZoneComponent?})"/>
    public void DeleteZone(Entity<ZoneComponent?> zone)
    {
        if (!Resolve(zone, ref zone.Comp))
            return;

        if (zone.Comp.ZoneParams?.Container is not { } container)
            return;

        DeleteZone(GetEntity(container), zone);
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
        Dirty(container);
        QueueDel(zone);
    }

    public void ClearZonesContainer(Entity<ZonesContainerComponent> container)
    {
        foreach (var zone in container.Comp.Zones)
            DeleteZone(GetEntity(zone));
    }

    public void DeleteZonesContaner(Entity<ZonesContainerComponent> container)
    {
        ClearZonesContainer(container);
        RemComp<ZonesContainerComponent>(container);
    }
}
