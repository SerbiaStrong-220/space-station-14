// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Numerics;
using System.Text;
using System.Text.Json;
using Content.Shared.CCVar;
using Content.Shared.SS220.CCVars;
using Content.Shared.Database;
using Content.Shared.SS220.Language;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.SS220.Investigation;

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

    /// <summary>
    ///     How long round end waits for buffered rows to reach disk before giving up on them.
    /// </summary>
    private static readonly TimeSpan WriterShutdownTimeout = TimeSpan.FromSeconds(15);

    /// <summary>
    ///     Names of the streams in a bundle, in the order they are opened. Also the order
    ///     <see cref="FlushSession"/> and <see cref="CloseSession"/> walk them.
    /// </summary>
    private const string PositionsFile = "positions.jsonl.gz";
    private const string NavMapFile = "navmap.jsonl.gz";
    private const string CharactersFile = "characters.jsonl.gz";
    private const string EventsFile = "events.jsonl.gz";
    private const string ChatFile = "chat.jsonl.gz";
    private const string RosterFile = "roster.jsonl.gz";
    private const string HealthFile = "health.jsonl.gz";
    private const string ControlFile = "control.jsonl.gz";
    private const string ObjectivesFile = "objectives.jsonl.gz";
    private const string MetaFile = "meta.json";

    /// <summary>
    ///     Prefix every bundle directory is named with. Also what retention matches on, so that nothing else that
    ///     happens to share the directory is ever deleted.
    /// </summary>
    private const string BundlePrefix = "round-";

    /// <summary>
    ///     Progress at or above which an objective counts as done. Matches
    ///     <c>SharedObjectivesSystem.IsCompleted</c>, which the round end screen uses, so the bundle and the
    ///     scoreboard never disagree about whether something was achieved.
    /// </summary>
    private const float CompletionThreshold = 0.999f;

    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IResourceManager _res = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ILogManager _logManager = default!;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    ///     UTF-8 without a byte order mark.
    /// </summary>
    /// <remarks>
    ///     <see cref="Encoding.UTF8"/> emits a BOM, which lands at the start of the first line of every
    ///     stream and makes that line invalid JSON for any reader that splits on newlines and parses each
    ///     line. Readers should still tolerate a BOM, because bundles written before this was fixed have
    ///     one, but nothing new should produce it.
    /// </remarks>
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private ISawmill _sawmill = default!;
    private bool _enabled;
    private float _flushInterval;
    private float _flushAccumulator;

    private Session? _session;

    /// <summary>
    ///     Reused to build position rows by hand. Positions are by far the largest stream, and building each row
    ///     through the serializer would allocate an anonymous object and a string per sample.
    /// </summary>
    private readonly StringBuilder _rowBuilder = new();

    private readonly Dictionary<EntityUid, RosterEntry> _roster = new();

    /// <summary>
    ///     Every entity that has ever been player-controlled this round, and is therefore tracked for the rest of it.
    ///     Keyed by <see cref="EntityUid.Id"/>, which is the same value admin logs record, and which is never
    ///     recycled within a round (see <c>EntityManager.GenerateEntityUid</c>).
    /// </summary>
    /// <remarks>
    ///     Read-only to callers on purpose: membership is decided by <see cref="TrackEntity"/>, which also has to
    ///     append a roster row, and an outside write would produce an entity that is sampled but never named.
    /// </remarks>
    public IReadOnlyDictionary<EntityUid, RosterEntry> Roster => _roster;

    /// <summary>
    ///     Latest observed position of each tracked entity, used to enrich admin logs and chat. Always the most
    ///     recent reading, even when that reading was not written as a row.
    /// </summary>
    private readonly Dictionary<EntityUid, SampledPosition> _positions = new();

    /// <summary>
    ///     Dead-reckoning state per tracked entity: what was last written, and what is being held back.
    ///     Kept separate from <see cref="_positions"/> so "where is it" and "what have we emitted" never get
    ///     confused with each other.
    /// </summary>
    private readonly Dictionary<EntityUid, PositionTrack> _tracks = new();

    /// <summary>
    ///     Last written loadout fingerprint per tracked entity, so we only emit a character row when it changes.
    /// </summary>
    private readonly Dictionary<EntityUid, int> _loadoutHashes = new();

    /// <summary>
    ///     Last written health sample per tracked entity, so an unharmed character costs one row per round.
    /// </summary>
    private readonly Dictionary<EntityUid, HealthSample> _healthSamples = new();

    /// <summary>
    ///     Last written progress per objective entity, so an objective nobody is making progress on costs one row.
    /// </summary>
    /// <remarks>
    ///     Keyed on the objective entity rather than on its owner: objectives belong to the mind, and a mind moves
    ///     between bodies. Keying on the body would re-emit every objective from scratch each time somebody was
    ///     cloned or borged.
    /// </remarks>
    private readonly Dictionary<EntityUid, ObjectiveSample> _objectiveSamples = new();

    /// <summary>
    ///     The game preset this round is running, resolved past "secret". See <see cref="SetGamemode"/>.
    /// </summary>
    private string? _gamemode;
    private string? _gamemodeTitle;

    /// <summary>
    ///     Resolves an entity's grid-local position on demand. See <see cref="SetPositionSource"/>.
    /// </summary>
    private IInvestigationPositionSource? _positionSource;

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

    /// <summary>
    ///     Supplies the live position lookup used to place speech from entities that are not on the roster.
    /// </summary>
    /// <remarks>
    ///     The recorder is an IoC manager and the only thing that can walk transforms is an entity system, so the
    ///     dependency has to point this way round. <see cref="InvestigationRecorderSystem"/> calls this once from
    ///     its own Initialize.
    ///
    ///     It exists because plenty of speech comes from entities that were never player-controlled and therefore
    ///     have no sampled position: mice, bots, PAIs, announcement consoles. Without a live lookup those lines
    ///     carry no coordinates and a reader cannot place a speech bubble for them.
    /// </remarks>
    public void SetPositionSource(IInvestigationPositionSource source)
    {
        _positionSource = source;
    }

    /// <summary>
    ///     Records which game preset the round is running under.
    /// </summary>
    /// <remarks>
    ///     Set at round start and again at round end, because "secret" resolves to a real preset and an admin can
    ///     change it mid-round. meta.json is written at both points, so the finished bundle always carries the
    ///     preset the round actually ran, not the one it was scheduled with.
    ///
    ///     Without this the antag list is unreadable: "three traitors and a nuke op" means one thing in Nuclear
    ///     Emergency and something else entirely in Secret.
    /// </remarks>
    public void SetGamemode(string? id, string? title)
    {
        _gamemode = id;
        _gamemodeTitle = title;
    }

    public void Initialize()
    {
        _sawmill = _logManager.GetSawmill("investigation");
        _cfg.OnValueChanged(CCVars220.InvestigationEnabled, enabled => _enabled = enabled, true);
        _cfg.OnValueChanged(CCVars220.InvestigationFlushInterval, interval => _flushInterval = interval, true);
    }

    public void Shutdown()
    {
        if (_session != null)
            StopRound(null);
    }

    #region Session lifecycle

    public void StartRound(int roundId, string? map)
    {
        if (!_enabled)
            return;

        if (_session is { } running)
        {
            // A round always stops before the next one starts, so reaching this means a lifecycle hook was
            // missed. Keep the running session rather than silently folding two rounds into one bundle, and
            // say so, because the alternative is a bundle nobody can trust.
            _sawmill.Warning(
                $"Round {roundId} started while round {running.RoundId} was still recording. Ignoring the new round.");
            return;
        }

        StreamWriterThread? writerThread = null;
        var openedStreams = new List<JsonlStream>();

        try
        {
            var baseDir = new ResPath(_cfg.GetCVar(CCVars220.InvestigationDirectory)).ToRootedPath();
            var stamp = DateTime.UtcNow.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture);
            var roundDir = baseDir / $"round-{roundId}_{stamp}";

            _res.UserData.CreateDir(roundDir);

            // Before opening anything, so a disk that is already full gets a chance to free space first.
            PruneOldBundles(baseDir);

            var writer = new StreamWriterThread(_sawmill);
            writerThread = writer;

            JsonlStream Open(string fileName)
            {
                var stream = OpenStream(writer, roundDir / fileName);
                openedStreams.Add(stream);
                return stream;
            }

            var session = new Session(roundId, roundDir, map, DateTime.UtcNow, _timing.CurTick.Value, writer)
            {
                Positions = Open(PositionsFile),
                NavMap = Open(NavMapFile),
                Characters = Open(CharactersFile),
                Events = Open(EventsFile),
                Chat = Open(ChatFile),
                Roster = Open(RosterFile),
                Health = Open(HealthFile),
                Control = Open(ControlFile),
                Objectives = Open(ObjectivesFile),
            };

            session.AllStreams = openedStreams.ToArray();

            // Written up front so a bundle from a crashed server is still identifiable and readable. Rewritten with
            // the final tick counts and duration on a clean stop.
            WriteMeta(session, null);

            _session = session;

            _sawmill.Info($"Started investigation recording for round {roundId} at {roundDir}");
        }
        catch (Exception e)
        {
            // Whatever was opened before the failure still holds a file handle, and the writer thread is already
            // running and parked on its queue. Without this a start that keeps failing — a full disk, a bad
            // directory cvar — leaks one thread and a handful of handles every round.
            _sawmill.Error($"Failed to start investigation recording: {e}");
            _session = null;

            foreach (var stream in openedStreams)
            {
                stream.Close();
            }

            writerThread?.Stop(WriterShutdownTimeout);
        }
    }

    public void StopRound(TimeSpan? duration)
    {
        if (_session is not { } session)
            return;

        // Held-back samples exist only in memory, and flushing them needs a live session to write into, so this
        // runs before the session is torn down. It touches no I/O of its own — it only appends rows to buffers.
        FlushPendingPositions();

        // Null out before the rest: if anything below throws we must not be left holding half-closed streams.
        _session = null;

        try
        {
            FlushSession(session);
            WriteMeta(session, duration);
            CloseSession(session);

            LastBundlePath = session.Directory;

            _sawmill.Info(
                $"Stopped investigation recording for round {session.RoundId}. " +
                $"{session.RowCount} rows across {_roster.Count} tracked entities.");
        }
        catch (Exception e)
        {
            _sawmill.Error($"Failed to finalize investigation recording: {e}");
        }
        finally
        {
            _roster.Clear();
            _positions.Clear();
            _tracks.Clear();
            _loadoutHashes.Clear();
            _healthSamples.Clear();
            _objectiveSamples.Clear();
        }
    }

    /// <summary>
    ///     Deletes bundles older than <c>investigation.retention_days</c>.
    /// </summary>
    /// <remarks>
    ///     Age comes from the timestamp in the directory name rather than from the filesystem, because a restore,
    ///     a copy or an rsync rewrites modification times and would make retention delete a fresh archive or keep
    ///     an ancient one. A directory whose name does not parse is left alone: it was not written by this
    ///     recorder, and this method deletes recursively.
    /// </remarks>
    private void PruneOldBundles(ResPath baseDir)
    {
        var retentionDays = _cfg.GetCVar(CCVars220.InvestigationRetentionDays);
        if (retentionDays <= 0)
            return;

        var cutoff = DateTime.UtcNow - TimeSpan.FromDays(retentionDays);
        var deleted = 0;

        try
        {
            foreach (var entry in _res.UserData.DirectoryEntries(baseDir))
            {
                if (!entry.StartsWith(BundlePrefix, StringComparison.Ordinal))
                    continue;

                var directory = baseDir / entry;
                if (!_res.UserData.IsDir(directory))
                    continue;

                // "round-{id}_{yyyy-MM-dd_HH-mm-ss}". Only the part after the first underscore is the timestamp;
                // the round id is a plain integer and never contains one.
                var separator = entry.IndexOf('_');
                if (separator < 0)
                    continue;

                if (!DateTime.TryParseExact(
                        entry[(separator + 1)..],
                        "yyyy-MM-dd_HH-mm-ss",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                        out var written))
                {
                    continue;
                }

                if (written >= cutoff)
                    continue;

                _res.UserData.Delete(directory);
                deleted++;
            }
        }
        catch (Exception e)
        {
            // Never fatal: failing to free space is a much smaller problem than failing to record the round.
            _sawmill.Error($"Failed to prune old investigation bundles: {e}");
        }

        if (deleted > 0)
            _sawmill.Info($"Deleted {deleted} investigation bundles older than {retentionDays} days.");
    }

    /// <remarks>
    ///     <see cref="CompressionLevel.Fastest"/> rather than Optimal: measured on real bundles the ratio
    ///     difference is around a tenth, on streams that are already single-digit megabytes, while the CPU cost
    ///     is several times lower. Cheap compression that never stalls beats tight compression that might.
    /// </remarks>
    private JsonlStream OpenStream(StreamWriterThread thread, ResPath path)
    {
        var file = _res.UserData.Open(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        var gzip = new GZipStream(file, CompressionLevel.Fastest);
        return new JsonlStream(thread, new StreamWriter(gzip, Utf8NoBom));
    }

    private void WriteMeta(Session session, TimeSpan? duration)
    {
        var meta = new
        {
            schema = SchemaVersion,
            roundId = session.RoundId,
            map = session.Map,
            gamemode = _gamemode,
            gamemodeTitle = _gamemodeTitle,
            serverName = _cfg.GetCVar(CCVars.AdminLogsServerName),
            startedUtc = session.StartedUtc.ToString("O", CultureInfo.InvariantCulture),
            startTick = session.StartTick,
            endTick = _timing.CurTick.Value,
            tickRate = _timing.TickRate,
            durationSeconds = duration?.TotalSeconds,
            // Chat text keeps its `%key` language prefixes, which are meaningless without the key table that
            // was loaded when the round ran. Writing it into the bundle means a reader can split a mixed
            // sentence into per-language runs without shipping — and then failing to update — its own copy of
            // the prototypes. Cheap: this is one array, once, not a per-row cost.
            languages = _prototype.EnumeratePrototypes<LanguagePrototype>()
                .OrderBy(language => language.ID)
                .Select(language => new
                {
                    id = language.ID,
                    key = language.KeyWithPrefix,
                    name = Loc.GetString(language.Name),
                    color = language.Color?.ToHex(),
                }),
            roster = _roster.Select(entry => new
            {
                e = entry.Key.Id,
                player = entry.Value.PlayerGuid?.ToString(),
                userName = entry.Value.UserName,
                name = entry.Value.Name,
                prototype = entry.Value.Prototype,
                firstTick = entry.Value.FirstTick,
            }),
        };

        using var file = _res.UserData.Open(session.Directory / MetaFile, FileMode.Create, FileAccess.Write, FileShare.Read);
        using var writer = new StreamWriter(file, Utf8NoBom);
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
        if (_session is not { } session || _roster.ContainsKey(uid))
            return;

        var entry = new RosterEntry(playerGuid, userName, name, prototype, _timing.CurTick.Value);
        _roster[uid] = entry;

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

    /// <summary>
    ///     Records a player taking control of an entity, or letting go of it.
    /// </summary>
    /// <remarks>
    ///     The roster answers "who was this entity", once, at first attachment. It cannot answer "who was driving
    ///     it at 14:32", and that is a different question with a different answer every time a body changes hands:
    ///     cloning, borging, revival by somebody else, an admin taking possession, or simply a player
    ///     disconnecting and another spawning into the same shell. Attributing a whole round of a body's actions
    ///     to whoever happened to hold it first is exactly the kind of mistake an investigation tool must not
    ///     make, so every transfer gets a row.
    /// </remarks>
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

    public void UntrackEntity(EntityUid uid)
    {
        // The entity is gone, so no later sample will ever arrive to justify the one being held back. Write it
        // now or its last known position is lost — which for a body that was just destroyed is the one position
        // an investigation actually wants.
        if (_tracks.TryGetValue(uid, out var track))
        {
            FlushPending(uid, ref track);
            _tracks.Remove(uid);
        }

        // Deliberately keeps the roster entry: it is the record of who this entity was. Only the live caches go.
        _positions.Remove(uid);
        _loadoutHashes.Remove(uid);
        _healthSamples.Remove(uid);
    }

    #endregion

    #region Row writers

    /// <summary>
    ///     Records a position sample, emitting a row only when the reader could not reconstruct it.
    /// </summary>
    /// <remarks>
    ///     Two filters, in order.
    ///
    ///     The first is the epsilon: an entity that has not moved produces no rows at all. That already covers
    ///     most of a round, since people stand still a lot.
    ///
    ///     The second is dead reckoning, and it is what makes walking cheap. Movement in SS14 is mostly straight
    ///     lines down corridors, and every sample along a straight line is implied by its endpoints. So one
    ///     sample is held back rather than written immediately, and when the next one arrives we ask whether the
    ///     held sample lands on the line between the last row we emitted and the new one. If it does, within
    ///     <paramref name="epsilon"/>, it is dropped — the reader will interpolate it back. Only genuine
    ///     direction changes become rows, which turns a corridor walk into two rows instead of forty.
    ///
    ///     The cost is that samples are written one behind, and that readers must interpolate rather than step.
    ///     Grid and container changes are discontinuities: they are never interpolated across, and always flush.
    /// </remarks>
    public bool WritePosition(EntityUid uid, EntityUid? grid, Vector2 local, EntityUid? container, float epsilon)
    {
        if (_session is null)
            return false;

        var tick = _timing.CurTick.Value;
        var observed = new SampledPosition(grid, local, container);

        // Always the latest reading, regardless of what gets written: this is what admin logs and chat join
        // against, and a forensic timestamp should not be one sample stale to save disk.
        _positions[uid] = observed;

        if (!_tracks.TryGetValue(uid, out var track))
        {
            // First sight of this entity: emit it, so every entity has an anchor to interpolate from.
            EmitPosition(uid, tick, observed);
            _tracks[uid] = new PositionTrack(tick, observed, null, default, tick);
            return true;
        }

        // A grid or container change is a teleport as far as the reader is concerned. Never bridge one.
        if (observed.IsDiscontinuousFrom(track.WrittenSample))
        {
            FlushPending(uid, ref track);
            EmitPosition(uid, tick, observed);
            _tracks[uid] = new PositionTrack(tick, observed, null, default, tick);
            return true;
        }

        if (track.PendingTick is not { } pendingTick)
        {
            // Nothing held back. Hold this one only if the entity has actually moved.
            if (!observed.DiffersFrom(track.WrittenSample, epsilon))
            {
                // Standing still still costs no rows, but the tick has to be remembered: it is where the
                // stationary stretch ends, and the anchor below is written at it.
                _tracks[uid] = track with { LastSampleTick = tick };
                return false;
            }

            // Movement resuming after a stationary stretch needs a row closing that stretch, or the reader
            // interpolates from the last row it has — which may be minutes old — straight to the next one.
            // Somebody who stood at their locker for ten minutes and then walked off would render as
            // drifting slowly across the room for the whole ten, which is exactly the kind of thing an
            // investigation would read as suspicious. Re-emit the written position at the last tick it was
            // still observed at, so the flat stretch is explicit.
            var anchored = false;
            if (track.LastSampleTick > track.WrittenTick)
            {
                EmitPosition(uid, track.LastSampleTick, track.WrittenSample);
                track = track with { WrittenTick = track.LastSampleTick };
                anchored = true;
            }

            _tracks[uid] = track with { PendingTick = tick, PendingSample = observed, LastSampleTick = tick };
            return anchored;
        }

        // Would the reader land within epsilon of the held sample by interpolating straight past it?
        var span = tick - track.WrittenTick;
        var fraction = span == 0 ? 0f : (pendingTick - track.WrittenTick) / (float) span;
        var predicted = Vector2.Lerp(track.WrittenSample.Local, observed.Local, fraction);

        if ((track.PendingSample.Local - predicted).LengthSquared() <= epsilon * epsilon)
        {
            // Redundant: the line from the last row to this one already passes through it.
            _tracks[uid] = track with { PendingTick = tick, PendingSample = observed, LastSampleTick = tick };
            return false;
        }

        // The path bends here, so the held sample has to become a row.
        EmitPosition(uid, pendingTick, track.PendingSample);
        _tracks[uid] = new PositionTrack(pendingTick, track.PendingSample, tick, observed, tick);
        return true;
    }

    /// <summary>
    ///     Writes out any sample being held back for an entity.
    /// </summary>
    /// <remarks>
    ///     Held samples are speculative: they are only dropped once a later sample proves them redundant. If no
    ///     later sample ever arrives — round end, or the entity being destroyed — the held sample is the entity's
    ///     last known position and must not be lost.
    /// </remarks>
    private void FlushPending(EntityUid uid, ref PositionTrack track)
    {
        if (track.PendingTick is not { } pendingTick)
            return;

        EmitPosition(uid, pendingTick, track.PendingSample);
        track = new PositionTrack(pendingTick, track.PendingSample, null, default, track.LastSampleTick);
    }

    /// <summary>
    ///     Flushes every held position sample. Called at round end so no entity's last movement is dropped.
    /// </summary>
    public void FlushPendingPositions()
    {
        if (_session is null)
            return;

        // Materialised because flushing mutates the dictionary's values.
        foreach (var uid in _tracks.Keys.ToArray())
        {
            var track = _tracks[uid];
            FlushPending(uid, ref track);
            _tracks[uid] = track;
        }
    }

    /// <remarks>
    ///     One decimal place, not two. The epsilon below which movement is not recorded at all is 0.15 tiles, so
    ///     a second decimal encodes nothing but sampling jitter — it costs bytes on every row of the largest
    ///     stream to record noise the recorder has already decided to ignore.
    /// </remarks>
    private void EmitPosition(EntityUid uid, uint tick, SampledPosition sample)
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

        _rowBuilder.Append('}');

        session.Positions.Write(_rowBuilder.ToString());
        session.RowCount++;
    }

    /// <summary>
    ///     Formats one coordinate as a JSON number.
    /// </summary>
    /// <remarks>
    ///     A non-finite coordinate would format as <c>NaN</c> or <c>Infinity</c>, neither of which is valid JSON,
    ///     and one such row makes the line it lands on unparseable for every reader. Physics can produce them
    ///     after a bad teleport or a detached transform, so they are clamped to zero rather than written out.
    /// </remarks>
    private static string FormatCoordinate(float value)
    {
        if (!float.IsFinite(value))
            return "0";

        return value.ToString("0.#", CultureInfo.InvariantCulture);
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
    ///     Records a health sample, but only when it differs from the last one written for this entity.
    /// </summary>
    /// <remarks>
    ///     Polled from the position loop rather than hooked onto DamageChangedEvent. Continuous damage
    ///     sources — suffocation, fire, bleeding, pressure — raise that event on their own tick cadence, so
    ///     a hull breach with a dozen casualties would produce an unbounded stream. Polling is bounded by
    ///     the sample rate, and it also gives every healthy character a baseline row, which an event-driven
    ///     stream never would.
    /// </remarks>
    public void WriteHealthIfChanged(
        EntityUid uid,
        float damage,
        string state,
        float? critThreshold,
        float? deadThreshold)
    {
        if (_session is not { } session)
            return;

        // Compared by value rather than by hash: a 32-bit hash collision here would silently drop a row, and
        // the row it drops is as likely as not the one recording somebody dying.
        var sample = new HealthSample(Math.Round(damage, 2), state);
        if (_healthSamples.TryGetValue(uid, out var previous) && previous == sample)
            return;

        _healthSamples[uid] = sample;

        session.Health.Write(JsonSerializer.Serialize(new
        {
            t = _timing.CurTick.Value,
            e = uid.Id,
            dmg = sample.Damage,
            state,
            crit = critThreshold.HasValue ? Math.Round(critThreshold.Value, 2) : (double?) null,
            dead = deadThreshold.HasValue ? Math.Round(deadThreshold.Value, 2) : (double?) null,
        }, JsonOptions));
        session.RowCount++;
    }

    /// <summary>
    ///     Records an objective's progress, but only when it moved since the last row.
    /// </summary>
    /// <remarks>
    ///     The point of this stream is the tick, not the final tally. Whether an antagonist completed their
    ///     objectives is already on the round end screen; <em>when</em> they completed them is not recorded
    ///     anywhere, and it is what turns "they killed the HoS" into "they killed the HoS twenty minutes before
    ///     the shuttle, having had the objective since round start".
    ///
    ///     Progress is rounded to two decimals before comparison, so an objective that inches forward — a steal
    ///     objective counting items, a kill objective tracking a crit — produces a row per real step rather than
    ///     one per float wobble.
    /// </remarks>
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
            // The objective entity, stable for the round, so a reader can follow one objective over time.
            o = objective.Id,
            // The body whose mind holds it. Follows the mind, so this changes if the player is cloned or borged.
            e = owner.Id,
            proto = prototype,
            title,
            desc = description,
            progress = rounded,
            done = complete,
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

    #region Foreign call sites

    /// <summary>
    ///     Runs one of the hooks that foreign code calls into, swallowing anything it throws.
    /// </summary>
    /// <remarks>
    ///     <see cref="OnChat"/> and <see cref="OnAdminLog"/> run synchronously inside <c>ChatSystem</c> and
    ///     <c>AdminLogManager.Add</c>, and <see cref="OnAnyCommandExecuted"/> inside the console host. An
    ///     exception escaping any of them takes down chat, admin logging, or the console for the rest of the
    ///     round, which is a catastrophic price for a nice-to-have forensic bundle. So the recorder fails alone:
    ///     it logs once and stops recording, and the game carries on without it.
    /// </remarks>
    private void Guarded(Action write)
    {
        try
        {
            write();
        }
        catch (Exception e)
        {
            _sawmill.Error($"Investigation recording threw and has been stopped for this round: {e}");

            // Deliberately not left running: whatever produced the exception will almost certainly produce it
            // again on the next row, and a hook that throws every time is worse than no hook at all.
            try
            {
                StopRound(null);
            }
            catch (Exception stopError)
            {
                _sawmill.Error($"Investigation recording also failed to stop cleanly: {stopError}");
                _session = null;
            }
        }
    }

    public void OnChat(
        EntityUid? source,
        string channel,
        string text,
        string? speakerName,
        string? defaultLanguage = null,
        IReadOnlyList<string>? languages = null,
        string? radioChannel = null)
    {
        if (_session is null || string.IsNullOrWhiteSpace(text))
            return;

        Guarded(() => WriteChat(source, channel, text, speakerName, defaultLanguage, languages, radioChannel));
    }

    private void WriteChat(
        EntityUid? source,
        string channel,
        string text,
        string? speakerName,
        string? defaultLanguage,
        IReadOnlyList<string>? languages,
        string? radioChannel)
    {
        if (_session is not { } session)
            return;

        SampledPosition position = default;
        var located = false;

        if (source is { } speaker)
        {
            located = _positions.TryGetValue(speaker, out position);

            // Untracked speaker: resolve it live rather than dropping the coordinates.
            if (!located
                && _positionSource is { } positionSource
                && positionSource.TryGetPosition(speaker, out var grid, out var local, out var container))
            {
                position = new SampledPosition(grid, local, container);
                located = true;
            }
        }

        session.Chat.Write(JsonSerializer.Serialize(new
        {
            t = _timing.CurTick.Value,
            e = source?.Id,
            ch = channel,
            name = speakerName,
            msg = text,
            // `lang` is the speaker's selected language: what `msg` is in wherever it carries no `%key` prefix.
            // `langs` lists every language actually used and only appears on the rare line that mixed two or
            // more, so the common case stays one cheap scalar. `msg` keeps its prefixes either way, which is
            // what lets a reader colour a sentence run by run rather than as a single language.
            lang = defaultLanguage ?? (languages is { Count: > 0 } ? languages[0] : null),
            langs = languages is { Count: > 1 } ? languages : null,
            rc = radioChannel,
            // Carried inline so the reader can place a speech bubble without joining against the position stream.
            g = located ? position.Grid?.Id : null,
            x = located ? Math.Round(position.Local.X, 2) : (double?) null,
            y = located ? Math.Round(position.Local.Y, 2) : (double?) null,
            c = located ? position.Container?.Id : null,
        }, JsonOptions));
        session.RowCount++;
    }

    public void OnAdminLog(LogType type, LogImpact impact, string message, Dictionary<string, object?> values)
    {
        if (_session is null)
            return;

        Guarded(() => WriteAdminLog(type, impact, message, values));
    }

    private void WriteAdminLog(LogType type, LogImpact impact, string message, Dictionary<string, object?> values)
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
                    var known = _positions.TryGetValue(entity.Uid, out var position);

                    entities.Add(new
                    {
                        role = key,
                        e = entity.Uid.Id,
                        name = entity.Name,
                        prototype = entity.Prototype,
                        player = entity.Session?.UserId.UserId.ToString(),
                        g = known ? position.Grid?.Id : null,
                        x = known ? Math.Round(position.Local.X, 2) : (double?) null,
                        y = known ? Math.Round(position.Local.Y, 2) : (double?) null,
                        c = known ? position.Container?.Id : null,
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

    /// <summary>
    ///     Hands every stream's buffered rows to the writer thread.
    /// </summary>
    /// <remarks>
    ///     Cheap by construction: each stream swaps a list reference and enqueues it. A write that actually fails
    ///     does so on the writer thread and is surfaced here on the next flush, so teardown always happens on the
    ///     game thread.
    /// </remarks>
    private void FlushSession(Session session)
    {
        foreach (var stream in session.AllStreams)
        {
            stream.Flush();
        }

        if (session.Writer.Failure is null)
            return;

        // The bundle is already damaged, so stop rather than keep appending to streams that may be dead. Teardown
        // is the same path as a normal stop: the streams still get closed, so whatever did reach disk stays
        // readable rather than being left without a gzip trailer.
        _session = null;
        CloseSession(session);
    }

    /// <summary>
    ///     Closes every stream and shuts the writer thread down, reporting anything that went wrong.
    /// </summary>
    private void CloseSession(Session session)
    {
        // A failed flush tears the session down mid-stop, and then StopRound reaches its own CloseSession with
        // the same session. Closing twice would push a second round of disposes at a queue that has already
        // been completed for adding.
        if (session.Closed)
            return;

        session.Closed = true;

        foreach (var stream in session.AllStreams)
        {
            stream.Close();
        }

        // Waits for the queued closes so the gzip trailers land before the bundle is treated as complete.
        session.Writer.Stop(WriterShutdownTimeout);

        if (session.Writer.Failure is { } failure)
            _sawmill.Error($"Investigation writing failed; bundle may be incomplete: {failure}");

        if (session.Writer.DroppedBatches > 0)
            _sawmill.Warning($"Dropped {session.Writer.DroppedBatches} investigation batches: disk could not keep up.");
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
            if (IsDiscontinuousFrom(other))
                return true;

            return (Local - other.Local).LengthSquared() >= epsilon * epsilon;
        }

        /// <summary>
        ///     Whether moving between these two samples is a teleport rather than travel. Interpolating across one
        ///     draws a path through walls, so the recorder never elides samples across a discontinuity.
        /// </summary>
        public bool IsDiscontinuousFrom(SampledPosition other) => Grid != other.Grid || Container != other.Container;
    }

    /// <summary>
    ///     Per-entity dead-reckoning state: the last row actually emitted, plus at most one sample held back
    ///     pending the decision to write or drop it. See <see cref="WritePosition"/>.
    /// </summary>
    /// <param name="LastSampleTick">
    ///     Tick of the most recent sample, written or not. Only used to date the anchor row that closes a
    ///     stationary stretch; see <see cref="WritePosition"/>.
    /// </param>
    private readonly record struct PositionTrack(
        uint WrittenTick,
        SampledPosition WrittenSample,
        uint? PendingTick,
        SampledPosition PendingSample,
        uint LastSampleTick);

    /// <summary>
    ///     What the health stream deduplicates on. Thresholds are effectively static per entity, so damage and
    ///     mob state are the whole of what can change.
    /// </summary>
    private readonly record struct HealthSample(double Damage, string State);

    /// <summary>
    ///     What the objectives stream deduplicates on.
    /// </summary>
    /// <remarks>
    ///     Completion is carried separately from progress rather than derived on read, so that the row where an
    ///     objective flips to done is always emitted even if the rounded progress did not visibly change.
    /// </remarks>
    private readonly record struct ObjectiveSample(double Progress, bool Complete);

    private sealed class Session
    {
        public readonly int RoundId;
        public readonly ResPath Directory;
        public readonly string? Map;
        public readonly DateTime StartedUtc;
        public readonly uint StartTick;
        public readonly StreamWriterThread Writer;

        public JsonlStream Positions = default!;
        public JsonlStream NavMap = default!;
        public JsonlStream Characters = default!;
        public JsonlStream Events = default!;
        public JsonlStream Chat = default!;
        public JsonlStream Roster = default!;
        public JsonlStream Health = default!;
        public JsonlStream Control = default!;
        public JsonlStream Objectives = default!;

        /// <summary>
        ///     Every stream, for the operations that treat them uniformly. Rebuilding this per flush would
        ///     allocate; it is set once, right after the streams are opened.
        /// </summary>
        public JsonlStream[] AllStreams = default!;

        public long RowCount;

        /// <summary>
        ///     Set once the streams have been handed to the writer thread for closing, so a second attempt is a
        ///     no-op rather than an enqueue against a completed queue.
        /// </summary>
        public bool Closed;

        public Session(
            int roundId,
            ResPath directory,
            string? map,
            DateTime startedUtc,
            uint startTick,
            StreamWriterThread writer)
        {
            RoundId = roundId;
            Directory = directory;
            Map = map;
            StartedUtc = startedUtc;
            StartTick = startTick;
            Writer = writer;
        }
    }

    /// <summary>
    ///     A newline-delimited JSON stream that buffers rows on the game thread and hands whole batches to a
    ///     background <see cref="StreamWriterThread"/> to be compressed and written.
    /// </summary>
    /// <remarks>
    ///     The game thread only ever appends to a list and, on flush, swaps that list for an empty one. Deflate
    ///     and the file write — the genuinely expensive parts — happen on the writer thread, so recording adds no
    ///     periodic hitch to the tick regardless of player count.
    /// </remarks>
    private sealed class JsonlStream
    {
        private readonly StreamWriterThread _thread;
        private readonly StreamWriter _writer;
        private List<string> _buffer = new();

        public JsonlStream(StreamWriterThread thread, StreamWriter writer)
        {
            _thread = thread;
            _writer = writer;
        }

        public void Write(string row) => _buffer.Add(row);

        /// <summary>
        ///     Hands the buffered rows to the writer thread. Returns false if the queue was full and the batch was
        ///     dropped, which the caller reports; the rows are gone either way, so the buffer is always reset.
        /// </summary>
        public bool Flush()
        {
            if (_buffer.Count == 0)
                return true;

            var batch = _buffer;
            _buffer = new List<string>();
            return _thread.Enqueue(_writer, batch);
        }

        /// <summary>
        ///     Flushes and closes the underlying file. Called on the writer thread during shutdown, never from the
        ///     game thread, so it cannot block a tick on a slow disk.
        /// </summary>
        public void Close() => _thread.EnqueueClose(_writer);
    }

    /// <summary>
    ///     Single background thread that owns all disk I/O for a recording session.
    /// </summary>
    /// <remarks>
    ///     Deliberately one thread for every stream rather than one per stream: the streams are written in the
    ///     same order they are produced, there is no contention worth parallelising, and one thread is one
    ///     lifetime to get right. The queue is bounded — if the disk cannot keep up, batches are dropped and
    ///     counted rather than growing the heap until the server dies. Losing rows from a forensic bundle is
    ///     bad; taking the game server down with it is worse.
    /// </remarks>
    private sealed class StreamWriterThread
    {
        /// <summary>
        ///     Roughly a minute of backlog for a busy server at the default flush interval. Reaching this means
        ///     the disk is badly unhealthy, not that the server is busy.
        /// </summary>
        private const int QueueCapacity = 64;

        /// <summary>
        ///     How long a stream close may wait for room in the queue before it is given up on.
        /// </summary>
        private static readonly TimeSpan CloseEnqueueTimeout = TimeSpan.FromSeconds(2);

        private readonly BlockingCollection<Action> _queue = new(QueueCapacity);
        private readonly Thread _thread;
        private readonly ISawmill _sawmill;

        private int _droppedBatches;

        /// <summary>
        ///     Set by the writer thread when a write throws, read by the game thread. The session is torn down on
        ///     the next tick rather than from the writer thread, so state is only ever mutated on one thread.
        /// </summary>
        private volatile string? _failure;

        public StreamWriterThread(ISawmill sawmill)
        {
            _sawmill = sawmill;
            _thread = new Thread(Run)
            {
                Name = "InvestigationWriter",
                IsBackground = true,
                // Recording must never compete with the simulation for CPU.
                Priority = ThreadPriority.BelowNormal,
            };
            _thread.Start();
        }

        public string? Failure => _failure;

        public int DroppedBatches => _droppedBatches;

        public bool Enqueue(StreamWriter writer, List<string> batch)
        {
            return Post(() =>
            {
                foreach (var row in batch)
                {
                    writer.Write(row);
                    writer.Write('\n');
                }

                writer.Flush();
            });
        }

        public void EnqueueClose(StreamWriter writer)
        {
            // Not routed through Post, because a close dropped on the first full queue costs the gzip trailer and
            // leaves a file readers have to salvage. It still gives up eventually: a plain blocking Add would park
            // the game thread on a hung disk for as long as the disk stays hung, which is exactly what Stop()'s
            // timeout exists to prevent.
            if (_queue.IsAddingCompleted || !_queue.TryAdd(() => writer.Dispose(), CloseEnqueueTimeout))
                _sawmill.Error("Could not queue an investigation stream close; that file will have no gzip trailer.");
        }

        private bool Post(Action work)
        {
            if (_queue.IsAddingCompleted || !_queue.TryAdd(work))
            {
                _droppedBatches++;
                return false;
            }

            return true;
        }

        /// <summary>
        ///     Stops the thread, waiting for queued work so the bundle on disk is complete.
        /// </summary>
        /// <remarks>
        ///     Bounded wait: a hung disk must not hang round end. If the timeout expires the thread is abandoned
        ///     (it is a background thread, so it cannot keep the process alive) and the bundle is left truncated,
        ///     which readers already tolerate because that is what a server crash produces.
        /// </remarks>
        public void Stop(TimeSpan timeout)
        {
            _queue.CompleteAdding();

            if (!_thread.Join(timeout))
                _sawmill.Error($"Investigation writer thread did not finish within {timeout.TotalSeconds:0}s; bundle may be truncated.");
        }

        private void Run()
        {
            try
            {
                foreach (var work in _queue.GetConsumingEnumerable())
                {
                    try
                    {
                        work();
                    }
                    catch (Exception e)
                    {
                        // Record the first failure and keep draining. Later items are cheap no-ops against a dead
                        // stream, and draining means Stop() still returns promptly.
                        _failure ??= e.ToString();
                    }
                }
            }
            catch (Exception e)
            {
                _failure ??= e.ToString();
            }
        }
    }

    #endregion
}
