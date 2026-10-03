// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using System.Numerics;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Pinpointer;
using Content.Shared.SS220.CultYogg.Cultists;
using Content.Shared.SS220.CultYogg.CultMiniMap;
using Content.Shared.SS220.CultYogg.MiGo;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.SS220.CultYogg.CultMiniMap;

/// <summary>
/// Grants the map to cult members and supplies positions independently of suit sensors and PVS.
/// </summary>
public sealed class CultMiniMapTrackingSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobThresholdSystem _thresholds = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private TimeSpan _nextUpdate;
    private uint _nextPingId;
    private readonly List<ActivePing> _pings = new();
    private readonly Dictionary<EntityUid, TimeSpan> _nextPingByOwner = new();
    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);
    private const int MaxStoredPingsPerChannel = 64;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultYoggComponent, ComponentInit>(OnCultInit);
        SubscribeLocalEvent<MiGoComponent, ComponentInit>(OnMiGoInit);
        SubscribeLocalEvent<CultYoggComponent, ComponentShutdown>(OnCultShutdown);
        SubscribeLocalEvent<MiGoComponent, ComponentShutdown>(OnMiGoShutdown);
        SubscribeLocalEvent<CultMiniMapComponent, BoundUIOpenedEvent>(OnOpened);
        SubscribeLocalEvent<CultMiniMapComponent, BoundUIClosedEvent>(OnClosed);
        SubscribeLocalEvent<CultMiniMapComponent, CultMiniMapPingMessage>(OnPing);
        SubscribeLocalEvent<CultMiniMapComponent, ComponentRemove>(OnMapRemove);
    }

    private void OnCultInit(Entity<CultYoggComponent> ent, ref ComponentInit args)
    {
        EnsureComp<CultMiniMapComponent>(ent);
    }

    private void OnMiGoInit(Entity<MiGoComponent> ent, ref ComponentInit args)
    {
        EnsureComp<CultMiniMapComponent>(ent);
    }

    private void OnCultShutdown(Entity<CultYoggComponent> ent, ref ComponentShutdown args)
    {
        if (!TerminatingOrDeleted(ent) && !HasComp<MiGoComponent>(ent))
            RemComp<CultMiniMapComponent>(ent);
    }

    private void OnMiGoShutdown(Entity<MiGoComponent> ent, ref ComponentShutdown args)
    {
        if (!TerminatingOrDeleted(ent) && !HasComp<CultYoggComponent>(ent))
            RemComp<CultMiniMapComponent>(ent);
    }

    private void OnOpened(Entity<CultMiniMapComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (!args.UiKey.Equals(CultMiniMapUIKey.Key))
            return;

        if (ent.Comp.State != null)
            return;

        UpdateUserInterface(ent);
    }

    private void OnClosed(Entity<CultMiniMapComponent> ent, ref BoundUIClosedEvent args)
    {
        if (!args.UiKey.Equals(CultMiniMapUIKey.Key))
            return;

        if (_ui.IsUiOpen(ent.Owner, CultMiniMapUIKey.Key))
            return;

        if (ent.Comp.State == null)
            return;

        ent.Comp.State = null;
        Dirty(ent);
    }

    private void OnPing(Entity<CultMiniMapComponent> ent, ref CultMiniMapPingMessage args)
    {
        TryCreatePing((ent.Owner, ent.Comp), args.Actor, GetCoordinates(args.Coordinates));
    }

    private void OnMapRemove(Entity<CultMiniMapComponent> ent, ref ComponentRemove args)
    {
        _nextPingByOwner.Remove(ent.Owner);
    }

    /// <summary>
    /// Validates and publishes a ping. Public so non-UI callers can use the same authoritative path.
    /// </summary>
    public bool TryCreatePing(Entity<CultMiniMapComponent?> ent, EntityUid actor, EntityCoordinates coordinates)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return false;

        if (actor != ent.Owner)
            return false;

        if (!_ui.IsUiOpen(ent.Owner, CultMiniMapUIKey.Key))
            return false;

        var grid = Transform(ent).GridUid;
        if (grid == null)
            return false;

        if (coordinates.EntityId != grid)
            return false;

        if (!coordinates.IsValid(EntityManager))
            return false;

        if (!TryComp<MapGridComponent>(grid.Value, out var gridComp))
            return false;

        if (!gridComp.LocalAABB.Contains(coordinates.Position))
            return false;

        var now = _timing.CurTime;
        if (_nextPingByOwner.TryGetValue(ent.Owner, out var nextPing) && nextPing > now)
            return false;

        var cooldown = ent.Comp.PingCooldown >= TimeSpan.Zero ? ent.Comp.PingCooldown : TimeSpan.FromSeconds(3);
        var duration = ent.Comp.PingDuration > TimeSpan.Zero ? ent.Comp.PingDuration : TimeSpan.FromSeconds(8);
        var scale = PositiveOrDefault(ent.Comp.PingScale, 1f);
        _nextPingByOwner[ent.Owner] = now + cooldown;

        TrimChannel(ent.Comp.PingChannel, MaxStoredPingsPerChannel);
        _pings.Add(new ActivePing(
            NextPingId(),
            grid.Value,
            coordinates.Position,
            ent.Comp.PingChannel,
            ent.Comp.PingIcon,
            ent.Comp.PingColor,
            scale,
            now + duration));

        BroadcastPings(ent.Comp.PingChannel);
        return true;
    }

    private static float PositiveOrDefault(float value, float fallback)
    {
        return float.IsFinite(value) && value > 0f ? value : fallback;
    }

    private void TrimChannel(string channel, int maxActive)
    {
        while (_pings.Count(ping => ping.Channel == channel) >= maxActive)
        {
            var oldest = _pings.FindIndex(ping => ping.Channel == channel);
            if (oldest < 0)
                return;

            _pings.RemoveAt(oldest);
        }
    }

    private uint NextPingId()
    {
        do
        {
            _nextPingId++;
        } while (_nextPingId == 0);

        return _nextPingId;
    }

    private void BroadcastPings(string channel)
    {
        var query = EntityQueryEnumerator<CultMiniMapComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.PingChannel != channel)
                continue;

            if (!_ui.IsUiOpen(uid, CultMiniMapUIKey.Key))
                continue;

            UpdateUserInterface((uid, component));
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        if (now < _nextUpdate)
            return;

        _nextUpdate = now + UpdateInterval;
        _pings.RemoveAll(ping => ping.ExpiresAt <= now || !Exists(ping.Grid));
        var query = EntityQueryEnumerator<CultMiniMapComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_ui.IsUiOpen(uid, CultMiniMapUIKey.Key))
                UpdateUserInterface((uid, comp));
        }
    }

    private void UpdateUserInterface(Entity<CultMiniMapComponent> ent)
    {
        var viewer = Transform(ent);
        var grid = viewer.GridUid;
        if (grid != null)
            EnsureComp<NavMapComponent>(grid.Value);

        var trackedEntities = new List<CultMiniMapTrackedEntity>
        {
            CreateTrackedEntity(viewer, ent.Owner, CreateSelfMarker(ent.Comp)),
        };
        var seen = new HashSet<EntityUid> { ent.Owner };
        var ruleIndex = 0;
        foreach (var rule in ResolveTrackingRules(ent.Comp))
            AddTrackedEntities(viewer, rule, ruleIndex++, trackedEntities, seen);

        var maxActivePings = Math.Clamp(ent.Comp.MaxActivePings, 1, MaxStoredPingsPerChannel);
        var pings = GetVisiblePings(ent.Comp.PingChannel, viewer, grid, maxActivePings);

        ent.Comp.State = new CultMiniMapState(
            GetNetEntity(grid),
            grid == null ? string.Empty : MetaData(grid.Value).EntityName,
            trackedEntities,
            pings);
        Dirty(ent);
    }

    private List<CultMiniMapPing> GetVisiblePings(
        string channel,
        TransformComponent viewer,
        EntityUid? grid,
        int maxActivePings)
    {
        var result = new List<CultMiniMapPing>();
        if (grid == null)
            return result;

        foreach (var ping in _pings)
        {
            if (ping.Channel != channel || !Exists(ping.Grid))
                continue;

            var mapCoordinates = _transform.ToMapCoordinates(new EntityCoordinates(ping.Grid, ping.Position));
            if (mapCoordinates.MapId != viewer.MapID)
                continue;

            var coordinates = _transform.ToCoordinates(grid.Value, mapCoordinates);
            result.Add(new CultMiniMapPing(
                ping.Id,
                GetNetCoordinates(coordinates),
                ping.Icon,
                ping.Color,
                ping.Scale));
        }

        if (result.Count > maxActivePings)
            result.RemoveRange(0, result.Count - maxActivePings);

        return result;
    }

    private IEnumerable<CultMiniMapTrackingRule> ResolveTrackingRules(CultMiniMapComponent component)
    {
        var trackingRules = component.TrackingRules;
        if (trackingRules != null)
        {
            foreach (var rule in trackingRules)
                yield return rule;

            yield break;
        }

        if (!_prototype.TryIndex(component.TrackingProfile, out var profile))
            yield break;

        foreach (var ruleId in profile.Rules)
        {
            if (_prototype.TryIndex(ruleId, out var rule))
                yield return rule;
        }
    }

    private static CultMiniMapMarker CreateSelfMarker(CultMiniMapComponent component)
    {
        return new CultMiniMapMarker(
            CultMiniMapMarker.SelfRuleIndex,
            CultMiniMapMarker.SelfComponent,
            "cult-mini-map-self-section",
            component.SelfIcon,
            component.SelfColor,
            PositiveOrDefault(component.SelfScale, 1f),
            CultMiniMapMarkerType.Icon,
            true,
            true);
    }

    private void AddTrackedEntities(
        TransformComponent viewer,
        CultMiniMapTrackingRule rule,
        int ruleIndex,
        List<CultMiniMapTrackedEntity> trackedEntities,
        HashSet<EntityUid> seen)
    {
        // YAML validates component names; tolerate unavailable types in runtime edits as well.
        if (!_componentFactory.TryGetRegistration(rule.ComponentName, out var registration))
            return;

        var marker = CreateRuleMarker(rule, ruleIndex);
        var query = EntityManager.AllEntityQueryEnumerator(registration.Type);
        while (query.MoveNext(out var uid, out _))
        {
            if (TerminatingOrDeleted(uid))
                continue;

            if (!MatchesPrototype(uid, rule))
                continue;

            if (!seen.Add(uid))
                continue;

            trackedEntities.Add(CreateTrackedEntity(viewer, uid, marker));
        }
    }

    private static CultMiniMapMarker CreateRuleMarker(CultMiniMapTrackingRule rule, int ruleIndex)
    {
        return new CultMiniMapMarker(
            ruleIndex,
            rule.ComponentName,
            rule.Label,
            rule.Icon,
            rule.Color,
            PositiveOrDefault(rule.Scale, 1f),
            rule.MarkerType,
            rule.ShowInList,
            rule.ShowHealth);
    }

    private bool MatchesPrototype(EntityUid uid, CultMiniMapTrackingRule rule)
    {
        if (rule.Prototypes.Count == 0)
            return true;

        var id = MetaData(uid).EntityPrototype?.ID;
        if (id == null)
            return false;

        return rule.Prototypes.Contains(new EntProtoId(id));
    }

    private CultMiniMapTrackedEntity CreateTrackedEntity(
        TransformComponent viewer,
        EntityUid uid,
        CultMiniMapMarker marker)
    {
        var xform = Transform(uid);
        var meta = MetaData(uid);
        NetCoordinates? coordinates = null;
        var viewerGrid = viewer.GridUid;
        if (viewerGrid != null && xform.MapID != MapId.Nullspace && xform.MapID == viewer.MapID)
            coordinates = GetNetCoordinates(_transform.WithEntityId(xform.Coordinates, viewerGrid.Value));

        var healthState = marker.ShowHealth ? GetHealthState(uid) : MobState.Invalid;
        var damagePercentage = marker.ShowHealth ? GetDamagePercentage(uid) : null;
        var rotation = GetMarkerRotation(xform, viewerGrid, marker.MarkerType);

        return new CultMiniMapTrackedEntity(meta.NetEntity, meta.EntityName, marker,
            coordinates, (float) rotation.Theta, healthState, damagePercentage,
            GetStructureLocation(xform, marker.MarkerType));
    }

    private Angle GetMarkerRotation(
        TransformComponent xform,
        EntityUid? viewerGrid,
        CultMiniMapMarkerType markerType)
    {
        if (viewerGrid == null)
            return xform.LocalRotation;

        var rotation = _transform.GetWorldRotation(xform);
        var sourceGrid = xform.GridUid;
        // Neighbor directions use source-grid tiles, independent of the wall's local rotation.
        if (sourceGrid != null && markerType is CultMiniMapMarkerType.Wall or CultMiniMapMarkerType.SecretDoor)
            rotation = _transform.GetWorldRotation(sourceGrid.Value);

        return rotation - _transform.GetWorldRotation(viewerGrid.Value);
    }

    private CultMiniMapStructureLocation? GetStructureLocation(
        TransformComponent xform,
        CultMiniMapMarkerType markerType)
    {
        if (markerType == CultMiniMapMarkerType.Icon)
            return null;

        var grid = xform.GridUid;
        if (grid == null)
            return null;

        if (!TryComp<MapGridComponent>(grid.Value, out var gridComp))
            return null;

        var tile = _map.TileIndicesFor(grid.Value, gridComp, xform.Coordinates);
        return new CultMiniMapStructureLocation(GetNetEntity(grid.Value), tile);
    }

    private MobState GetHealthState(EntityUid uid)
    {
        return TryComp<MobStateComponent>(uid, out var mobState)
            ? mobState.CurrentState
            : MobState.Invalid;
    }

    private float? GetDamagePercentage(EntityUid uid)
    {
        if (!TryComp<DamageableComponent>(uid, out var damageable))
            return null;

        if (!TryComp<MobThresholdsComponent>(uid, out var thresholds))
            return null;

        if (!_thresholds.TryGetThresholdForState(uid, MobState.Critical, out var criticalThreshold, thresholds))
            return null;

        if (criticalThreshold.Value <= 0)
            return null;

        // Crew monitoring uses this legacy API for the same percentage; the engine has no numeric replacement yet.
#pragma warning disable CS0618 // DamageableSystem.GetTotalDamage
        var totalDamage = _damageable.GetTotalDamage((uid, damageable)).Float();
#pragma warning restore CS0618
        return MathF.Max(0f, totalDamage / criticalThreshold.Value.Float());
    }

    private sealed record ActivePing(
        uint Id,
        EntityUid Grid,
        Vector2 Position,
        string Channel,
        SpriteSpecifier Icon,
        Color Color,
        float Scale,
        TimeSpan ExpiresAt);
}
