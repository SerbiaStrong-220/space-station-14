// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Prototypes;
using Content.Shared.SS220.Zones;
using Content.Shared.SS220.Zones.Components;
using Content.Shared.SS220.Zones.Systems;
using Robust.Server.GameObjects;
using Robust.Server.GameStates;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using System.Linq;
using System.Numerics;
using static Content.Shared.SS220.Zones.ZoneParams;

namespace Content.Server.SS220.Zones.Systems;

public sealed partial class ZonesSystem : SharedZonesSystem
{
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly PvsOverrideSystem _pvsOverride = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ZonesContainerComponent, ComponentShutdown>(OnZonesContainerShutdown);
        SubscribeLocalEvent<ZonesContainerComponent, ComponentGetState>(OnContainerGetState);

        SubscribeLocalEvent<ZoneComponent, ComponentShutdown>(OnZoneShutdown);
        SubscribeLocalEvent<ZoneComponent, ComponentGetState>(OnZoneGetState);

        SubscribeLocalEvent<TileChangedEvent>(OnTileChanged);
    }

    private void OnZonesContainerShutdown(Entity<ZonesContainerComponent> entity, ref ComponentShutdown args)
    {
        ClearZonesContainer(entity);
    }

    private void OnContainerGetState(Entity<ZonesContainerComponent> entity, ref ComponentGetState args)
    {
        args.State = new ZonesContainerComponentState(entity.Comp.Zones);
    }

    private void OnZoneShutdown(Entity<ZoneComponent> entity, ref ComponentShutdown args)
    {
        DeleteZone(entity.Owner);
    }

    private void OnZoneGetState(Entity<ZoneComponent> entity, ref ComponentGetState args)
    {
        args.State = new ZoneComponentState(entity.Comp.ZoneParams.GetState());
    }

    private void OnTileChanged(ref TileChangedEvent args)
    {
        if (!HasComp<TransformComponent>(args.Entity))
            return;

        foreach (var entry in args.Changes)
        {
            if (!entry.OldTile.IsEmpty && !entry.NewTile.IsEmpty)
                continue;

            var coords = _map.GridTileToLocal(args.Entity, args.Entity, entry.GridIndices);
            var zones = GetZonesByPoint(coords, RegionType.Original);
            foreach (var zone in zones)
                RecalculateZoneRegions(zone);
        }
    }

    /// <inheritdoc cref="CreateZone(ZoneParams)"/>
    public (Entity<ZoneComponent>? Zone, string? FailReason) CreateZone(
        IEnumerable<(EntityCoordinates, EntityCoordinates)> boxCoordinates,
        string? protoId = null,
        string? name = null,
        Color? color = null,
        bool attachToGrid = false,
        CutSpaceOptions cutSpaceOption = CutSpaceOptions.None)
    {
        try
        {
            EntityUid? container = null;
            var vectors = boxCoordinates.Select(e =>
            {
                var p1 = e.Item1;
                var p2 = e.Item2;

                container ??= p1.EntityId;
                if (container != p1.EntityId || container != p2.EntityId)
                    throw new Exception($"Can't create a a zone with coordinates from different parents. parent1: {p1.EntityId}, parent2: {p2.EntityId}; expected: {container}");

                var v1 = new Vector2(p1.X, p1.Y);
                var v2 = new Vector2(p2.X, p2.Y);
                return (v1, v2);
            });
            if (container == null)
                return (null, "Can't create a zone with an invalid container");

            return CreateZone(GetNetEntity(container.Value), vectors, protoId, name, color, attachToGrid, cutSpaceOption);
        }
        catch (Exception e)
        {
            return (null, e.Message);
        }
    }

    /// <inheritdoc cref="CreateZone(ZoneParams)"/>
    public (Entity<ZoneComponent>? Zone, string? FailReason) CreateZone(
        IEnumerable<(MapCoordinates, MapCoordinates)> boxCoordinates,
        string? protoId = null,
        string? name = null,
        Color? color = null,
        bool attachToGrid = false,
        CutSpaceOptions cutSpaceOption = CutSpaceOptions.None)
    {
        try
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
                    throw new Exception($"Can't create a zone with coordinates from different maps. map1: {map1}, map2: {map2}; expected: {container}");

                var v1 = new Vector2(p1.X, p1.Y);
                var v2 = new Vector2(p2.X, p2.Y);
                return (v1, v2);
            });

            if (container == null)
                return (null, "Can't create a zone with an invalid container");

            return CreateZone(GetNetEntity(container.Value), vectors, protoId, name, color, attachToGrid, cutSpaceOption);
        }
        catch (Exception e)
        {
            return (null, e.Message);
        }
    }

    /// <inheritdoc cref="CreateZone(ZoneParams)"/>
    public (Entity<ZoneComponent>? Zone, string? FailReason) CreateZone(
        NetEntity container,
        IEnumerable<(Vector2, Vector2)> points,
        string? protoId = null,
        string? name = null,
        Color? color = null,
        bool attachToGrid = false,
        CutSpaceOptions cutSpaceOption = CutSpaceOptions.None)
    {
        var region = points.Select(p => Box2.FromTwoPoints(p.Item1, p.Item2));
        return CreateZone(container, region, protoId, name, color, attachToGrid, cutSpaceOption);
    }

    /// <inheritdoc cref="CreateZone(ZoneParams)"/>
    public (Entity<ZoneComponent>? Zone, string? FailReason) CreateZone(
        NetEntity container,
        IEnumerable<Box2> originalRegion,
        string? protoId = null,
        string? name = null,
        Color? color = null,
        bool attachToGrid = false,
        CutSpaceOptions cutSpaceOption = CutSpaceOptions.None)
    {
        var @params = new ZoneParams()
        {
            Container =  GetEntity(container),
            ProtoID = protoId ?? string.Empty,
            Name = name ?? string.Empty,
            Color = color ?? DefaultColor,
            AttachToGrid = attachToGrid,
            CutSpaceOption = cutSpaceOption
        };
        @params.SetOriginalSize(originalRegion);
        return CreateZone(@params);
    }

    /// <summary>
    /// Creates new zone
    /// </summary>
    public (Entity<ZoneComponent>? Zone, string? FailReason) CreateZone(ZoneParams @params)
    {
        @params = @params.GetCopy();
        if (@params.OriginalRegion.Count <= 0)
            return (null, "Can't create a zone with an empty region");

        var container = @params.Container;
        if (!IsValidContainer(container))
            return (null, "Can't create a zone with an invalid container");

        if (!_prototype.TryIndex<EntityPrototype>(@params.ProtoID, out var proto))
            return (null, "Can't create a zone with an invalid prototype id");

        if (!proto.HasComponent<ZoneComponent>())
            return (null, $"Can't create a zone with prototype that doesn't has a {nameof(ZoneComponent)}");

        if (string.IsNullOrEmpty(@params.Name))
            @params.Name = $"Zone {GetZonesCount() + 1}";

        @params.RecalculateRegions();

        var zone = Spawn(@params.ProtoID, Transform(container).Coordinates);
        _pvsOverride.AddGlobalOverride(zone);
        _transform.AttachToGridOrMap(zone);

        var zoneComp = EnsureComp<ZoneComponent>(zone);
        zoneComp.ZoneParams = @params;
        _metaData.SetEntityName(zone, @params.Name);
        Dirty(zone, zoneComp);

        var zoneContainer = EnsureComp<ZonesContainerComponent>(container);
        zoneContainer.Zones.Add(GetNetEntity(zone));
        Dirty(container, zoneContainer);

        return ((zone, zoneComp), null);
    }

    public void ChangeZone(Entity<ZoneComponent> zone, ZoneParams newParams)
    {
        if (!IsValidContainer(newParams.Container))
            return;

        if (NeedRecreate(zone.Comp.ZoneParams, newParams))
        {
            DeleteZone(zone);
            CreateZone(newParams);
            return;
        }

        zone.Comp.ZoneParams = newParams;
        _metaData.SetEntityName(zone, newParams.Name);
        Dirty(zone);
    }

    /// <inheritdoc cref="DeleteZone(Entity{ZoneComponent})"/>
    public void DeleteZone(NetEntity zone)
    {
        DeleteZone(GetEntity(zone));
    }

    /// <inheritdoc cref="DeleteZone(Entity{ZoneComponent})"/>
    public void DeleteZone(EntityUid zone)
    {
        if (!TryComp<ZoneComponent>(zone, out var zoneComp))
            return;

        DeleteZone((zone, zoneComp));
    }

    /// <summary>
    /// Deletes the <paramref name="zone"/>
    /// </summary>
    public void DeleteZone(Entity<ZoneComponent> zone)
    {
        var container = zone.Comp.ZoneParams.Container;
        if (TryComp<ZonesContainerComponent>(container, out var containerComp))
        {
            containerComp.Zones.Remove(GetNetEntity(zone));
            Dirty(container, containerComp);
        }

        QueueDel(zone);
    }

    public void ClearZonesContainer(Entity<ZonesContainerComponent> container)
    {
        foreach (var zone in container.Comp.Zones)
            DeleteZone(GetEntity(zone));
    }

    public void DeleteZonesContaner(Entity<ZonesContainerComponent> container)
    {
        RemComp<ZonesContainerComponent>(container);
    }
}
