// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Maths;
using Content.Shared.SS220.Zones.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using static Content.Shared.SS220.Zones.Systems.ZoneParams;

namespace Content.Shared.SS220.Zones.Systems;

public abstract partial class SharedZonesSystem : EntitySystem
{
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;

    public const string ZoneCommandsPrefix = "zones:";

    public static readonly EntProtoId<ZoneComponent> BaseZoneId = "BaseZone";
    public static readonly Color DefaultColor = Color.Gray;

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

        var entitiesToLeave = zone.Comp.Entities.ToHashSet();
        var entitiesToEnter = new HashSet<EntityUid>();
        var curEntities = GetInZoneEntities(zone, RegionTypes.Active);
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

    /// <summary>
    /// Returns entities located in the <paramref name="zone"/>.
    /// The check is performed at the <see cref="TransformComponent.Coordinates"/> of the entities.
    /// </summary>
    public IEnumerable<EntityUid> GetInZoneEntities(Entity<ZoneComponent> zone, RegionTypes regionType = RegionTypes.Original)
    {
        HashSet<EntityUid> entities = [];
        var container = GetEntity(zone.Comp.ZoneParams.Container);
        if (!container.IsValid())
            return entities;

        var mapId = Transform(container).MapID;
        foreach (var bounds in GetWorldRegion(zone, regionType))
        {
            foreach (var uid in _entityLookup.GetEntitiesIntersecting(mapId, bounds, LookupFlags.Uncontained))
            {
                if (InZone(zone, uid, regionType))
                    entities.Add(uid);
            }
        }

        return entities;
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

    /// <inheritdoc cref="InZone(Entity{ZoneComponent}, MapCoordinates, RegionTypes)"/>
    public bool InZone(Entity<ZoneComponent> zone, EntityUid entity, RegionTypes regionType = RegionTypes.Active)
    {
        return InZone(zone, _transform.GetMapCoordinates(entity), regionType);
    }

    /// <inheritdoc cref="InZone(Entity{ZoneComponent}, MapCoordinates, RegionTypes)"/>
    public bool InZone(Entity<ZoneComponent> zone, EntityCoordinates point, RegionTypes regionType = RegionTypes.Active)
    {
        if (GetEntity(zone.Comp.ZoneParams.Container) != point.EntityId)
            return false;

        return InZone(zone, _transform.ToMapCoordinates(point), regionType);
    }

    /// <summary>
    /// Determines whether the <paramref name="point"/> is located inside the <paramref name="zone"/>.
    /// </summary>
    public bool InZone(Entity<ZoneComponent> zone, MapCoordinates point, RegionTypes regionType = RegionTypes.Active)
    {
        var container = GetEntity(zone.Comp.ZoneParams.Container);
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

    /// <inheritdoc cref="GetZonesByPoint(MapCoordinates, RegionTypes)"/>
    public IEnumerable<Entity<ZoneComponent>> GetZonesByPoint(MapId mapId, Vector2 point, RegionTypes regionType = RegionTypes.Active)
    {
        return GetZonesByPoint(new MapCoordinates(point, mapId), regionType);
    }

    /// <inheritdoc cref="GetZonesByPoint(MapCoordinates, RegionTypes)"/>
    public IEnumerable<Entity<ZoneComponent>> GetZonesByPoint(EntityCoordinates point, RegionTypes regionType = RegionTypes.Active)
    {
        return GetZonesByPoint(_transform.ToMapCoordinates(point), regionType);
    }

    /// <summary>s
    /// Returns zones containing a <paramref name="point"/>
    /// </summary>
    public IEnumerable<Entity<ZoneComponent>> GetZonesByPoint(MapCoordinates point, RegionTypes regionType = RegionTypes.Active)
    {
        HashSet<Entity<ZoneComponent>> result = new();

        var query = EntityQueryEnumerator<ZonesContainerComponent>();
        while (query.MoveNext(out var uid, out var container))
        {
            if (Transform(uid).MapID != point.MapId)
                continue;

            foreach (var zone in GetZonesInContainer((uid, container)))
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

    public static void RecalculateZoneRegions(ref IEnumerable<Box2> boxes)
    {
        MathHelperExtensions.GetNonOverlappingBoxes(ref boxes);
        MathHelperExtensions.UnionInEqualSizedBoxes(ref boxes);
    }

    public static IEnumerable<Box2> RecalculateZoneRegions(IEnumerable<Box2> boxes)
    {
        RecalculateZoneRegions(ref boxes);
        return boxes;
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
        var query = EntityQueryEnumerator<ZoneComponent>();
        while (query.MoveNext(out _, out _))
            result++;

        return result;
    }

    public static bool TryParseTag(string input, string tag, [NotNullWhen(true)] out string? value)
    {
        value = null;

        if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(tag))
            return false;

        var pattern = @$"{Regex.Escape(tag)}=(?:""([^""]*)""|(\S+))";
        var regex = new Regex(pattern, RegexOptions.Compiled);

        var match = regex.Match(input);
        if (!match.Success)
            return false;

        for (var i = 1; i < match.Groups.Count; i++)
        {
            var group = match.Groups[i];
            if (group.Success)
                value = group.Value;
        }

        return value != null;
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

    public bool IsValidContainer(NetEntity netEntity)
    {
        return IsValidContainer(GetEntity(netEntity));
    }

    public bool IsValidContainer(EntityUid uid)
    {
        return uid.IsValid() && (HasComp<MapComponent>(uid) || HasComp<MapGridComponent>(uid));
    }

    public List<Box2Rotated> GetWorldRegion(Entity<ZoneComponent> zone, RegionTypes regionType)
    {
        var world = new List<Box2Rotated>();
        var local = zone.Comp.ZoneParams.GetRegion(regionType);
        if (local.Count <= 0)
            return world;

        var container = GetEntity(zone.Comp.ZoneParams.Container);
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

    public HashSet<Entity<ZoneComponent>> GetZonesInContainer(Entity<ZonesContainerComponent> entity)
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
[DataDefinition]
public sealed partial class ZoneParams()
{
    /// <summary>
    /// The entity that this zone is assigned to.
    /// Used to determine local coordinates
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public NetEntity Container
    {
        get => _container;
        set => TryChangeContainer(value);
    }

    private NetEntity _container = NetEntity.Invalid;

    /// <summary>
    /// Name of the zone
    /// </summary>
    [ViewVariables]
    public string Name = string.Empty;

    /// <summary>
    /// ID of the zone's entity prototype
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public EntProtoId<ZoneComponent> ProtoID = SharedZonesSystem.BaseZoneId;

    /// <summary>
    /// Current color of the zone
    /// </summary>
    [ViewVariables]
    public Color Color = SharedZonesSystem.DefaultColor;

    /// <summary>
    /// Should the size of the zone be attached to the grid
    /// </summary>
    [ViewVariables]
    public bool AttachToGrid
    {
        get => _attachToGrid;
        set
        {
            _attachToGrid = value;
            RecalculateRegions();
        }
    }
    private bool _attachToGrid = false;

    /// <summary>
    /// Space cutting option.
    /// It only works if the <see cref="Container"/> is a grid
    /// </summary>
    [ViewVariables]
    public CutSpaceOptions CutSpaceOption
    {
        get => _cutSpaceOption;
        set
        {
            _cutSpaceOption = value;
            RecalculateRegions();
        }
    }
    private CutSpaceOptions _cutSpaceOption = CutSpaceOptions.None;

    /// <summary>
    /// Original size of the zone
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    [Access(Other = AccessPermissions.Read)]
    public List<Box2> OriginalRegion = new();

    /// <summary>
    /// Disabled zone size
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    [Access(Other = AccessPermissions.Read)]
    public List<Box2> DisabledRegion = new();

    /// <summary>
    /// The <see cref="OriginalRegion"/> with the cut-out <see cref="DisabledRegion"/>
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    [Access(Other = AccessPermissions.Read)]
    public List<Box2> ActiveRegion = new();

    public ZoneParams(ZoneParams @params) : this()
    {
        CopyFrom(@params);
    }

    public void ParseTags(string input)
    {
        // Cursed because GetFields() doesn't accesible on the client side

        if (SharedZonesSystem.TryParseTag(input, nameof(Container).ToLower(), out var value) &&
            NetEntity.TryParse(value, out var container))
            Container = container;

        if (SharedZonesSystem.TryParseTag(input, nameof(OriginalRegion).ToLower(), out value))
        {
            var boxesStrings = value.Split(";");
            var list = new List<Box2>();
            foreach (var str in boxesStrings)
            {
                if (MathHelperExtensions.TryParseBox2(str, out var box))
                    list.Add(box.Value);
            }
            OriginalRegion = list;
        }

        if (SharedZonesSystem.TryParseTag(input, nameof(Name).ToLower(), out value))
            Name = value;

        if (SharedZonesSystem.TryParseTag(input, nameof(ProtoID).ToLower(), out value))
            ProtoID = value;

        if (SharedZonesSystem.TryParseTag(input, nameof(Color).ToLower(), out value) &&
            Color.TryParse(value, out var color))
            Color = color;

        if (SharedZonesSystem.TryParseTag(input, nameof(AttachToGrid).ToLower(), out value) &&
            bool.TryParse(value, out var attach))
            AttachToGrid = attach;

        if (SharedZonesSystem.TryParseTag(input, nameof(CutSpaceOption).ToLower(), out value) &&
            Enum.TryParse<CutSpaceOptions>(value, out var cutSpaceOption))
            CutSpaceOption = cutSpaceOption;
    }

    public static bool operator ==(ZoneParams left, ZoneParams right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ZoneParams left, ZoneParams right)
    {
        return !left.Equals(right);
    }

    public override int GetHashCode()
    {
        var sorted = GetSortedBoxes(OriginalRegion);
        return HashCode.Combine(Container, Name, ProtoID, Color, AttachToGrid, sorted);
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        if (obj is not ZoneParams @params)
            return false;

        return Equals(@params);
    }

    public bool Equals(ZoneParams other)
    {
        var isFieldsEquals = Container == other.Container &&
            Name == other.Name &&
            ProtoID == other.ProtoID &&
            Color == other.Color &&
            AttachToGrid == other.AttachToGrid;

        if (!isFieldsEquals)
            return false;

        return IsRegionEquals(this, other, RegionTypes.Original);
    }

    public bool TryChangeContainer(NetEntity newContainer)
    {
        var zonesSys = IoCManager.Resolve<IEntityManager>().System<SharedZonesSystem>();
        if (!zonesSys.IsValidContainer(newContainer))
            return false;

        _container = newContainer;
        return true;
    }

    public string[] GetTags()
    {
        // Cursed because GetFields() doesn't accesible on the client side

        var original = OriginalRegion.Select(b => b.ToString());
        var originalStr = string.Join("; ", original);

        return [
            $"{nameof(Container).ToLower()}={Container}",
            $"{nameof(OriginalRegion).ToLower()}=\"{originalStr}\"",
            $"{nameof(Name).ToLower()}=\"{Name}\"",
            $"{nameof(ProtoID).ToLower()}=\"{ProtoID}\"",
            $"{nameof(Color).ToLower()}={Color.ToHex()}",
            $"{nameof(AttachToGrid).ToLower()}={AttachToGrid}",
            $"{nameof(CutSpaceOption).ToLower()}={CutSpaceOption}"
            ];
    }

    public void RecalculateRegions()
    {
        var original = OriginalRegion.AsEnumerable();
        var zoneSys = IoCManager.Resolve<IEntityManager>().System<SharedZonesSystem>();

        if (AttachToGrid)
            zoneSys.AttachToGrid(Container, ref original);

        IEnumerable<Box2> disabled = [];
        switch (CutSpaceOption)
        {
            case CutSpaceOptions.Dinamic:
                disabled = zoneSys.GetSpaceBoxes(Container, original).ToList();
                break;

            case CutSpaceOptions.Forever:
                zoneSys.CutSpace(Container, ref original, out _);
                break;
        }

        SharedZonesSystem.RecalculateZoneRegions(ref disabled);
        DisabledRegion = [.. disabled];

        SharedZonesSystem.RecalculateZoneRegions(ref original);
        OriginalRegion = [.. original];

        var active = MathHelperExtensions.SubstructBox(original, disabled);
        SharedZonesSystem.RecalculateZoneRegions(ref active);
        ActiveRegion = [.. active];
    }

    public void SetOriginalSize(IEnumerable<Box2> newSize)
    {
        OriginalRegion = [.. newSize];
        RecalculateRegions();
    }

    public ZoneParams GetCopy()
    {
        return new ZoneParams(this);
    }

    public void CopyFrom(ZoneParams @params)
    {
        _container = @params.Container;
        Name = @params.Name;
        ProtoID = @params.ProtoID;
        Color = @params.Color;
        _attachToGrid = @params.AttachToGrid;
        _cutSpaceOption = @params.CutSpaceOption;
        OriginalRegion = @params.OriginalRegion;
        ActiveRegion = @params.ActiveRegion;
        DisabledRegion = @params.DisabledRegion;
    }

    public List<Box2> GetRegion(RegionTypes type)
    {
        return type switch
        {
            RegionTypes.Original => OriginalRegion,
            RegionTypes.Active => ActiveRegion,
            RegionTypes.Disabled => DisabledRegion,
            _ => ActiveRegion
        };
    }

    public enum CutSpaceOptions
    {
        None,
        Dinamic,
        Forever
    }

    public enum RegionTypes
    {
        Original,
        Active,
        Disabled
    }

    public static bool IsRegionEquals(ZoneParams left, ZoneParams right, RegionTypes region = RegionTypes.Original)
    {
        return IsRegionEquals(left, right, region, region);
    }

    public static bool IsRegionEquals(ZoneParams left, ZoneParams right, RegionTypes leftRegion, RegionTypes rightRegion)
    {
        var ourBoxes = GetSortedBoxes(left.GetRegion(leftRegion));
        var otherBoxes = GetSortedBoxes(right.GetRegion(rightRegion));
        return ourBoxes.SequenceEqual(otherBoxes);
    }

    public static IEnumerable<Box2> GetSortedBoxes(in IEnumerable<Box2> boxes)
    {
        var sorted = boxes.OrderBy(b => Box2.Area(b))
            .ThenBy(b => b.BottomLeft)
            .ThenBy(b => b.TopRight);

        return sorted;
    }
}
