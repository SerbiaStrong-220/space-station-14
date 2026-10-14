// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using System.Numerics;
using Content.Server.Silicons.StationAi;
using Content.Server.SurveillanceCamera;
using Content.Shared.ActionBlocker;
using Content.Shared.Pinpointer;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Silicons.StationAi;
using Content.Shared.SS220.Silicons.StationAi.CameraMap;
using Content.Shared.StationAi;
using Content.Shared.SurveillanceCamera.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Map.Components;

namespace Content.Server.SS220.Silicons.StationAi.CameraMap;

public sealed class StationAiCameraMapSystem : SharedStationAiCameraMapSystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedPowerReceiverSystem _power = default!;
    [Dependency] private readonly StationAiSystem _stationAi = default!;
    [Dependency] private readonly SurveillanceCameraMapSystem _cameraMap = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StationAiCameraMapComponent, BoundUIOpenedEvent>(OnOpened);
        SubscribeLocalEvent<StationAiCameraMapComponent, StationAiCameraMapJumpMessage>(OnJump);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<StationAiCameraMapComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (UpdateDue((uid, comp)) && _ui.IsUiOpen(uid, StationAiCameraMapUiKey.Key))
                UpdateUi(uid);
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

        UpdateUi(ent);
    }

    private void OnJump(Entity<StationAiCameraMapComponent> ent, ref StationAiCameraMapJumpMessage args)
    {
        if (args.Actor != ent.Owner || !TryGetEntity(args.Target, out var target))
            return;

        if (TryJump(ent.AsNullable(), target.Value, out var failure) || failure == null)
            return;

        var message = new StationAiCameraMapFailureMessage(failure.Value);
        _ui.ServerSendUiMessage(ent.Owner, StationAiCameraMapUiKey.Key, message, ent.Owner);
    }

    /// <summary>
    ///     Moves the AI eye to an active camera, or to a beacon that a working camera covers.
    ///     The target is checked again here because the client map can be up to a second old.
    /// </summary>
    public bool TryJump(Entity<StationAiCameraMapComponent?> ai, EntityUid target,
        out StationAiCameraMapFailure? failure)
    {
        failure = null;
        if (!Resolve(ai, ref ai.Comp, false) || !IsReady(ai))
            return false;

        if (!TryGetRemoteEye(ai, out var eye, out var grid) || !TryGetTarget(target, grid, out var entry))
            return false;

        if (!entry.Active)
        {
            failure = entry.IsCamera
                ? StationAiCameraMapFailure.CameraInactive
                : StationAiCameraMapFailure.NoCameraNearby;
            return false;
        }

        StartCooldown((ai, ai.Comp));
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

        _ui.SetUiState(ai, StationAiCameraMapUiKey.Key, new StationAiCameraMapState(GetNetEntity(grid), targets));
    }

    private readonly record struct CameraCoverage(Vector2 Position, float Range);
}
