// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using Content.Shared.CCVar;
using Content.Shared.SS220.CCVars;
using Content.Shared.SS220.Language;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.SS220.Investigation;

public sealed partial class InvestigationRecorder : IInvestigationRecorder
{
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
    private const string GridPoseFile = "gridpose.jsonl.gz";
    private const string MetaFile = "meta.json";

    private const string BundlePrefix = "round-";

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

    /// <remarks>A BOM would make the first line of a stream invalid JSON.</remarks>
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private ISawmill _sawmill = default!;
    private bool _enabled;
    private TimeSpan _flushInterval;
    private TimeSpan _nextFlush;

    private Session? _session;

    private readonly StringBuilder _rowBuilder = new();

    private readonly Dictionary<EntityUid, RosterEntry> _roster = new();

    public IReadOnlyDictionary<EntityUid, RosterEntry> Roster => _roster;

    private readonly Dictionary<EntityUid, SampledPosition> _positions = new();

    private readonly Dictionary<EntityUid, ObjectiveSample> _objectiveSamples = new();

    private readonly Dictionary<int, string> _maps = new();

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

    public void SetPositionSource(IInvestigationPositionSource source)
    {
        _positionSource = source;
    }

    public void SetGamemode(string? id, string? title)
    {
        _gamemode = id;
        _gamemodeTitle = title;
    }

    public void Initialize()
    {
        _sawmill = _logManager.GetSawmill("investigation");
        _cfg.OnValueChanged(CCVars220.InvestigationEnabled, enabled => _enabled = enabled, true);
        _cfg.OnValueChanged(CCVars220.InvestigationFlushInterval, i => _flushInterval = TimeSpan.FromSeconds(i), true);
    }

    public void Shutdown()
    {
        if (_session != null)
            StopRound(null);
    }

    public void StartRound(int roundId, string? map)
    {
        if (!_enabled)
            return;

        if (_session is { } running)
        {
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
                GridPose = Open(GridPoseFile),
            };

            session.AllStreams = openedStreams.ToArray();

            // Written up front so a crashed server still leaves an identifiable bundle.
            WriteMeta(session, null);

            _nextFlush = _timing.CurTime + _flushInterval;
            _session = session;

            _sawmill.Info($"Started investigation recording for round {roundId} at {roundDir}");
        }
        catch (Exception e)
        {
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

        // Held samples exist only on the tracked components and need a live session to write into.
        _positionSource?.FlushPendingPositions();

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
            _objectiveSamples.Clear();
            _maps.Clear();
        }
    }

    /// <remarks>Age comes from the directory name: a copy or restore rewrites mtimes.</remarks>
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
            _sawmill.Error($"Failed to prune old investigation bundles: {e}");
        }

        if (deleted > 0)
            _sawmill.Info($"Deleted {deleted} investigation bundles older than {retentionDays} days.");
    }

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
            languages = _prototype.EnumeratePrototypes<LanguagePrototype>()
                .OrderBy(language => language.ID)
                .Select(language => new
                {
                    id = language.ID,
                    key = language.KeyWithPrefix,
                    name = Loc.GetString(language.Name),
                }),
            maps = _maps.Select(entry => new
            {
                id = entry.Key,
                name = entry.Value,
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

    public void Update()
    {
        if (_session is not { } session)
            return;

        var now = _timing.CurTime;
        if (now < _nextFlush)
            return;

        _nextFlush += _flushInterval;
        if (_nextFlush <= now)
            _nextFlush = now + _flushInterval;

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

        _session = null;
        CloseSession(session);
    }

    private void CloseSession(Session session)
    {
        if (session.Closed)
            return;

        session.Closed = true;

        foreach (var stream in session.AllStreams)
        {
            stream.Close();
        }

        session.Writer.Stop(WriterShutdownTimeout);

        if (session.Writer.Failure is { } failure)
            _sawmill.Error($"Investigation writing failed; bundle may be incomplete: {failure}");

        if (session.Writer.DroppedBatches > 0)
            _sawmill.Warning($"Dropped {session.Writer.DroppedBatches} investigation batches: disk could not keep up.");
    }

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
        public JsonlStream GridPose = default!;

        public JsonlStream[] AllStreams = default!;

        public long RowCount;

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
}
