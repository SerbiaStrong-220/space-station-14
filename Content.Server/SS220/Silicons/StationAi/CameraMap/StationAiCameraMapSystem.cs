// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using System.Numerics;
using Content.Server.Silicons.StationAi;
using Content.Server.SurveillanceCamera;
using Content.Shared.ActionBlocker;
using Content.Shared.Pinpointer;
using Content.Shared.Medical.SuitSensors;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Silicons.StationAi;
using Content.Shared.SS220.Silicons.StationAi.CameraMap;
using Content.Shared.Station;
using Content.Shared.StationAi;
using Content.Shared.SurveillanceCamera.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Map.Components;

namespace Content.Server.SS220.Silicons.StationAi.CameraMap;

public sealed partial class StationAiCameraMapSystem : SharedStationAiCameraMapSystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _power = default!;
    [Dependency] private readonly SharedSuitSensorSystem _sensors = default!;
    [Dependency] private readonly SharedStationSystem _station = default!;
    [Dependency] private readonly StationAiSystem _stationAi = default!;
    [Dependency] private readonly SurveillanceCameraMapSystem _cameraMap = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly StationAiVisionSystem _vision = default!;

    /// <summary>
    ///     Set by camera and beacon events; open maps are refreshed on the next tick.
    /// </summary>
    private bool _targetsChanged;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StationAiCameraMapComponent, BoundUIOpenedEvent>(OnOpened);

        // Vanilla systems already own camera power/shutdown and beacon anchoring; those paths are
        // covered by the deactivation broadcast or by the periodic refresh.
        SubscribeLocalEvent<SurveillanceCameraDeactivateEvent>(OnCameraDeactivated);
        SubscribeLocalEvent<SurveillanceCameraComponent, AnchorStateChangedEvent>(OnCameraAnchorChanged);
        SubscribeLocalEvent<NavMapBeaconComponent, ComponentShutdown>(OnBeaconShutdown);
    }

    /// <summary>
    ///     Events cover camera deactivation (including power loss and removal), camera anchoring and beacon
    ///     removal. Re-activation, the AI vision wire, monitor visibility and beacon toggles or anchoring raise
    ///     no event this system can subscribe to, so a periodic refresh catches those.
    /// </summary>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var changed = _targetsChanged;
        _targetsChanged = false;
        var query = EntityQueryEnumerator<StationAiCameraMapComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            var due = UpdateDue((uid, comp));
            if ((changed || due) && _ui.IsUiOpen(uid, StationAiCameraMapUiKey.Key))
                UpdateUi(uid);
        }
    }

    private void OnCameraDeactivated(SurveillanceCameraDeactivateEvent args)
    {
        _targetsChanged = true;
    }

    private void OnCameraAnchorChanged(Entity<SurveillanceCameraComponent> ent, ref AnchorStateChangedEvent args)
    {
        _targetsChanged = true;
    }

    private void OnBeaconShutdown(Entity<NavMapBeaconComponent> ent, ref ComponentShutdown args)
    {
        _targetsChanged = true;
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

        UpdateUi(ent);
    }

    protected override bool TryMoveEye(Entity<StationAiCameraMapComponent> ai, NetEntity target)
    {
        if (!TryGetEntity(target, out var uid))
            return false;

        if (TryMoveEye(ai.Owner, uid.Value, out var failure))
            return true;

        if (failure != null)
        {
            var message = new StationAiCameraMapFailureMessage(failure.Value);
            _ui.ServerSendUiMessage(ai.Owner, StationAiCameraMapUiKey.Key, message, ai.Owner);
        }

        return false;
    }

    /// <summary>
    ///     Same as pressing a camera in the window: checks the cooldown, moves the eye, starts the cooldown.
    /// </summary>
    public bool TryJump(Entity<StationAiCameraMapComponent?> ai, EntityUid target,
        out StationAiCameraMapFailure? failure)
    {
        failure = null;
        if (!Resolve(ai, ref ai.Comp, false) || !IsReady(ai))
            return false;

        if (!TryMoveEye(ai.Owner, target, out failure))
            return false;

        StartCooldown((ai, ai.Comp));
        return true;
    }

    /// <summary>
    ///     Moves the AI eye to an active camera, or to a beacon that a working camera covers.
    ///     The target is checked again here because the client map may be out of date.
    /// </summary>
    private bool TryMoveEye(EntityUid ai, EntityUid target, out StationAiCameraMapFailure? failure)
    {
        failure = null;
        if (!TryGetRemoteEye(ai, out var eye, out var grid) || !TryGetTarget(target, grid, out var entry))
            return false;

        if (!entry.Active)
        {
            failure = entry.IsCamera
                ? StationAiCameraMapFailure.CameraInactive
                : StationAiCameraMapFailure.NoCameraNearby;
            return false;
        }

        _transform.SetCoordinates(eye, _transform.WithEntityId(Transform(target).Coordinates, grid));
        return true;
    }

    /// <summary>
    ///     Finds the eye of an AI that is currently looking around the station remotely.
    /// </summary>
    private bool TryGetRemoteEye(EntityUid ai, out EntityUid eye, out EntityUid grid)
    {
        eye = default;
        grid = default;
        if (!HasComp<StationAiHeldComponent>(ai) || !_actionBlocker.CanComplexInteract(ai))
            return false;

        if (!_stationAi.TryGetCore(ai, out var core) || core.Comp is not { Remote: true, RemoteEntity: { } remote })
            return false;

        if (!Exists(remote) || Transform(core).GridUid is not { } coreGrid || !HasComp<MapGridComponent>(coreGrid))
            return false;

        eye = remote;
        grid = coreGrid;
        return true;
    }

    /// <summary>
    ///     Describes a camera or beacon on the given grid. Disabled cameras stay listed as inactive;
    ///     cameras hidden from monitors are not listed at all.
    /// </summary>
    public bool TryGetTarget(EntityUid uid, EntityUid grid, out StationAiCameraMapTarget target)
    {
        return TryGetTarget(uid, grid, null, out target);
    }

    private bool TryGetTarget(EntityUid uid, EntityUid grid, List<CameraCoverage>? coverage,
        out StationAiCameraMapTarget target)
    {
        target = default!;
        if (TerminatingOrDeleted(uid) || !TryComp(uid, out TransformComponent? xform))
            return false;

        if (xform.GridUid != grid || !xform.Anchored)
            return false;

        var position = _transform.WithEntityId(xform.Coordinates, grid).Position;
        if (TryComp<SurveillanceCameraComponent>(uid, out var camera))
        {
            if (!_cameraMap.IsCameraVisible(uid) || !TryComp<StationAiVisionComponent>(uid, out var vision))
                return false;

            var name = camera.UseEntityNameAsCameraId ? Name(uid) : camera.CameraId;
            var active = camera.Active && vision.Enabled && _power.IsPowered(uid);
            target = new StationAiCameraMapTarget(GetNetEntity(uid), position, name, true, active);
            return true;
        }

        if (!TryComp<NavMapBeaconComponent>(uid, out var beacon) || !beacon.Enabled)
            return false;

        coverage ??= GetCoverage(grid);
        var covered = coverage.Any(c => Vector2.Distance(c.Position, position) <= c.Range);
        target = new StationAiCameraMapTarget(GetNetEntity(uid), position, beacon.Text ?? Name(uid), false, covered);
        return true;
    }

    /// <summary>
    ///     Active cameras on the grid; a beacon is reachable only inside one of their vision ranges.
    /// </summary>
    private List<CameraCoverage> GetCoverage(EntityUid grid)
    {
        var coverage = new List<CameraCoverage>();
        var cameras = EntityQueryEnumerator<SurveillanceCameraComponent, StationAiVisionComponent>();
        while (cameras.MoveNext(out var uid, out _, out var vision))
        {
            if (TryGetTarget(uid, grid, out var camera) && camera.Active)
                coverage.Add(new CameraCoverage(camera.Position, vision.Range));
        }

        return coverage;
    }

    private void UpdateUi(EntityUid ai)
    {
        if (!TryGetRemoteEye(ai, out _, out var grid))
        {
            _ui.CloseUi(ai, StationAiCameraMapUiKey.Key);
            return;
        }

        EnsureComp<NavMapComponent>(grid);
        var coverage = GetCoverage(grid);
        var targets = new List<StationAiCameraMapTarget>();
        var cameras = EntityQueryEnumerator<SurveillanceCameraComponent>();
        while (cameras.MoveNext(out var uid, out _))
        {
            if (TryGetTarget(uid, grid, coverage, out var target))
                targets.Add(target);
        }

        var beacons = EntityQueryEnumerator<NavMapBeaconComponent>();
        while (beacons.MoveNext(out var uid, out _))
        {
            if (!HasComp<SurveillanceCameraComponent>(uid) && TryGetTarget(uid, grid, coverage, out var target))
                targets.Add(target);
        }

        var crew = new List<StationAiCameraMapCrew>();
        var positions = new Dictionary<NetEntity, Vector2>();
        CollectCrew(ai, grid, crew, positions);

        var state = new StationAiCameraMapState(GetNetEntity(grid), targets, crew, positions);
        _ui.SetUiState(ai, StationAiCameraMapUiKey.Key, state);
    }

    private readonly record struct CameraCoverage(Vector2 Position, float Range);
}
