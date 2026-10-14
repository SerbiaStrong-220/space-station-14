// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Numerics;
using Content.Shared.Medical.SuitSensors;
using Content.Shared.SS220.Silicons.StationAi.CameraMap;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;

namespace Content.Server.SS220.Silicons.StationAi.CameraMap;

public sealed partial class StationAiCameraMapSystem
{
    private const int CriticalHealthLevel = 5;

    protected override bool TryMoveEyeToCrew(Entity<StationAiCameraMapComponent> ai, NetEntity sensor)
    {
        if (!TryGetEntity(sensor, out var uid))
            return false;

        if (TryMoveEyeToCrew(ai.Owner, uid.Value, out var failure))
            return true;

        if (failure != null)
        {
            var message = new StationAiCameraMapFailureMessage(failure.Value);
            _ui.ServerSendUiMessage(ai.Owner, StationAiCameraMapUiKey.Key, message, ai.Owner);
        }

        return false;
    }

    /// <summary>
    ///     Same as pressing a crew member in the window: checks the cooldown, moves the eye, starts the cooldown.
    /// </summary>
    public bool TryJumpToCrew(Entity<StationAiCameraMapComponent?> ai, EntityUid sensor,
        out StationAiCameraMapFailure? failure)
    {
        failure = null;
        if (!Resolve(ai, ref ai.Comp, false) || !IsReady(ai))
            return false;

        if (!TryMoveEyeToCrew(ai.Owner, sensor, out failure))
            return false;

        StartCooldown((ai, ai.Comp));
        return true;
    }

    /// <summary>
    ///     Moves the AI eye straight to a crew member whose sensors report coordinates,
    ///     unless they stand where the AI sees only static. Coordinates come from the server, not the client.
    /// </summary>
    private bool TryMoveEyeToCrew(EntityUid ai, EntityUid sensor, out StationAiCameraMapFailure? failure)
    {
        failure = null;
        if (!TryGetRemoteEye(ai, out var eye, out var grid))
            return false;

        if (!TryGetCrew(sensor, grid, StationOf(ai), out _, out var position))
            return false;

        if (position == null)
        {
            failure = StationAiCameraMapFailure.NoCoordinates;
            return false;
        }

        if (!IsInView(grid, position.Value))
        {
            failure = StationAiCameraMapFailure.OutOfView;
            return false;
        }

        _transform.SetCoordinates(eye, new EntityCoordinates(grid, position.Value));
        return true;
    }

    /// <summary>
    ///     The same check the AI overlay uses to decide where to draw static.
    /// </summary>
    public bool IsInView(EntityUid grid, Vector2 position)
    {
        if (!TryComp<MapGridComponent>(grid, out var gridComp) || !TryComp<BroadphaseComponent>(grid, out var broadphase))
            return false;

        var tile = _map.LocalToTile(grid, gridComp, new EntityCoordinates(grid, position));
        return _vision.IsAccessible((grid, broadphase, gridComp), tile);
    }

    /// <summary>
    ///     Reads a suit sensor by the crew monitor rules: sensors on, worn, assigned to the AI's station.
    /// </summary>
    /// <param name="position">Tile centre on the AI's grid, or null without coordinates.</param>
    public bool TryGetCrew(EntityUid sensor, EntityUid grid, EntityUid? station,
        out StationAiCameraMapCrew crew, out Vector2? position)
    {
        crew = default!;
        position = null;
        if (station == null || !TryComp<SuitSensorComponent>(sensor, out var comp) || comp.StationId != station)
            return false;

        if (_sensors.GetSensorState((sensor, comp, null)) is not { } status)
            return false;

        if (status.Coordinates is { } net && GetCoordinates(net) is var coordinates && coordinates.EntityId == grid)
        {
            var tile = coordinates.Position.Floored();
            position = new Vector2(tile.X + 0.5f, tile.Y + 0.5f);
        }

        int? health = null;
        if (status.DamagePercentage is { } damage)
        {
            health = damage >= 1f
                ? CriticalHealthLevel
                : Math.Clamp((int) MathF.Round(4f * damage), 0, CriticalHealthLevel - 1);
        }

        crew = new StationAiCameraMapCrew(GetNetEntity(sensor), status.Name, status.Job, status.JobIcon,
            status.IsAlive, health, position != null);
        return true;
    }

    private EntityUid? StationOf(EntityUid ai)
    {
        return _stationAi.TryGetCore(ai, out var core) ? _station.GetOwningStation(core.Owner) : null;
    }

    private void CollectCrew(EntityUid ai, EntityUid grid, List<StationAiCameraMapCrew> crew,
        Dictionary<NetEntity, Vector2> positions)
    {
        var station = StationOf(ai);
        var sensors = EntityQueryEnumerator<SuitSensorComponent>();
        while (sensors.MoveNext(out var uid, out _))
        {
            if (!TryGetCrew(uid, grid, station, out var member, out var position))
                continue;

            crew.Add(member);
            if (position != null)
                positions[member.Sensor] = position.Value;
        }
    }
}
