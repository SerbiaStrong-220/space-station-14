// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Dataset;
using Content.Shared.GameTicking;
using Content.Shared.SS220.CCVars;
using Content.Shared.SS220.TTS;
using Microsoft.IO;
using Prometheus;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Content.Server.SS220.TTS;

public sealed partial class TtsSystem : SharedTtsSystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedTransformSystem _xforms = default!;

    #region Prometheus
    private static readonly Histogram RequestTimings = Metrics.CreateHistogram(
        "tts_req_timings",
        "Timings of TTS API requests",
        new HistogramConfiguration()
        {
            LabelNames = ["type"],
            Buckets = Histogram.ExponentialBuckets(.1, 1.5, 10),
        });

    private static readonly Counter WantedCount = Metrics.CreateCounter(
        "tts_wanted_count",
        "Amount of wanted TTS audio.");

    private static readonly Counter ReusedCount = Metrics.CreateCounter(
        "tts_reused_count",
        "Amount of reused TTS audio from cache.");

    private static readonly Counter WantedRadioCount = Metrics.CreateCounter(
        "tts_wanted_radio_count",
        "Amount of wanted TTS audio.");

    private static readonly Counter ReusedRadioCount = Metrics.CreateCounter(
        "tts_reused_radio_count",
        "Amount of reused TTS audio from cache.");
    #endregion

    private int _maxMessageChars;
    private int _maxAnnounceMessageChars;

    private readonly HashSet<ICommonSession> _sessionsNotToSend = [];

    private float _requestTimeout;
    private const string AudioFileExtension = "ogg";

    private readonly RecyclableMemoryStreamManager _memoryStreamPool = new();

    private static readonly ProtoId<LocalizedDatasetPrototype> VoiceTestSamplesDatasetId = "TtsVoiceTestSamples";
    private LocalizedDatasetPrototype _voiceTestSamplesDataset = default!;

    public override void Initialize()
    {
        base.Initialize();

        _voiceTestSamplesDataset = _prototypeManager.Index(VoiceTestSamplesDatasetId);

        Subs.CVar(_cfg, CCVars220.MaxCharInTTSAnnounceMessage, x => _maxAnnounceMessageChars = x, true);
        Subs.CVar(_cfg, CCVars220.MaxCharInTTSMessage, x => _maxMessageChars = x, true);
        Subs.CVar(_cfg, CCVars220.TtsRequestTimeout, v => _requestTimeout = v, true);

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
        SubscribeNetworkEvent<RequestTtsVoiceTestEvent>(OnRequestTtsVoiceTest);

        // remove if Robust PR for clientCVar subs merged
        SubscribeNetworkEvent<ReceiveTtsCVarChanged>(OnReceiveTtsCVarChanged);
        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
        // end

        InitializeSanitizer();
        InitializeFFMpeg();
        InitializeEntitySubscriptions();
        InitializeNTTS();
        InitializeSilero();
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        ClearCache();
    }

    private void OnRequestTtsVoiceTest(RequestTtsVoiceTestEvent ev, EntitySessionEventArgs args)
    {
        if (!_prototypeManager.TryIndex(ev.VoiceId, out var voice))
            return;

        var request = new TtsVoiceTestRequest()
        {
            Voice = voice,
            Text = Loc.GetString(_random.Pick(_voiceTestSamplesDataset.Values)),
            Receivers = [args.SenderSession]
        };

        RunTtsRequestHandle(request);
    }

    private void OnReceiveTtsCVarChanged(ReceiveTtsCVarChanged msg, EntitySessionEventArgs args)
    {
        if (!msg.Value)
            _sessionsNotToSend.Add(args.SenderSession);
        else
            _sessionsNotToSend.Remove(args.SenderSession);
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus == SessionStatus.Disconnected)
            _sessionsNotToSend.Remove(e.Session);
    }

    /// <summary>
    /// Clears all TTS response caches.
    /// </summary>
    public void ClearCache()
    {
        ClearCache(Enum.GetValues<TtsProvider>());
    }

    /// <summary>
    /// Clears TTS response caches for the specified <paramref name="providers"/>.
    /// </summary>
    public void ClearCache(params TtsProvider[] providers)
    {
        foreach (var provider in providers)
            ClearCache(provider);
    }

    /// <summary>
    /// Clears TTS response cache for the specified <paramref name="provider"/>.
    /// </summary>
    public void ClearCache(TtsProvider provider)
    {
        if (!TryGetProviderHandler(provider, out var handler))
            return;

        handler.ClearCache();
    }

    /// <summary>
    /// Clears TTS audio queues for all connected clients.
    /// </summary>
    public void ClearClientQueues()
    {
        var ev = new TtsClearAllQueuesMessage();
        RaiseNetworkEvent(ev);
    }

    /// <summary>
    /// Clears TTS audio queues for the specified <paramref name="sessions"/>.
    /// </summary>
    public void ClearClientQueues(params ICommonSession[] sessions)
    {
        foreach (var session in sessions)
            ClearClientQueues(session);
    }

    /// <summary>
    /// Clears TTS audio queues for the specified <paramref name="session"/>.
    /// </summary>
    public void ClearClientQueues(ICommonSession session)
    {
        var ev = new TtsClearAllQueuesMessage();
        RaiseNetworkEvent(ev, session);
    }

    private static string ToQueryString(NameValueCollection nvc)
    {
        var array = (
            from key in nvc.AllKeys
            from value in nvc.GetValues(key) ?? []
            select $"{key}={HttpUtility.UrlEncode(value)}"
            ).ToArray();

        return "?" + string.Join("&", array);
    }

    private void RunTaskWithTryCatch(Func<Task> task)
    {
        if (task == null)
            return;

        Task.Run(async () =>
        {
            try
            {
                await task().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Log.Error($"{e.Message}\n{e.StackTrace}");
            }
        });
    }

    private IEnumerable<ICommonSession> ToValidReceivers(IEnumerable<EntityUid> entities)
    {
        return ToValidReceivers(EntitiesToSessions(entities));
    }

    private IEnumerable<ICommonSession> ToValidReceivers(IEnumerable<ICommonSession> receivers)
    {
        return receivers.Where(IsValidReceiver);
    }

    private IEnumerable<ICommonSession> EntitiesToSessions(IEnumerable<EntityUid> entities)
    {
        foreach (var entity in entities)
        {
            if (_playerManager.TryGetSessionByEntity(entity, out var receiver))
                yield return receiver;
        }
    }

    private bool IsValidReceiver(ICommonSession receiver)
    {
        if (_sessionsNotToSend.Contains(receiver))
            return false;

        if (receiver.Status == SessionStatus.Disconnected)
            return false;

        return true;
    }

    private sealed class TtsCache()
    {
        private readonly ConcurrentDictionary<TtsCacheKey, TtsResponse.Reference> _lookup = new();
        private readonly ConcurrentQueue<TtsCacheKey> _keysQueue = new();

        public int Limit
        {
            get => _limit;
            set => _limit = Math.Max(value, 0);
        }

        private int _limit = 1;

        public TtsCache(int limit) : this()
        {
            Limit = limit;
        }

        public void Cache(TtsCacheKey key, TtsResponse value)
        {
            var currentCount = _lookup.Count;
            while (currentCount > 0 && currentCount + 1 > Limit)
            {
                if (_keysQueue.TryDequeue(out var firstKey) && _lookup.TryRemove(firstKey, out var responce))
                    responce.Dispose();

                currentCount = _lookup.Count;
            }

            if (Limit != 0)
            {
                _lookup[key] = value.GetReference();
                _keysQueue.Enqueue(key);
            }
        }

        public bool TryGet(TtsCacheKey key, [NotNullWhen(true)] out TtsResponse.Reference? responce)
        {
            responce = null;
            if (!_lookup.TryGetValue(key, out var exist))
                return false;

            responce = exist;
            return true;
        }

        public void Clear()
        {
            _lookup.Clear();
            _keysQueue.Clear();
        }

        public void Trim()
        {
            while (_lookup.Count > Limit)
            {
                if (_keysQueue.TryDequeue(out var firstKey) && _lookup.TryRemove(firstKey, out var reuseBuffer))
                    reuseBuffer.GetReference().Dispose();
            }

        }
    }
}

[Virtual]
public class ReferenceCounter<T>(T value)
{
    public T Value = value;
    public int ReferenceCount { get; private set; } = 0;

    public Reference GetReference()
    {
        ReferenceCount++;
        return new(this);
    }

    protected virtual void OnReferenceDisposed()
    {
        ReferenceCount--;
    }

    public struct Reference(ReferenceCounter<T> counter) : IDisposable
    {
        private readonly ReferenceCounter<T> _counter = counter;
        private bool _disposed = false;

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _counter.OnReferenceDisposed();
        }

        public readonly Reference GetReference()
        {
            return _counter.GetReference();
        }

        public readonly bool TryGetValue([NotNullWhen(true)] out T? value)
        {
            value = _counter.Value;
            return _disposed && value != null;
        }
    }
}

public static class ReferenceCounterExtentions
{
    public static bool TryGetValue<T>(this ReferenceCounter<T>.Reference? reference, [NotNullWhen(true)] out T? value)
    {
        value = default;
        return reference != null && reference.Value.TryGetValue(out value);
    }
}
