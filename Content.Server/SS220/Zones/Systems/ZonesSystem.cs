// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Administration.Managers;
using Content.Shared.Prototypes;
using Content.Shared.SS220.Maths;
using Content.Shared.SS220.Zones;
using Content.Shared.SS220.Zones.Components;
using Content.Shared.SS220.Zones.Systems;
using Robust.Server.GameObjects;
using Robust.Server.GameStates;
using Robust.Server.Player;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using System.Linq;
using System.Numerics;

namespace Content.Server.SS220.Zones.Systems;

public sealed partial class ZonesSystem : SharedZonesSystem
{
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly PvsOverrideSystem _pvsOverride = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IAdminManager _admin = default!;

    private static TimeSpan _overrideUpdateRate = TimeSpan.FromSeconds(5);
    private TimeSpan _nextOverrideUpdate = TimeSpan.Zero;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<CreateZoneRequestMessage>(OnCreateZoneRequest);
        SubscribeNetworkEvent<ChangeZoneRequestMessage>(OnChangeZoneRequest);
        SubscribeNetworkEvent<DeleteZoneRequestMessage>(OnDeleteZoneRequest);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _gameTiming.CurTime;
        if (curTime >= _nextOverrideUpdate)
        {
            UpdateZonesOverrides();
            _nextOverrideUpdate = curTime += _overrideUpdateRate;
        }

    }

    private void OnCreateZoneRequest(CreateZoneRequestMessage msg, EntitySessionEventArgs args)
    {
        if (!_admin.HasAdminFlag(args.SenderSession, Shared.Administration.AdminFlags.Mapping))
            return;

        CreateZone(GetEntity(msg.Parent), msg.ProtoId, msg.Area, msg.Name, msg.Color, msg.AttachToLattice);
    }

    private void OnChangeZoneRequest(ChangeZoneRequestMessage msg, EntitySessionEventArgs args)
    {
        if (!_admin.HasAdminFlag(args.SenderSession, Shared.Administration.AdminFlags.Mapping))
            return;

        var uid = GetEntity(msg.Zone);
        if (!TryComp<ZoneComponent>(uid, out var zoneComp))
            return;

        var zone = (uid, zoneComp);
        if (msg.Parent is { } parent)
            SetZoneParent(zone, GetEntity(parent), recalculate: false);

        if (msg.Area is { } area)
            SetZoneArea(zone, area, recalculate: false);

        if (msg.Name is { } name)
            SetZoneName(zone, name);

        if (msg.Color is { } color)
            SetZoneColor(zone, color);

        if (msg.AttachToLattice is { } attachToLattice)
            SetZoneAttachToLattice(zone, attachToLattice, recalculate: false);

        RecalculateZoneAreas(zone);
    }

    private void OnDeleteZoneRequest(DeleteZoneRequestMessage msg, EntitySessionEventArgs args)
    {
        if (!_admin.HasAdminFlag(args.SenderSession, Shared.Administration.AdminFlags.Mapping))
            return;

        var uid = GetEntity(msg.Zone);
        if (!TryComp<ZoneComponent>(uid, out var zoneComp))
            return;

        DeleteZone((uid, zoneComp));
    }

    /// <summary>
    /// Creates new zone
    /// </summary>
    public (Entity<ZoneComponent>? Zone, string? FailReason) CreateZone(
        EntityUid parent,
        EntProtoId<ZoneComponent> protoId,
        List<Box2> area,
        string? name = null,
        Color? color = null,
        bool attachToLattice = false)
    {
        if (area.Count <= 0)
            return (null, "Can't create a zone with an empty region");

        if (!IsValidParent(parent))
            return (null, "Can't create a zone with an invalid parent");

        if (!_prototype.TryIndex<EntityPrototype>(protoId, out var proto))
            return (null, "Can't create a zone with an invalid prototype id");

        if (!proto.HasComponent<ZoneComponent>())
            return (null, $"Can't create a zone with prototype that doesn't has a {nameof(ZoneComponent)}");

        if (string.IsNullOrEmpty(name))
            name = $"Zone {GetZonesCount() + 1}";

        var uid = Spawn(protoId, Transform(parent).Coordinates);
        var zoneComp = EnsureComp<ZoneComponent>(uid);
        var zone = (uid, zoneComp);

        SetZoneParent(zone, parent, recalculate: false);
        SetZoneArea(zone, area, recalculate: false);
        SetZoneName(zone, name);

        if (color != null)
            SetZoneColor(zone, color.Value);

        SetZoneAttachToLattice(zone, attachToLattice, recalculate: false);
        _metaData.SetEntityName(uid, name);

        RecalculateZoneAreas(zone);
        Dirty(uid, zoneComp);

        UpdateZoneOverrides(zone);

        return (zone, null);
    }

    public void DeleteZone(Entity<ZoneComponent> ent)
    {
        Del(ent);
    }

    public void RecalculateZoneAreas(Entity<ZoneComponent> ent)
    {
        var area = ent.Comp.Area.ToList().AsEnumerable();

        var parent = Transform(ent).ParentUid;

        if (ent.Comp.AttachToLattice)
        {
            float latticeSize;
            if (TryComp<MapGridComponent>(parent, out var mapGrid))
                latticeSize = mapGrid.TileSize;
            else
                latticeSize = 1f;

            area = MathHelperExtensions.AttachToLattice(area, latticeSize);
        }

        area = MathHelperExtensions.GetNonOverlappingBoxes(area);
        area = MathHelperExtensions.UnionInEqualSizedBoxes(area);
        ent.Comp.Area = [.. area];

        Dirty(ent);
    }

    public bool SetZoneParent(Entity<ZoneComponent> ent, EntityUid parent, bool recalculate = true)
    {
        if (!IsValidParent(parent))
            return false;

        _transform.SetParent(ent, parent);
        _transform.SetLocalPosition(ent, new Vector2(0, 0));

        if (recalculate)
            RecalculateZoneAreas(ent);

        Dirty(ent);
        return true;
    }

    public void SetZoneArea(Entity<ZoneComponent> ent, List<Box2> area, bool recalculate = true)
    {
        ent.Comp.Area = area;

        if (recalculate)
            RecalculateZoneAreas(ent);

        Dirty(ent);
    }

    public void SetZoneName(Entity<ZoneComponent> ent, string name)
    {
        _metaData.SetEntityName(ent, name);
        Dirty(ent);
    }

    public void SetZoneColor(Entity<ZoneComponent> ent, Color color)
    {
        ent.Comp.Color = color;
        Dirty(ent);
    }

    public void SetZoneAttachToLattice(Entity<ZoneComponent> ent, bool value, bool recalculate = true)
    {
        ent.Comp.AttachToLattice = value;

        if (recalculate)
            RecalculateZoneAreas(ent);

        Dirty(ent);
    }

    private void UpdateZonesOverrides()
    {
        var query = AllEntityQuery<ZoneComponent>();
        while (query.MoveNext(out var uid, out var zoneComp))
        {
            UpdateZoneOverrides((uid, zoneComp));
        }
    }

    private void UpdateZoneOverrides(Entity<ZoneComponent> ent)
    {
        var xform = Transform(ent);
        foreach (var session in _player.Sessions)
        {
            if (ShouldSessionOverride(session))
                _pvsOverride.AddSessionOverride(ent, session);
            else
                _pvsOverride.RemoveSessionOverride(ent, session);
        }

        bool ShouldSessionOverride(ICommonSession session)
        {
            if (session.AttachedEntity is not { } player)
                return false;

            if (Transform(player).MapID != xform.MapID)
                return false;

            return true;
        }
    }
}
