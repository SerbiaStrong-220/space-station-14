// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Administration;
using Content.Server.Administration.Managers;
using Content.Shared.Prototypes;
using Content.Shared.SS220.Zone.Components;
using Robust.Server.GameObjects;
using Robust.Server.GameStates;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using System.Linq;
using System.Numerics;
using Content.Shared.SS220.Zone.Systems;
using Content.Shared.SS220.Zone;

namespace Content.Server.SS220.Zone.Systems;

public sealed partial class ZoneSystem : SharedZoneSystem
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

        _cfg.OnValueChanged(Robust.Shared.CVars.NetMaxUpdateRange, value => _pvsRange = value, true);

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

    protected override void OnZoneMapInit(Entity<ZoneComponent> entity, ref MapInitEvent args)
    {
        foreach (var reg in entity.Comp.RelationUpdateComponents.Values)
        {
            var compType = reg.Component.GetType();
            RegisterRelationUpdate(compType, entity.Comp);
        }

        base.OnZoneMapInit(entity, ref args);
    }

    protected override void OnZoneShutdown(Entity<ZoneComponent> entity, ref ComponentShutdown args)
    {
        foreach (var reg in entity.Comp.RelationUpdateComponents.Values)
        {
            var compType = reg.Component.GetType();
            UnregisterRelationUpdate(compType, entity.Comp);
        }

        base.OnZoneShutdown(entity, ref args);
    }

    private void OnCreateZoneRequest(CreateZoneRequestMessage msg, EntitySessionEventArgs args)
    {
        if (!_admin.HasAdminFlag(args.SenderSession, AdminFlags.Mapping))
            return;

        CreateZone(GetEntity(msg.Parent), msg.ProtoId, msg.Area, msg.Name, msg.Color);
    }

    private void OnChangeZoneRequest(ChangeZoneRequestMessage msg, EntitySessionEventArgs args)
    {
        if (!_admin.HasAdminFlag(args.SenderSession, AdminFlags.Mapping))
            return;

        if (!TryGetEntity(msg.Zone, out var uid))
            return;

        if (!TryComp<ZoneComponent>(uid, out var zoneComp))
            return;

        Entity<ZoneComponent> zone = (uid.Value, zoneComp);
        if (msg.ProtoId is { } newProtoId)
            TryChangeZoneProto(ref zone, newProtoId);

        if (msg.Parent is { } parent)
            SetZoneParent(zone, GetEntity(parent), updateCache: false);

        if (msg.Area is { } area)
            SetZoneArea(zone, area, optimize: false, updateCache: false);

        if (msg.Name is { } name)
            SetZoneName(zone, name);

        if (msg.Color is { } color)
            SetZoneColor(zone, color);

        OptimizeZoneArea(zone, updateCache: true);
    }

    private void OnDeleteZoneRequest(DeleteZoneRequestMessage msg, EntitySessionEventArgs args)
    {
        if (!_admin.HasAdminFlag(args.SenderSession, AdminFlags.Mapping))
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

        SetZoneParent(zone, parent, updateCache: false);
        SetZoneArea(zone, area, optimize: false, updateCache: false);
        SetZoneName(zone, name);

        if (color != null)
            SetZoneColor(zone, color.Value);

        _metaData.SetEntityName(uid, name);

        OptimizeZoneArea(zone, updateCache: true);
        Dirty(uid, zoneComp);

        UpdatePvsOverride(zone);

        return zone;
    }

    public void DeleteZone(Entity<ZoneComponent> ent)
    {
        Del(ent);
    }

    /// <summary>
    /// Optimizes the area boxes of the zone, removing their intersections and, if possible, merging them
    /// </summary>
    public void OptimizeZoneArea(Entity<ZoneComponent> ent, bool updateCache = true)
    {
        var area = ent.Comp.Area.ToList();
        ent.Comp.Area = OptimizeArea(area);

        if (updateCache)
            UpdateZoneCache(ent);

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

    public bool SetZoneParent(Entity<ZoneComponent> ent, EntityUid parent, bool updateCache = true)
    {
        if (!IsValidParent(parent))
            return false;

        _transform.SetCoordinates((ent, Transform(ent), MetaData(ent)), new EntityCoordinates(parent, Vector2.Zero), Angle.Zero);

        if (updateCache)
            UpdateZoneCache(ent);

        Dirty(ent);
        return true;
    }

    public void SetZoneArea(Entity<ZoneComponent> ent, List<Box2> area, bool optimize = true, bool updateCache = true)
    {
        ent.Comp.Area = area;

        if (optimize)
            OptimizeZoneArea(ent, updateCache: false);

        if (updateCache)
            UpdateZoneCache(ent);

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

    public override bool RegisterRelationUpdate(EntityUid uid, object registrator)
    {
        return RelationUpdateData.RegisterEntity(uid, registrator);
    }

    public override bool RegisterRelationUpdate(Type componentType, object registrator)
    {
        return RelationUpdateData.RegisterComponent(componentType, registrator);
    }

    public override bool UnregisterRelationUpdate(EntityUid uid, object registrator)
    {
        return RelationUpdateData.UnregisterEntity(uid, registrator);
    }

    public override bool UnregisterRelationUpdate(Type componentType, object registrator)
    {
        return RelationUpdateData.UnregisterComponent(componentType, registrator);
    }

    public override bool UnregisterRelationUpdateForced(EntityUid uid)
    {
        return RelationUpdateData.UnregisterEntityForced(uid);
    }

    public override bool UnregisterRelationUpdateForced(Type componentType)
    {
        return RelationUpdateData.UnregisterComponentForced(componentType);
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
            //if (_admin.HasAdminFlag(session, AdminFlags.Mapping))
            //    return true;

            if (session.AttachedEntity is not { } player)
                return false;

            var playerMapCoords = _transform.GetMapCoordinates(player);
            if (playerMapCoords.MapId != xform.MapID)
                return false;

            var pvsRangeSquared = Math.Pow(_pvsRange, 2);
            var localPos = _transform.ToCoordinates(ent.Owner, playerMapCoords).Position;
            foreach (var box in ent.Comp.Area)
            {
                var distanceSquared = (box.ClosestPoint(localPos) - localPos).LengthSquared();
                if (distanceSquared <= pvsRangeSquared)
                    return true;
            }

            return false;
        }
    }
}
