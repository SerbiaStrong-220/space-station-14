// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Linq;
using System.Numerics;
using Content.Server.Silicons.StationAi;
using Content.Server.SurveillanceCamera;
using Content.Shared.Medical.SuitSensor;
using Content.Shared.Medical.SuitSensors;
using Content.Shared.Station;
using Content.Shared.ActionBlocker;
using Content.Shared.Pinpointer;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Silicons.StationAi;
using Content.Shared.SS220.Silicons.StationAi;
using Content.Shared.StationAi;
using Content.Shared.SurveillanceCamera.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;

namespace Content.Server.SS220.Silicons.StationAi;

public sealed class StationAiCameraMapSystem : EntitySystem
{
    [Dependency] private readonly StationAiSystem _ai = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _power = default!;
    [Dependency] private readonly SurveillanceCameraMapSystem _cameras = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedSuitSensorSystem _sensors = default!;
    [Dependency] private readonly SharedStationSystem _station = default!;
    [Dependency] private readonly StationAiVisionSystem _vision = default!;
    [Dependency] private readonly SharedMapSystem _maps = default!;

    [Dependency] private readonly IGameTiming _timing = default!;

    private float _elapsed;

    public override void Initialize()
    {
        SubscribeLocalEvent<StationAiCameraMapComponent, BoundUIOpenedEvent>(OnOpened);
        SubscribeLocalEvent<StationAiCameraMapComponent, StationAiCameraMapTeleportMessage>(OnTeleport);
        SubscribeLocalEvent<StationAiCameraMapComponent, StationAiCameraMapJumpToCoreMessage>(OnJumpToCore);
        SubscribeLocalEvent<StationAiCameraMapComponent, StationAiCameraMapCrewMessage>(OnCrew);
    }

    public override void Update(float frameTime)
    {
        _elapsed += frameTime;
        if (_elapsed < 1f)
            return;

        _elapsed = 0f;
        var query = EntityQueryEnumerator<StationAiCameraMapComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            if (_ui.IsUiOpen(uid, StationAiCameraMapUiKey.Key))
                UpdateMap(uid);
        }
    }

    private void OnOpened(Entity<StationAiCameraMapComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (!args.UiKey.Equals(StationAiCameraMapUiKey.Key))
            return;

        if (args.Actor != ent.Owner)
        {
            _ui.CloseUi(ent.Owner, StationAiCameraMapUiKey.Key, args.Actor);
            return;
        }

        // A freshly opened window has no state yet; always send one.
        ent.Comp.LastTargets.Clear();
        ent.Comp.LastCrew.Clear();
        UpdateMap(ent);
    }

    private bool TryGetEye(EntityUid user, out EntityUid eye, out EntityUid grid)
    {
        eye = default;
        grid = default;
        if (!HasComp<StationAiHeldComponent>(user) || !_blocker.CanComplexInteract(user) ||
            !_ai.TryGetCore(user, out var core) || core.Comp is not { Remote: true, RemoteEntity: { } remote } ||
            !Exists(remote) || Transform(core).GridUid is not { } coreGrid || !HasComp<MapGridComponent>(coreGrid))
            return false;

        eye = remote;
        grid = coreGrid;
        return true;
    }

    /// <summary>Revalidated on click: client coordinates and stale map markers are never trusted.</summary>
    public bool TryTeleport(EntityUid user, EntityUid target)
    {
        return TryTeleport(user, target, out _);
    }

    public bool TryTeleport(EntityUid user, EntityUid target, out StationAiCameraMapFailure? failure)
    {
        failure = null;
        if (!TryComp<StationAiCameraMapComponent>(user, out var map) || _timing.CurTime < map.NextTeleport ||
            !TryGetEye(user, out var eye, out var grid) || !TryGetTarget(target, grid, out var entry))
            return false;

        // A beacon is only a destination while a working camera actually sees it.
        if (!entry.Active || !entry.IsCamera && !IsInView(grid, entry.Position))
        {
            failure = entry.IsCamera ? StationAiCameraMapFailure.CameraInactive : StationAiCameraMapFailure.OutOfView;
            return false;
        }

        MoveEye(user, map, eye, _transform.WithEntityId(Transform(target).Coordinates, grid));
        return true;
    }

    private void MoveEye(EntityUid user, StationAiCameraMapComponent map, EntityUid eye, EntityCoordinates coordinates)
    {
        map.NextTeleport = _timing.CurTime + StationAiCameraMapComponent.TeleportCooldown;
        _transform.SetCoordinates(eye, coordinates);
        // Push the new cooldown now instead of on the next periodic refresh.
        if (_ui.IsUiOpen(user, StationAiCameraMapUiKey.Key))
            UpdateMap(user);
    }

    /// <summary>
    /// Same as the vanilla "jump to core" action: raises <see cref="JumpToCoreEvent"/>, so the eye returns
    /// to the core and other listeners (e.g. holopad calls) react as before. Shares the camera cooldown.
    /// </summary>
    public bool TryJumpToCore(EntityUid user)
    {
        if (!TryComp<StationAiCameraMapComponent>(user, out var map) || _timing.CurTime < map.NextTeleport ||
            !TryGetEye(user, out _, out _))
            return false;

        map.NextTeleport = _timing.CurTime + StationAiCameraMapComponent.TeleportCooldown;
        RaiseLocalEvent(user, new JumpToCoreEvent { Performer = user });
        if (_ui.IsUiOpen(user, StationAiCameraMapUiKey.Key))
            UpdateMap(user);
        return true;
    }

    /// <summary>
    /// Moves the eye straight to a crew member whose sensors report coordinates. Refused when their tile is
    /// outside AI vision (the static zone).
    /// </summary>
    public bool TryJumpToCrew(EntityUid user, EntityUid sensor, out StationAiCameraMapFailure? failure)
    {
        failure = null;
        if (!TryComp<StationAiCameraMapComponent>(user, out var map) || _timing.CurTime < map.NextTeleport ||
            !TryGetEye(user, out var eye, out var grid) ||
            !TryGetCrew(sensor, grid, StationOf(user), out var crew))
            return false;

        if (crew.Position is not { } position)
        {
            failure = StationAiCameraMapFailure.NoCoordinates;
            return false;
        }

        if (!IsInView(grid, position))
        {
            failure = StationAiCameraMapFailure.OutOfView;
            return false;
        }

        MoveEye(user, map, eye, new EntityCoordinates(grid, position));
        return true;
    }

    /// <summary>The same check that decides where the AI overlay draws static.</summary>
    public bool IsInView(EntityUid grid, Vector2 position)
    {
        if (!TryComp<MapGridComponent>(grid, out var gridComp) || !TryComp<BroadphaseComponent>(grid, out var broadphase))
            return false;

        var tile = _maps.LocalToTile(grid, gridComp, new EntityCoordinates(grid, position));
        return _vision.IsAccessible((grid, broadphase, gridComp), tile);
    }

    private List<(Vector2 Position, float Range)> GetCoverage(EntityUid grid)
    {
        var coverage = new List<(Vector2, float)>();
        var cameras = EntityQueryEnumerator<SurveillanceCameraComponent, StationAiVisionComponent>();
        while (cameras.MoveNext(out var uid, out _, out var vision))
        {
            if (TryGetTarget(uid, grid, out var camera) && camera.Active)
                coverage.Add((camera.Position, vision.Range));
        }

        return coverage;
    }

    private EntityUid? StationOf(EntityUid user)
    {
        return _ai.TryGetCore(user, out var core) ? _station.GetOwningStation(core.Owner) : null;
    }

    /// <summary>Crew monitor rules: sensors on, worn, and assigned to the AI's station.</summary>
    public bool TryGetCrew(EntityUid sensor, EntityUid grid, EntityUid? station, out StationAiCameraMapCrew crew)
    {
        crew = default;
        if (station == null || !TryComp<SuitSensorComponent>(sensor, out var comp) || comp.StationId != station ||
            _sensors.GetSensorState((sensor, comp, null)) is not { } status)
            return false;

        Vector2? position = null;
        // Tile centre: the map does not need sub-tile precision, and walking within a tile then doesn't
        // force the whole state to be resent. Vision checks are per tile anyway.
        if (status.Coordinates is { } net && GetCoordinates(net) is var coords && coords.EntityId == grid)
            position = new Vector2(MathF.Floor(coords.Position.X) + 0.5f, MathF.Floor(coords.Position.Y) + 0.5f);

        crew = new StationAiCameraMapCrew(GetNetEntity(sensor), status.Name, status.Job, status.JobIcon,
            status.IsAlive, status.DamagePercentage, position);
        return true;
    }

    private void OnCrew(Entity<StationAiCameraMapComponent> ent, ref StationAiCameraMapCrewMessage args)
    {
        if (args.Actor != ent.Owner || !TryGetEntity(args.Sensor, out var sensor))
            return;

        TryJumpToCrew(ent, sensor.Value, out var failure);
        if (failure is { } reason)
            _ui.ServerSendUiMessage(ent.Owner, StationAiCameraMapUiKey.Key, new StationAiCameraMapFailureMessage(reason), ent.Owner);
    }

    private void OnJumpToCore(Entity<StationAiCameraMapComponent> ent, ref StationAiCameraMapJumpToCoreMessage args)
    {
        if (args.Actor == ent.Owner)
            TryJumpToCore(ent);
    }

    private void OnTeleport(Entity<StationAiCameraMapComponent> ent, ref StationAiCameraMapTeleportMessage args)
    {
        if (args.Actor != ent.Owner || !TryGetEntity(args.Target, out var target))
            return;

        TryTeleport(ent, target.Value, out var failure);
        if (failure is { } reason)
            _ui.ServerSendUiMessage(ent.Owner, StationAiCameraMapUiKey.Key, new StationAiCameraMapFailureMessage(reason), ent.Owner);

        UpdateMap(ent);
    }

    /// <summary>
    /// Lists installed cameras even when temporarily unavailable; hidden cameras remain excluded.
    /// Beacons are active only within the vision range of a working camera.
    /// </summary>
    /// <param name="coverage">Working cameras on the grid, precomputed by the periodic refresh.</param>
    public bool TryGetTarget(EntityUid uid, EntityUid grid, out StationAiCameraMapTarget target,
        List<(Vector2 Position, float Range)>? coverage = null)
    {
        target = default;
        if (TerminatingOrDeleted(uid) || !TryComp(uid, out TransformComponent? xform) ||
            xform.GridUid != grid || !xform.Anchored)
            return false;

        string name;
        var active = true;
        var isCamera = TryComp(uid, out SurveillanceCameraComponent? camera);
        if (isCamera)
        {
            if (!_cameras.IsCameraVisible(uid) || !TryComp<StationAiVisionComponent>(uid, out var vision))
                return false;

            active = camera!.Active && _power.IsPowered(uid) && vision.Enabled;
            name = camera.UseEntityNameAsCameraId ? Name(uid) : camera.CameraId;
        }
        else if (TryComp<NavMapBeaconComponent>(uid, out var beacon) && beacon.Enabled)
            name = beacon.Text ?? Name(uid);
        else
            return false;

        var coordinates = _transform.WithEntityId(xform.Coordinates, grid);
        if (!isCamera)
        {
            coverage ??= GetCoverage(grid);
            active = coverage.Any(c => Vector2.Distance(c.Position, coordinates.Position) <= c.Range);
        }

        target = new StationAiCameraMapTarget(GetNetEntity(uid), coordinates.Position, name, isCamera, active);
        return true;
    }

    private void UpdateMap(EntityUid user)
    {
        if (!TryGetEye(user, out _, out var grid))
        {
            _ui.CloseUi(user, StationAiCameraMapUiKey.Key);
            return;
        }

        EnsureComp<NavMapComponent>(grid);
        var targets = new List<StationAiCameraMapTarget>();
        var cameras = EntityQueryEnumerator<SurveillanceCameraComponent>();
        while (cameras.MoveNext(out var uid, out _))
        {
            if (TryGetTarget(uid, grid, out var target))
                targets.Add(target);
        }

        var coverage = new List<(Vector2 Position, float Range)>();
        foreach (var camera in targets)
        {
            if (camera.Active && TryGetEntity(camera.Entity, out var uid) && TryComp<StationAiVisionComponent>(uid, out var vision))
                coverage.Add((camera.Position, vision.Range));
        }

        var beacons = EntityQueryEnumerator<NavMapBeaconComponent>();
        while (beacons.MoveNext(out var uid, out _))
        {
            if (!HasComp<SurveillanceCameraComponent>(uid) && TryGetTarget(uid, grid, out var target, coverage))
                targets.Add(target);
        }

        var crew = new List<StationAiCameraMapCrew>();
        var station = StationOf(user);
        var sensors = EntityQueryEnumerator<SuitSensorComponent>();
        while (sensors.MoveNext(out var uid, out _))
        {
            if (TryGetCrew(uid, grid, station, out var member))
                crew.Add(member);
        }

        // The periodic refresh usually finds nothing new; don't resend ~200 entries every second.
        var map = Comp<StationAiCameraMapComponent>(user);
        if (map.LastGrid == grid && map.LastSentTeleport == map.NextTeleport && map.LastTargets.SequenceEqual(targets) &&
            map.LastCrew.SequenceEqual(crew))
            return;

        map.LastGrid = grid;
        map.LastSentTeleport = map.NextTeleport;
        map.LastTargets = targets;
        map.LastCrew = crew;
        _ui.SetUiState(user, StationAiCameraMapUiKey.Key,
            new StationAiCameraMapState(GetNetEntity(grid), targets, crew, map.NextTeleport));
    }
}
