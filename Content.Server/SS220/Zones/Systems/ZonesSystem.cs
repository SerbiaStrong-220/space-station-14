// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Administration;
using Content.Server.Administration.Managers;
using Content.Shared.Prototypes;
using Content.Shared.SS220.Zones;
using Content.Shared.SS220.Zones.Components;
using Content.Shared.SS220.Zones.Systems;
using Robust.Server.GameObjects;
using Robust.Server.GameStates;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using System.Linq;
using System.Numerics;
using Content.Shared.CCVar;

namespace Content.Server.SS220.Zones.Systems;

public sealed partial class ZonesSystem : SharedZonesSystem
{
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly PvsOverrideSystem _pvsOverride = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IAdminManager _admin = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private float _pvsRange = float.PositiveInfinity;

    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(CCVars.NetMaxUpdateRange, value => _pvsRange = value, true);

        SubscribeNetworkEvent<CreateZoneRequestMessage>(OnCreateZoneRequest);
        SubscribeNetworkEvent<ChangeZoneRequestMessage>(OnChangeZoneRequest);
        SubscribeNetworkEvent<DeleteZoneRequestMessage>(OnDeleteZoneRequest);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = AllEntityQuery<ZoneComponent>();
        while (query.MoveNext(out var uid, out var zoneComp))
        {
            UpdatePvsOverride((uid, zoneComp));
        }
    }

    private void OnCreateZoneRequest(CreateZoneRequestMessage msg, EntitySessionEventArgs args)
    {
        if (!_admin.HasAdminFlag(args.SenderSession, Shared.Administration.AdminFlags.Mapping))
            return;

        CreateZone(GetEntity(msg.Parent), msg.ProtoId, msg.Area, msg.Name, msg.Color);
    }

    private void OnChangeZoneRequest(ChangeZoneRequestMessage msg, EntitySessionEventArgs args)
    {
        if (!_admin.HasAdminFlag(args.SenderSession, Shared.Administration.AdminFlags.Mapping))
            return;

        if (!TryGetEntity(msg.Zone, out var uid))
            return;

        if (!TryComp<ZoneComponent>(uid, out var zoneComp))
            return;

        Entity<ZoneComponent> zone = (uid.Value, zoneComp);
        if (msg.ProtoId is { } newProtoId)
            TryChangeZoneProto(ref zone, newProtoId);

        if (msg.Parent is { } parent)
            SetZoneParent(zone, GetEntity(parent), recalculate: false);

        if (msg.Area is { } area)
            SetZoneArea(zone, area, recalculate: false);

        if (msg.Name is { } name)
            SetZoneName(zone, name);

        if (msg.Color is { } color)
            SetZoneColor(zone, color);

        RecalculateZoneArea(zone);
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
    public Entity<ZoneComponent>? CreateZone(
        EntityUid parent,
        EntProtoId<ZoneComponent> protoId,
        List<Box2> area,
        string? name = null,
        Color? color = null)
    {
        if (area.Count <= 0)
            return null;

        if (!IsValidParent(parent))
            return null;

        if (!_prototype.TryIndex<EntityPrototype>(protoId, out var proto))
            return null;

        if (!proto.HasComponent<ZoneComponent>())
            return null;

        if (string.IsNullOrEmpty(name))
            name = $"Zone {GetZonesCount() + 1}";

        var uid = Spawn(protoId, Transform(parent).Coordinates);
        var zoneComp = EnsureComp<ZoneComponent>(uid);
        var zone = (uid, zoneComp);

        var xform = Transform(uid);
        xform.GridTraversal = false;

        SetZoneParent(zone, parent, recalculate: false);
        SetZoneArea(zone, area, recalculate: false);
        SetZoneName(zone, name);

        if (color != null)
            SetZoneColor(zone, color.Value);

        _metaData.SetEntityName(uid, name);

        RecalculateZoneArea(zone);
        Dirty(uid, zoneComp);

        UpdatePvsOverride(zone);

        return zone;
    }

    public void DeleteZone(Entity<ZoneComponent> ent)
    {
        Del(ent);
    }

    public void RecalculateZoneArea(Entity<ZoneComponent> ent)
    {
        var area = ent.Comp.Area.ToList();

        var parent = Transform(ent).ParentUid;
        ent.Comp.Area = RecalculateArea(area, parent);

        Dirty(ent);
    }

    public bool TryChangeZoneProto(ref Entity<ZoneComponent> ent, EntProtoId<ZoneComponent> newProtoId)
    {
        var meta = MetaData(ent);
        if (newProtoId.Id == meta.EntityPrototype?.ID)
            return false;

        var newZone = CreateZone(
                Transform(ent).ParentUid,
                newProtoId,
                ent.Comp.Area,
                meta.EntityName,
                ent.Comp.Color);

        if (newZone == null)
            return false;

        DeleteZone(ent);
        ent = newZone.Value;
        return true;
    }

    public bool SetZoneParent(Entity<ZoneComponent> ent, EntityUid parent, bool recalculate = true)
    {
        if (!IsValidParent(parent))
            return false;

        _transform.SetCoordinates((ent, Transform(ent), MetaData(ent)), new EntityCoordinates(parent, Vector2.Zero), Angle.Zero);

        if (recalculate)
            RecalculateZoneArea(ent);

        Dirty(ent);
        return true;
    }

    public void SetZoneArea(Entity<ZoneComponent> ent, List<Box2> area, bool recalculate = true)
    {
        ent.Comp.Area = area;

        if (recalculate)
            RecalculateZoneArea(ent);

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

    private void UpdatePvsOverride(Entity<ZoneComponent> ent)
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
            if (_admin.HasAdminFlag(session, AdminFlags.Mapping))
                return true;

            if (session.AttachedEntity is not { } player)
                return false;

            var playerMapCoords = _transform.GetMapCoordinates(player);
            if (playerMapCoords.MapId != xform.MapID)
                return false;

            var pvsRangSqrt = Math.Sqrt(_pvsRange);
            var localPos = _transform.ToCoordinates(ent.Owner, playerMapCoords).Position;
            foreach (var box in ent.Comp.Area)
            {
                if (box.ClosestPoint(localPos).LengthSquared() <= pvsRangSqrt)
                    return true;
            }

            return false;
        }
    }
}
