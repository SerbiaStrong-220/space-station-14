using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Investigation;

/// <summary>
///     Writes the per-round investigation bundle. See <see cref="IInvestigationRecorder"/>.
/// </summary>
/// <remarks>
///     Everything here runs on the main game thread: the sampling loops are driven by
///     <see cref="InvestigationRecorderSystem"/> and the admin log hook is called synchronously from
///     <see cref="Content.Server.Administration.Logs.AdminLogManager.Add"/>. Rows are buffered in memory and
///     flushed to gzip streams on an interval so that disk I/O stays off the hot path.
/// </remarks>
public sealed class InvestigationRecorder : IInvestigationRecorder
{
    /// <summary>
    ///     Bump this when the on-disk row shapes change in a way readers must care about.
    /// </summary>
    public const int SchemaVersion = 1;

    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IResourceManager _res = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ILogManager _logManager = default!;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private ISawmill _sawmill = default!;
    private bool _enabled;
    private float _flushInterval;
    private float _flushAccumulator;

    private Session? _session;
    private readonly StringBuilder _sb = new();

    /// <summary>
    ///     Every entity that has ever been player-controlled this round, and is therefore tracked for the rest of it.
    ///     Keyed by <see cref="EntityUid.Id"/>, which is the same value admin logs record, and which is never
    ///     recycled within a round (see <c>EntityManager.GenerateEntityUid</c>).
    /// </summary>
    public Dictionary<EntityUid, RosterEntry> Roster { get; } = new();

    /// <summary>
    ///     Last sampled position of each tracked entity, used both to skip unchanged rows and to enrich admin logs.
    /// </summary>
    private readonly Dictionary<EntityUid, SampledPosition> _positions = new();

    /// <summary>
    ///     Last written loadout fingerprint per tracked entity, so we only emit a character row when it changes.
    /// </summary>
    private readonly Dictionary<EntityUid, int> _loadoutHashes = new();

    public bool IsRecording => _session != null;

    /// <summary>
    ///     Directory of the most recently completed bundle, or null if no round has finished recording yet.
    /// </summary>
    public ResPath? LastBundlePath { get; private set; }

    /// <summary>
    ///     Directory of the bundle currently being written, or null when not recording.
    /// </summary>
    public ResPath? CurrentBundlePath => _session?.Directory;

    /// <summary>
    ///     Flushes buffered rows to disk immediately, rather than waiting for the next interval.
    /// </summary>
    public void Flush()
    {
        if (_session is { } session)
            FlushSession(session);
    }

    public void Initialize()
    {
        _sawmill = _logManager.GetSawmill("investigation");
        _cfg.OnValueChanged(CCVars.InvestigationEnabled, v => _enabled = v, true);
        _cfg.OnValueChanged(CCVars.InvestigationFlushInterval, v => _flushInterval = v, true);
    }

    public void Shutdown()
    {
        if (_session != null)
            StopRound(null);
    }

    #region Session lifecycle

    public void StartRound(int roundId, string? map)
    {
        if (!_enabled || _session != null)
            return;

        try
        {
            var baseDir = new ResPath(_cfg.GetCVar(CCVars.InvestigationDirectory)).ToRootedPath();
            var stamp = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture);
            var roundDir = baseDir / $"round-{roundId}_{stamp}";

            _res.UserData.CreateDir(roundDir);

            _session = new Session(roundId, roundDir, map, DateTime.UtcNow, _timing.CurTick.Value)
            {
                Positions = OpenStream(roundDir / "positions.jsonl.gz"),
                NavMap = OpenStream(roundDir / "navmap.jsonl.gz"),
                Characters = OpenStream(roundDir / "characters.jsonl.gz"),
                Events = OpenStream(roundDir / "events.jsonl.gz"),
                Chat = OpenStream(roundDir / "chat.jsonl.gz"),
                Roster = OpenStream(roundDir / "roster.jsonl.gz"),
            };

            // Written up front so a bundle from a crashed server is still identifiable and readable. Rewritten with
            // the final tick counts and duration on a clean stop.
            WriteMeta(_session, null);

            _sawmill.Info($"Started investigation recording for round {roundId} at {roundDir}");
        }
        catch (Exception e)
        {
            _sawmill.Error($"Failed to start investigation recording: {e}");
            _session = null;
        }
    }

    public void StopRound(TimeSpan? duration)
    {
        if (_session is not { } session)
            return;

        // Null out first: if anything below throws we must not be left holding half-closed streams.
        _session = null;

        try
        {
            FlushSession(session);
            WriteMeta(session, duration);

            session.Positions.Dispose();
            session.NavMap.Dispose();
            session.Characters.Dispose();
            session.Events.Dispose();
            session.Chat.Dispose();
            session.Roster.Dispose();

            LastBundlePath = session.Directory;

            _sawmill.Info(
                $"Stopped investigation recording for round {session.RoundId}. " +
                $"{session.RowCount} rows across {Roster.Count} tracked entities.");
        }
        catch (Exception e)
        {
            _sawmill.Error($"Failed to finalize investigation recording: {e}");
        }
        finally
        {
            Roster.Clear();
            _positions.Clear();
            _loadoutHashes.Clear();
        }
    }

    private JsonlStream OpenStream(ResPath path)
    {
        var file = _res.UserData.Open(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        var gzip = new GZipStream(file, CompressionLevel.Optimal);
        return new JsonlStream(new StreamWriter(gzip, Encoding.UTF8));
    }

    private void WriteMeta(Session session, TimeSpan? duration)
    {
        var meta = new
        {
            schema = SchemaVersion,
            roundId = session.RoundId,
            map = session.Map,
            serverName = _cfg.GetCVar(CCVars.AdminLogsServerName),
            startedUtc = session.StartedUtc.ToString("O", CultureInfo.InvariantCulture),
            startTick = session.StartTick,
            endTick = _timing.CurTick.Value,
            tickRate = _timing.TickRate,
            durationSeconds = duration?.TotalSeconds,
            roster = Roster.Select(kv => new
            {
                e = kv.Key.Id,
                player = kv.Value.PlayerGuid?.ToString(),
                userName = kv.Value.UserName,
                name = kv.Value.Name,
                prototype = kv.Value.Prototype,
                firstTick = kv.Value.FirstTick,
            }),
        };

        using var file = _res.UserData.Open(session.Directory / "meta.json", FileMode.Create, FileAccess.Write, FileShare.Read);
        using var writer = new StreamWriter(file, Encoding.UTF8);
        writer.Write(JsonSerializer.Serialize(meta, JsonOptions));
    }

    #endregion

    #region Roster

    /// <summary>
    ///     Registers an entity as tracked. Once registered it stays tracked for the rest of the round even after the
    ///     player leaves it, because a corpse being moved around is exactly what investigations care about.
    /// </summary>
    public void TrackEntity(EntityUid uid, Guid? playerGuid, string? userName, string name, string? prototype)
    {
        if (_session is not { } session || Roster.ContainsKey(uid))
            return;

        var entry = new RosterEntry(playerGuid, userName, name, prototype, _timing.CurTick.Value);
        Roster[uid] = entry;

        // Also appended to its own stream, so the roster survives a crash. meta.json is only complete on a clean
        // stop; this stream is flushed on the normal interval like everything else.
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

    public void UntrackEntity(EntityUid uid)
    {
        // Deliberately keeps the roster entry: it is the record of who this entity was. Only the live caches go.
        _positions.Remove(uid);
        _loadoutHashes.Remove(uid);
    }

    #endregion

    #region Row writers

    /// <summary>
    ///     Records a position sample. Returns true if a row was actually written.
    /// </summary>
    public bool WritePosition(EntityUid uid, EntityUid? grid, Vector2 local, EntityUid? container, float epsilon)
    {
        if (_session is not { } session)
            return false;

        var sample = new SampledPosition(grid, local, container);

        if (_positions.TryGetValue(uid, out var previous) && !previous.DiffersFrom(sample, epsilon))
            return false;

        _positions[uid] = sample;

        _sb.Clear();
        _sb.Append("{\"t\":").Append(_timing.CurTick.Value)
            .Append(",\"e\":").Append(uid.Id)
            .Append(",\"g\":");

        if (grid is { } gridUid)
            _sb.Append(gridUid.Id);
        else
            _sb.Append("null");

        _sb.Append(",\"x\":").Append(local.X.ToString("0.##", CultureInfo.InvariantCulture))
            .Append(",\"y\":").Append(local.Y.ToString("0.##", CultureInfo.InvariantCulture));

        if (container is { } containerUid)
            _sb.Append(",\"c\":").Append(containerUid.Id);

        _sb.Append('}');

        session.Positions.Write(_sb.ToString());
        session.RowCount++;
        return true;
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

    public void WriteNavMapBeacons(EntityUid grid, IEnumerable<object> beacons)
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

    /// <summary>
    ///     Writes a character loadout snapshot, but only if it differs from the last one written for this entity.
    /// </summary>
    public void WriteCharacterIfChanged(EntityUid uid, object snapshot, int fingerprint)
    {
        if (_session is not { } session)
            return;

        if (_loadoutHashes.TryGetValue(uid, out var previous) && previous == fingerprint)
            return;

        _loadoutHashes[uid] = fingerprint;
        session.Characters.Write(JsonSerializer.Serialize(snapshot, JsonOptions));
        session.RowCount++;
    }

    #endregion

    #region Chat hook

    public void OnChat(EntityUid? source, string channel, string text, string? speakerName)
    {
        if (_session is not { } session || string.IsNullOrWhiteSpace(text))
            return;

        SampledPosition position = default;
        var located = source is { } src && _positions.TryGetValue(src, out position);

        session.Chat.Write(JsonSerializer.Serialize(new
        {
            t = _timing.CurTick.Value,
            e = source?.Id,
            ch = channel,
            name = speakerName,
            msg = text,
            // Carried inline so the reader can place a speech bubble without joining against the position stream.
            g = located ? position.Grid?.Id : null,
            x = located ? Math.Round(position.Local.X, 2) : (double?) null,
            y = located ? Math.Round(position.Local.Y, 2) : (double?) null,
            c = located ? position.Container?.Id : null,
        }, JsonOptions));
        session.RowCount++;
    }

    #endregion

    #region Admin log hook

    public void OnAdminLog(LogType type, LogImpact impact, string message, Dictionary<string, object?> values)
    {
        if (_session is not { } session)
            return;

        List<object>? entities = null;

        foreach (var (key, value) in values)
        {
            switch (value)
            {
                case EntityStringRepresentation entity:
                {
                    entities ??= new List<object>();

                    // Resolve against the live position cache. This is why the join happens here rather than in the
                    // reader: at this exact moment we have the entity's sampled position for free.
                    _positions.TryGetValue(entity.Uid, out var pos);
                    var known = _positions.ContainsKey(entity.Uid);

                    entities.Add(new
                    {
                        role = key,
                        e = entity.Uid.Id,
                        name = entity.Name,
                        prototype = entity.Prototype,
                        player = entity.Session?.UserId.UserId.ToString(),
                        g = known ? pos.Grid?.Id : null,
                        x = known ? Math.Round(pos.Local.X, 2) : (double?) null,
                        y = known ? Math.Round(pos.Local.Y, 2) : (double?) null,
                        c = known ? pos.Container?.Id : null,
                    });
                    break;
                }
                // A small number of call sites pass coordinates directly (e.g. PointingSystem). Those are more
                // precise than our sampled cache, so keep them verbatim alongside the resolved entities.
                case EntityCoordinates coordinates:
                {
                    entities ??= new List<object>();
                    entities.Add(new
                    {
                        role = key,
                        parent = coordinates.EntityId.Id,
                        x = Math.Round(coordinates.X, 2),
                        y = Math.Round(coordinates.Y, 2),
                    });
                    break;
                }
            }
        }

        session.Events.Write(JsonSerializer.Serialize(new
        {
            t = _timing.CurTick.Value,
            utc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            type = type.ToString(),
            impact = impact.ToString(),
            msg = message,
            entities,
        }, JsonOptions));
        session.RowCount++;
    }

    #endregion

    #region Flushing

    /// <summary>
    ///     Driven from the system's update loop. Flushes buffered rows to disk on an interval.
    /// </summary>
    public void Update(float frameTime)
    {
        if (_session is not { } session)
            return;

        _flushAccumulator += frameTime;
        if (_flushAccumulator < _flushInterval)
            return;

        _flushAccumulator = 0f;
        FlushSession(session);
    }

    private void FlushSession(Session session)
    {
        try
        {
            session.Positions.Flush();
            session.NavMap.Flush();
            session.Characters.Flush();
            session.Events.Flush();
            session.Chat.Flush();
            session.Roster.Flush();
        }
        catch (Exception e)
        {
            _sawmill.Error($"Failed to flush investigation recording, stopping: {e}");
            _session = null;
        }
    }

    #endregion

    #region Types

    public readonly record struct RosterEntry(
        Guid? PlayerGuid,
        string? UserName,
        string Name,
        string? Prototype,
        uint FirstTick);

    private readonly record struct SampledPosition(EntityUid? Grid, Vector2 Local, EntityUid? Container)
    {
        /// <summary>
        ///     Grid and container changes are discontinuities and always warrant a row; plain movement only does if it
        ///     exceeded the epsilon.
        /// </summary>
        public bool DiffersFrom(SampledPosition other, float epsilon)
        {
            if (Grid != other.Grid || Container != other.Container)
                return true;

            return (Local - other.Local).LengthSquared() >= epsilon * epsilon;
        }
    }

    private sealed class Session
    {
        public readonly int RoundId;
        public readonly ResPath Directory;
        public readonly string? Map;
        public readonly DateTime StartedUtc;
        public readonly uint StartTick;

        public JsonlStream Positions = default!;
        public JsonlStream NavMap = default!;
        public JsonlStream Characters = default!;
        public JsonlStream Events = default!;
        public JsonlStream Chat = default!;
        public JsonlStream Roster = default!;

        public long RowCount;

        public Session(int roundId, ResPath directory, string? map, DateTime startedUtc, uint startTick)
        {
            RoundId = roundId;
            Directory = directory;
            Map = map;
            StartedUtc = startedUtc;
            StartTick = startTick;
        }
    }

    /// <summary>
    ///     A newline-delimited JSON stream that buffers rows in memory and writes them out on flush.
    /// </summary>
    private sealed class JsonlStream : IDisposable
    {
        private readonly StreamWriter _writer;
        private readonly List<string> _buffer = new();

        public JsonlStream(StreamWriter writer)
        {
            _writer = writer;
        }

        public void Write(string row) => _buffer.Add(row);

        public void Flush()
        {
            if (_buffer.Count == 0)
                return;

            foreach (var row in _buffer)
            {
                _writer.Write(row);
                _writer.Write('\n');
            }

            _buffer.Clear();
            _writer.Flush();
        }

        public void Dispose()
        {
            Flush();
            _writer.Dispose();
        }
    }

    #endregion
}
