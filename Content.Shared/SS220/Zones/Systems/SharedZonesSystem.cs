// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Maps;
using Content.Shared.SS220.Maths;
using Content.Shared.SS220.Zones.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using YamlDotNet.Core.Tokens;

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
        var container = GetEntity(zone.Comp.ZoneParams.Container);
        if (!TryComp<BroadphaseComponent>(container, out var broadphase))
            return new HashSet<EntityUid>();

        return GetEntitiesInZone((container, broadphase), zone);
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

        var lookup = container.Comp;
        var state = (entities, zone);

        foreach (var box in zone.Comp.ZoneParams.CurrentSize)
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
        if (GetEntity(zone.Comp.ZoneParams.Container) != _map.GetMap(point.MapId))
            return false;

        return InZone(zone, point.Position);
    }

    /// <inheritdoc cref="InZone(Entity{ZoneComponent}, Vector2)"/>
    public bool InZone(Entity<ZoneComponent> zone, EntityCoordinates point)
    {
        if (GetEntity(zone.Comp.ZoneParams.Container) != point.EntityId)
            return false;

        return InZone(zone, point.Position);
    }

    /// <summary>
    /// Determines whether the <paramref name="point"/> is located inside the <paramref name="zone"/>.
    /// </summary>
    public static bool InZone(Entity<ZoneComponent> zone, Vector2 point)
    {
        foreach (var box in zone.Comp.ZoneParams.CurrentSize)
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
        zone.Comp.ZoneParams.RecalculateSize();
        Dirty(zone, zone.Comp);
    }

    public static void RecalculateZoneBoxes(ref IEnumerable<Box2> boxes)
    {
        MathHelperExtensions.GetNonOverlappingBoxes(ref boxes);
        MathHelperExtensions.UnionInEqualSizedBoxes(ref boxes);
    }

    public static IEnumerable<Box2> RecalculateZoneBoxes(IEnumerable<Box2> boxes)
    {
        RecalculateZoneBoxes(ref boxes);
        return boxes;
    }

    public Box2 AttachToGrid(NetEntity container, Box2 box)
    {
        return AttachToGrid(GetEntity(container), box);
    }

    public Box2 AttachToGrid(EntityUid container, Box2 box)
    {
        if (TryComp<MapGridComponent>(container, out var mapGrid))
            return MathHelperExtensions.AttachToGrid(box, mapGrid.TileSize);

        return MathHelperExtensions.AttachToGrid(box);
    }

    public void AttachToGrid(NetEntity container, ref Box2 box)
    {
        AttachToGrid(GetEntity(container), ref box);
    }

    public void AttachToGrid(EntityUid container, ref Box2 box)
    {
        box = AttachToGrid(container, box);
    }

    public IEnumerable<Box2> AttachToGrid(NetEntity container, IEnumerable<Box2> boxes)
    {
        return AttachToGrid(GetEntity(container), boxes);
    }

    public IEnumerable<Box2> AttachToGrid(EntityUid container, IEnumerable<Box2> boxes)
    {
        if (TryComp<MapGridComponent>(container, out var mapGrid))
            return MathHelperExtensions.AttachToGrid(boxes, mapGrid.TileSize);

        return MathHelperExtensions.AttachToGrid(boxes);
    }

    public void AttachToGrid(NetEntity container, ref IEnumerable<Box2> boxes)
    {
        AttachToGrid(GetEntity(container), ref boxes);
    }

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

    public IEnumerable<Box2> CutSpace(NetEntity parent, IEnumerable<Box2> boxes, out IEnumerable<Box2> spaceBoxes)
    {
        return CutSpace(GetEntity(parent), boxes, out spaceBoxes);
    }

    public IEnumerable<Box2> CutSpace(EntityUid parent, IEnumerable<Box2> boxes, out IEnumerable<Box2> spaceBoxes)
    {
        spaceBoxes = [];
        if (!TryComp<MapGridComponent>(parent, out var mapGrid))
            return [];

        return CutSpace((parent, mapGrid), boxes, out spaceBoxes);
    }

    public IEnumerable<Box2> CutSpace(Entity<MapGridComponent> grid, IEnumerable<Box2> boxes, out IEnumerable<Box2> spaceBoxes)
    {
        spaceBoxes = GetSpaceBoxes(grid, boxes);
        return MathHelperExtensions.SubstructBox(boxes, spaceBoxes);
    }

    public void CutSpace(NetEntity parent, ref IEnumerable<Box2> boxes, out IEnumerable<Box2> spaceBoxes)
    {
        CutSpace(GetEntity(parent), ref boxes, out spaceBoxes);
    }

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

    public void CutSpace(Entity<MapGridComponent> grid, ref IEnumerable<Box2> boxes, out IEnumerable<Box2> spaceBoxes)
    {
        boxes = CutSpace(grid, boxes, out spaceBoxes);
    }

    public IEnumerable<Box2> GetSpaceBoxes(NetEntity parent, IEnumerable<Box2> boxes)
    {
        return GetSpaceBoxes(GetEntity(parent), boxes);
    }

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

    public static IEnumerable<Box2> GetSortedBoxes(in IEnumerable<Box2> boxes)
    {
        var sorted = boxes.OrderBy(b => Box2.Area(b))
            .ThenBy(b => b.BottomLeft)
            .ThenBy(b => b.TopRight);

        return sorted;
    }

    public static bool IsBoxesEquals(ZoneParams state1, ZoneParams state2)
    {
        var ourBoxes = GetSortedBoxes(state1.CurrentSize);
        var otherBoxes = GetSortedBoxes(state2.CurrentSize);
        return ourBoxes.SequenceEqual(otherBoxes);
    }

    public bool IsValidContainer(NetEntity netEntity)
    {
        return IsValidContainer(GetEntity(netEntity));
    }

    public bool IsValidContainer(EntityUid uid)
    {
        return HasComp<MapComponent>(uid) || HasComp<MapGridComponent>(uid);
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

    [ViewVariables]
    public string Name = string.Empty;

    [ViewVariables(VVAccess.ReadOnly)]
    public string ProtoId = SharedZonesSystem.BaseZoneId;

    /// <summary>
    /// Current color of the zone
    /// </summary>
    [ViewVariables]
    public Color Color = SharedZonesSystem.DefaultColor;

    [ViewVariables]
    public bool AttachToGrid
    {
        get => _attachToGrid;
        set
        {
            _attachToGrid = value;
            RecalculateSize();
        }
    }
    private bool _attachToGrid = false;

    [ViewVariables]
    public CutSpaceOptions CutSpaceOption
    {
        get => _cutSpaceOption;
        set
        {
            _cutSpaceOption = value;
            RecalculateSize();
        }
    }
    private CutSpaceOptions _cutSpaceOption = CutSpaceOptions.None;

    [ViewVariables(VVAccess.ReadOnly)]
    [Access(Other = AccessPermissions.Read)]
    public List<Box2> OriginalSize = new();

    [ViewVariables(VVAccess.ReadOnly)]
    [Access(Other = AccessPermissions.Read)]
    public List<Box2> CurrentSize = new();

    [ViewVariables(VVAccess.ReadOnly)]
    [Access(Other = AccessPermissions.Read)]
    public List<Box2> CutOutSize = new();

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

        if (SharedZonesSystem.TryParseTag(input, nameof(OriginalSize).ToLower(), out value))
        {
            var boxesStrings = value.Split(";");
            var list = new List<Box2>();
            foreach (var str in boxesStrings)
            {
                if (MathHelperExtensions.TryParseBox2(str, out var box))
                    list.Add(box.Value);
            }
            OriginalSize = list;
        }

        if (SharedZonesSystem.TryParseTag(input, nameof(Name).ToLower(), out value))
            Name = value;

        if (SharedZonesSystem.TryParseTag(input, nameof(ProtoId).ToLower(), out value))
            ProtoId = value;

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
        var sorted = SharedZonesSystem.GetSortedBoxes(CurrentSize);
        return HashCode.Combine(Container, Name, ProtoId, Color, AttachToGrid, sorted);
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        if (obj is not ZoneParams state)
            return false;

        return Equals(state);
    }

    public bool Equals(ZoneParams other)
    {
        var isFieldsEquals = Container == other.Container &&
            Name == other.Name &&
            ProtoId == other.ProtoId &&
            Color == other.Color &&
            AttachToGrid == other.AttachToGrid;

        return isFieldsEquals && SharedZonesSystem.IsBoxesEquals(this, other);
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

        var original = OriginalSize.Select(b => b.ToString());
        var originalStr = string.Join("; ", original);

        return [
            $"{nameof(Container).ToLower()}={Container}",
            $"{nameof(OriginalSize).ToLower()}=\"{originalStr}\"",
            $"{nameof(Name).ToLower()}=\"{Name}\"",
            $"{nameof(ProtoId).ToLower()}=\"{ProtoId}\"",
            $"{nameof(Color).ToLower()}={Color.ToHex()}",
            $"{nameof(AttachToGrid).ToLower()}={AttachToGrid}",
            $"{nameof(CutSpaceOption).ToLower()}={CutSpaceOption}"
            ];
    }

    public void RecalculateSize()
    {
        var original = OriginalSize.AsEnumerable();
        var zoneSys = IoCManager.Resolve<IEntityManager>().System<SharedZonesSystem>();

        if (AttachToGrid)
            zoneSys.AttachToGrid(Container, ref original);

        IEnumerable<Box2> cutOut = [];
        switch (CutSpaceOption)
        {
            case CutSpaceOptions.Dinamic:
                cutOut = zoneSys.GetSpaceBoxes(Container, original).ToList();
                break;

            case CutSpaceOptions.Forever:
                zoneSys.CutSpace(Container, ref original, out _);
                break;
        }

        SharedZonesSystem.RecalculateZoneBoxes(ref cutOut);
        CutOutSize = cutOut.ToList();

        SharedZonesSystem.RecalculateZoneBoxes(ref original);
        OriginalSize = original.ToList();

        var current = MathHelperExtensions.SubstructBox(original, cutOut);
        SharedZonesSystem.RecalculateZoneBoxes(ref current);
        CurrentSize = current.ToList();
    }

    public void SetOriginalSize(IEnumerable<Box2> newSize)
    {
        OriginalSize = newSize.ToList();
        RecalculateSize();
    }

    public ZoneParams GetCopy()
    {
        return new ZoneParams(this);
    }

    public void CopyFrom(ZoneParams @params)
    {
        _container = @params.Container;
        Name = @params.Name;
        ProtoId = @params.ProtoId;
        Color = @params.Color;
        _attachToGrid = @params.AttachToGrid;
        _cutSpaceOption = @params.CutSpaceOption;
        OriginalSize = @params.OriginalSize;
        CurrentSize = @params.CurrentSize;
        CutOutSize = @params.CutOutSize;
    }

    public enum CutSpaceOptions
    {
        None,
        Dinamic,
        Forever
    }
}
