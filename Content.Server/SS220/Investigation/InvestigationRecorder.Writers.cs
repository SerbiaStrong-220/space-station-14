// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Content.Server.SS220.Investigation;

public sealed partial class InvestigationRecorder
{
    public void TrackEntity(EntityUid uid, Guid? playerGuid, string? userName, string name, string? prototype)
    {
        if (_session is not { } session || _roster.ContainsKey(uid))
            return;

        var entry = new RosterEntry(playerGuid, userName, name, prototype, _timing.CurTick.Value);
        _roster[uid] = entry;

        session.Roster.Write(JsonSerializer.Serialize(new
        {
            e = uid.Id,
            player = entry.PlayerGuid?.ToString(),
            userName = entry.UserName,
            name = entry.Name,
            prototype = entry.Prototype,
            firstTick = entry.FirstTick,
        }, JsonOptions));
        session.RowCount++;
    }

    public void WriteControl(EntityUid uid, Guid? playerGuid, string? userName, bool attached)
    {
        if (_session is not { } session)
            return;

        session.Control.Write(JsonSerializer.Serialize(new
        {
            t = _timing.CurTick.Value,
            e = uid.Id,
            player = playerGuid?.ToString(),
            userName,
            action = attached ? "attach" : "detach",
        }, JsonOptions));
        session.RowCount++;
    }

    public void WriteAdminPresence(Guid player, string? userName, bool active)
    {
        if (_session is not { } session)
            return;

        session.Admins.Write(JsonSerializer.Serialize(new
        {
            t = _timing.CurTick.Value,
            player = player.ToString(),
            userName,
            active,
        }, JsonOptions));
        session.RowCount++;
    }

    public void UpdateKnownPosition(EntityUid uid, SampledPosition sample)
    {
        _positions[uid] = sample;
    }

    public void WritePositionRow(EntityUid uid, uint tick, SampledPosition sample)
    {
        if (_session is not { } session)
            return;

        _rowBuilder.Clear();
        _rowBuilder.Append("{\"t\":").Append(tick)
            .Append(",\"e\":").Append(uid.Id)
            .Append(",\"g\":");

        if (sample.Grid is { } gridUid)
            _rowBuilder.Append(gridUid.Id);
        else
            _rowBuilder.Append("null");

        _rowBuilder.Append(",\"x\":").Append(FormatCoordinate(sample.Local.X))
            .Append(",\"y\":").Append(FormatCoordinate(sample.Local.Y));

        if (sample.Container is { } containerUid)
            _rowBuilder.Append(",\"c\":").Append(containerUid.Id);

        // Off-grid coordinates are in the map frame, so the map has to be named for them to mean anything.
        if (sample.Grid is null)
            _rowBuilder.Append(",\"m\":").Append(sample.Map);

        _rowBuilder.Append('}');

        session.Positions.Write(_rowBuilder.ToString());
        session.RowCount++;
    }

    /// <remarks>A non-finite coordinate is not valid JSON and would make its whole line unparseable, so it is clamped.</remarks>
    private static string FormatCoordinate(float value)
    {
        if (!float.IsFinite(value))
            return "0";

        return value.ToString("0.#", CultureInfo.InvariantCulture);
    }

    public void WriteGridPose(EntityUid grid, GridPose pose, string? mapName, string? gridName)
    {
        if (_session is not { } session)
            return;

        if (mapName != null)
            _maps[pose.Map] = mapName;

        if (!string.IsNullOrEmpty(gridName))
            _grids[grid.Id] = gridName;

        session.GridPose.Write(JsonSerializer.Serialize(new
        {
            t = _timing.CurTick.Value,
            g = grid.Id,
            m = pose.Map,
            wx = Math.Round(pose.World.X, 2),
            wy = Math.Round(pose.World.Y, 2),
            rot = Math.Round(pose.Rotation, 4),
        }, JsonOptions));
        session.RowCount++;
    }

    public void WriteNavMapChunk(EntityUid grid, Vector2i origin, int[] tiles)
    {
        if (_session is not { } session)
            return;

        session.NavMap.Write(JsonSerializer.Serialize(new
        {
            t = _timing.CurTick.Value,
            g = grid.Id,
            cx = origin.X,
            cy = origin.Y,
            tiles,
        }, JsonOptions));
        session.RowCount++;
    }

    public void WriteNavMapBeacons(EntityUid grid, List<BeaconRow> beacons)
    {
        if (_session is not { } session)
            return;

        session.NavMap.Write(JsonSerializer.Serialize(new
        {
            t = _timing.CurTick.Value,
            g = grid.Id,
            beacons,
        }, JsonOptions));
        session.RowCount++;
    }

    public void WriteHealthRow(EntityUid uid, HealthSample sample, float? critThreshold, float? deadThreshold)
    {
        if (_session is not { } session)
            return;

        session.Health.Write(JsonSerializer.Serialize(new
        {
            t = _timing.CurTick.Value,
            e = uid.Id,
            dmg = sample.Damage,
            state = sample.State,
            crit = critThreshold.HasValue ? Math.Round(critThreshold.Value, 2) : (double?) null,
            dead = deadThreshold.HasValue ? Math.Round(deadThreshold.Value, 2) : (double?) null,
        }, JsonOptions));
        session.RowCount++;
    }

    public void WriteObjectiveIfChanged(
        EntityUid objective,
        EntityUid owner,
        string? prototype,
        string title,
        string? description,
        float progress)
    {
        if (_session is not { } session)
            return;

        var rounded = Math.Round(progress, 2);
        var complete = progress >= CompletionThreshold;
        var sample = new ObjectiveSample(rounded, complete);

        if (_objectiveSamples.TryGetValue(objective, out var previous) && previous == sample)
            return;

        _objectiveSamples[objective] = sample;

        session.Objectives.Write(JsonSerializer.Serialize(new
        {
            t = _timing.CurTick.Value,
            o = objective.Id,
            e = owner.Id,
            proto = prototype,
            title,
            desc = description,
            progress = rounded,
            done = complete,
        }, JsonOptions));
        session.RowCount++;
    }

    public void WriteCharacterRow(CharacterSnapshot snapshot)
    {
        if (_session is not { } session)
            return;

        session.Characters.Write(JsonSerializer.Serialize(snapshot, JsonOptions));
        session.RowCount++;
    }

    public readonly record struct RosterEntry(
        Guid? PlayerGuid,
        string? UserName,
        string Name,
        string? Prototype,
        uint FirstTick);

    public readonly record struct BeaconRow(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("x")] double X,
        [property: JsonPropertyName("y")] double Y);

    private readonly record struct ObjectiveSample(double Progress, bool Complete);
}
