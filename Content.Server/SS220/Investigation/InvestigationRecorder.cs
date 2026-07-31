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

/// <summary>Writes the per-round investigation bundle. See <see cref="IInvestigationRecorder"/>.</summary>
/// <remarks>Everything here runs on the main game thread; rows are buffered and flushed on an interval.</remarks>
public sealed class InvestigationRecorder : IInvestigationRecorder
{
    /// <summary>Bump when the on-disk row shapes change in a way readers must care about.</summary>
    public const int SchemaVersion = 1;

    private static readonly TimeSpan WriterShutdownTimeout = TimeSpan.FromSeconds(15);

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

    /// <summary>Prefix every bundle directory is named with. Retention matches on it, so nothing else is deleted.</summary>
    private const string BundlePrefix = "round-";

    /// <summary>Progress at or above which an objective counts as done. Matches <c>SharedObjectivesSystem.IsCompleted</c>.</summary>
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

    /// <summary>UTF-8 without a byte order mark.</summary>
    /// <remarks><see cref="Encoding.UTF8"/> emits a BOM, which makes the first line of a stream invalid JSON.</remarks>
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private ISawmill _sawmill = default!;
    private bool _enabled;
    private float _flushInterval;
    private float _flushAccumulator;

    private Session? _session;

    /// <summary>Reused to build position rows by hand; the serializer would allocate per sample.</summary>
    private readonly StringBuilder _rowBuilder = new();

    private readonly Dictionary<EntityUid, RosterEntry> _roster = new();

    /// <summary>
    ///     Every entity ever player-controlled this round. Keyed by <see cref="EntityUid.Id"/>, which is what admin
    ///     logs record and is never recycled within a round.
    /// </summary>
    public IReadOnlyDictionary<EntityUid, RosterEntry> Roster => _roster;

    /// <summary>Latest observed position of each tracked entity, used to enrich admin logs and chat.</summary>
    private readonly Dictionary<EntityUid, SampledPosition> _positions = new();

    /// <summary>Dead-reckoning state per tracked entity: what was last written, and what is being held back.</summary>
    private readonly Dictionary<EntityUid, PositionTrack> _tracks = new();

    private readonly Dictionary<EntityUid, int> _loadoutHashes = new();

    private readonly Dictionary<EntityUid, HealthSample> _healthSamples = new();

    /// <summary>Last written progress per objective entity.</summary>
    /// <remarks>Keyed on the objective entity, not its owner: objectives follow the mind, which moves between bodies.</remarks>
    private readonly Dictionary<EntityUid, ObjectiveSample> _objectiveSamples = new();

    private string? _gamemode;
    private string? _gamemodeTitle;

    private IInvestigationPositionSource? _positionSource;

    public bool IsRecording => _session != null;

    public ResPath? LastBundlePath { get; private set; }

    public ResPath? CurrentBundlePath => _session?.Directory;

    public void Flush()
    {
        if (_session is { } session)
            FlushSession(session);
    }

    /// <summary>Supplies the live position lookup for speech from entities that are not on the roster.</summary>
    public void SetPositionSource(IInvestigationPositionSource source)
    {
        _positionSource = source;
    }

    /// <summary>Records which game preset the round is running under.</summary>
    /// <remarks>Set at round start and again at round end, because "secret" resolves to a real preset.</remarks>
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
            // Reaching this means a lifecycle hook was missed. Keep the running session rather than folding two rounds.
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

            // Written up front so a crashed server still leaves an identifiable bundle. Rewritten on a clean stop.
            WriteMeta(session, null);

            _session = session;

            _sawmill.Info($"Started investigation recording for round {roundId} at {roundDir}");
        }
        catch (Exception e)
        {
            // Without this a repeatedly failing start leaks a thread and a handful of handles every round.
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

        // Held-back samples live only in memory and need a live session to write into.
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

    /// <summary>Deletes bundles older than <c>investigation.retention_days</c>.</summary>
    /// <remarks>Age comes from the directory name, not the filesystem: a copy or restore rewrites mtimes.</remarks>
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

                // "round-{id}_{timestamp}": only the part after the first underscore is the timestamp.
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

    /// <remarks>Fastest rather than Optimal: about a tenth worse ratio for several times less CPU.</remarks>
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
            // Chat keeps its `%key` prefixes, which need the round's key table to split into per-language runs.
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

    /// <summary>Registers an entity as tracked. It stays tracked for the rest of the round, corpse included.</summary>
    public void TrackEntity(EntityUid uid, Guid? playerGuid, string? userName, string name, string? prototype)
    {
        if (_session is not { } session || _roster.ContainsKey(uid))
            return;

        var entry = new RosterEntry(playerGuid, userName, name, prototype, _timing.CurTick.Value);
        _roster[uid] = entry;

        // Also appended to its own stream, so the roster survives a crash.
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

    /// <summary>Records a player taking control of an entity, or letting go of it.</summary>
    /// <remarks>Bodies change hands — cloning, borging, revival, admin possession — so every transfer gets a row.</remarks>
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
        // No later sample will arrive to justify the held one. Write it now or the last known position is lost.
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

    /// <summary>Records a position sample, emitting a row only when the reader could not reconstruct it.</summary>
    /// <remarks>
    ///     Two filters: an epsilon that drops unmoved entities, and dead reckoning that holds one sample back and
    ///     drops it when it lands on the line between the last emitted row and the next. Only direction changes
    ///     become rows, so samples are written one behind. Grid and container changes always flush.
    /// </remarks>
    public bool WritePosition(EntityUid uid, EntityUid? grid, Vector2 local, EntityUid? container, float epsilon)
    {
        if (_session is null)
            return false;

        var tick = _timing.CurTick.Value;
        var observed = new SampledPosition(grid, local, container);

        // Always the latest reading, regardless of what gets written: this is what admin logs and chat join against.
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
                // Standing still costs no rows, but the tick is where the stationary stretch ends.
                _tracks[uid] = track with { LastSampleTick = tick };
                return false;
            }

            // Movement resuming after a stationary stretch needs a row closing it, or the reader interpolates from
            // a row that may be minutes old and renders the entity drifting across the room for the whole time.
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

    /// <remarks>If no later sample arrives, the held sample is the last known position and must not be lost.</remarks>
    private void FlushPending(EntityUid uid, ref PositionTrack track)
    {
        if (track.PendingTick is not { } pendingTick)
            return;

        EmitPosition(uid, pendingTick, track.PendingSample);
        track = new PositionTrack(pendingTick, track.PendingSample, null, default, track.LastSampleTick);
    }

    /// <summary>Flushes every held position sample. Called at round end so no last movement is dropped.</summary>
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

    /// <remarks>One decimal: the movement epsilon is 0.15 tiles, so a second encodes only sampling jitter.</remarks>
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

    /// <remarks>A non-finite coordinate is not valid JSON and would make its whole line unparseable, so it is clamped.</remarks>
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

    /// <summary>Records a health sample, but only when it differs from the last one written.</summary>
    /// <remarks>Polled rather than hooked to DamageChangedEvent: continuous damage would produce an unbounded stream.</remarks>
    public void WriteHealthIfChanged(
        EntityUid uid,
        float damage,
        string state,
        float? critThreshold,
        float? deadThreshold)
    {
        if (_session is not { } session)
            return;

        // Compared by value, not by hash: a collision would silently drop a row, quite possibly one recording a death.
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

    /// <summary>Records an objective's progress, but only when it moved since the last row.</summary>
    /// <remarks>The point of this stream is the tick, not the tally: when an objective completed is recorded nowhere else.</remarks>
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

    /// <summary>Writes a character loadout snapshot, but only if it differs from the last one written.</summary>
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

    /// <summary>Runs one of the hooks foreign code calls into, swallowing anything it throws.</summary>
    /// <remarks>An exception escaping one would take down chat, admin logging or the console, so the recorder fails alone.</remarks>
    private void Guarded(Action write)
    {
        try
        {
            write();
        }
        catch (Exception e)
        {
            _sawmill.Error($"Investigation recording threw and has been stopped for this round: {e}");

            // Not left running: a hook that throws once will almost certainly throw on the next row.
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
            // `lang` is the speaker's selected language; `langs` only appears on lines that mixed two or more.
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

                    // Resolved here rather than in the reader: the sampled position is free at this moment.
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
                // Some call sites pass coordinates directly (e.g. PointingSystem); those beat the sampled cache.
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

    public void Update(float frameTime)
    {
        if (_session is not { } session)
            return;

        _flushAccumulator += frameTime;
        if (_flushAccumulator < _flushInterval)
            return;

        _flushAccumulator -= _flushInterval;
        FlushSession(session);
    }

    private void FlushSession(Session session)
    {
        foreach (var stream in session.AllStreams)
        {
            stream.Flush();
        }

        if (session.Writer.Failure is null)
            return;

        // The bundle is already damaged. Streams still get closed, so what reached disk stays readable.
        _session = null;
        CloseSession(session);
    }

    private void CloseSession(Session session)
    {
        // A failed flush tears the session down mid-stop, and StopRound then reaches CloseSession with the same session.
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
        /// <summary>Grid and container changes always warrant a row; plain movement only past the epsilon.</summary>
        public bool DiffersFrom(SampledPosition other, float epsilon)
        {
            if (IsDiscontinuousFrom(other))
                return true;

            return (Local - other.Local).LengthSquared() >= epsilon * epsilon;
        }

        /// <summary>Whether this is a teleport rather than travel. Interpolating across one draws a path through walls.</summary>
        public bool IsDiscontinuousFrom(SampledPosition other) => Grid != other.Grid || Container != other.Container;
    }

    /// <summary>Per-entity dead-reckoning state: last row emitted, plus at most one sample held back.</summary>
    /// <param name="LastSampleTick">Tick of the most recent sample, written or not. Dates the anchor row closing a stationary stretch.</param>
    private readonly record struct PositionTrack(
        uint WrittenTick,
        SampledPosition WrittenSample,
        uint? PendingTick,
        SampledPosition PendingSample,
        uint LastSampleTick);

    /// <summary>What the health stream deduplicates on. Thresholds are static per entity.</summary>
    private readonly record struct HealthSample(double Damage, string State);

    /// <summary>What the objectives stream deduplicates on.</summary>
    /// <remarks>Completion is carried separately so the row where an objective flips to done is always emitted.</remarks>
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

        /// <summary>Set once, right after the streams are opened; rebuilding per flush would allocate.</summary>
        public JsonlStream[] AllStreams = default!;

        public long RowCount;

        /// <summary>Set once streams are handed off for closing, so a second attempt is a no-op.</summary>
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

    /// <summary>Newline-delimited JSON stream; buffers rows on the game thread, writes on the writer thread.</summary>
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

        /// <summary>Hands buffered rows to the writer thread. False if the queue was full and the batch was dropped.</summary>
        public bool Flush()
        {
            if (_buffer.Count == 0)
                return true;

            var batch = _buffer;
            _buffer = new List<string>();
            return _thread.Enqueue(_writer, batch);
        }

        /// <summary>Flushes and closes the file on the writer thread, never on the game thread.</summary>
        public void Close() => _thread.EnqueueClose(_writer);
    }

    /// <summary>Single background thread that owns all disk I/O for a recording session.</summary>
    /// <remarks>Bounded queue: if the disk cannot keep up, batches are dropped and counted rather than growing the heap.</remarks>
    private sealed class StreamWriterThread
    {
        /// <summary>Reaching this means the disk is unhealthy, not that the server is busy.</summary>
        private const int QueueCapacity = 64;

        private static readonly TimeSpan CloseEnqueueTimeout = TimeSpan.FromSeconds(2);

        private readonly BlockingCollection<Action> _queue = new(QueueCapacity);
        private readonly Thread _thread;
        private readonly ISawmill _sawmill;

        private int _droppedBatches;

        /// <summary>Written by the writer thread, read by the game thread; teardown happens on the game thread.</summary>
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
            // Not routed through Post: a dropped close costs the gzip trailer. Bounded so a hung disk cannot park the tick.
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

        /// <summary>Stops the thread, waiting for queued work. Bounded: a hung disk must not hang round end.</summary>
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
                        // Record the first failure and keep draining.
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
