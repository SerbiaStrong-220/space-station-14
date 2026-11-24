// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Dataset;
using Content.Shared.GameTicking;
using Content.Shared.SS220.CCVars;
using Content.Shared.SS220.TTS;
using Content.Shared.SS220.TTS.Systems;
using Microsoft.IO;
using Prometheus;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Content.Server.SS220.TTS;

public sealed partial class TtsSystem : SharedTtsSystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IServerNetManager _netManager = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedTransformSystem _xforms = default!;

    #region Prometheus
    private const string ProviderLabel = "provider";

    private static readonly Histogram RequestTimings = Metrics.CreateHistogram(
        "tts_req_timings",
        "Timings of TTS provider API requests in seconds",
        new HistogramConfiguration()
        {
            LabelNames = [ProviderLabel, "result"],
            Buckets = Histogram.ExponentialBuckets(.1, 1.5, 12),
        });

    private static readonly Counter WantedCount = Metrics.CreateCounter(
        "tts_wanted_count",
        "Amount of wanted TTS audio.",
        ProviderLabel);

    private static readonly Counter ReusedCount = Metrics.CreateCounter(
        "tts_reused_count",
        "Amount of TTS audio reused from the provider cache.",
        ProviderLabel);

    private static readonly Histogram FFMpegProcessingTimings = Metrics.CreateHistogram(
        "tts_ffmpeg_process_seconds",
        "Seconds spent on ffmpeg processing (including pipe operations) of TTS audio",
        new HistogramConfiguration()
        {
            LabelNames = ["kind", "result"],
            Buckets = Histogram.ExponentialBuckets(.005, 1.6, 12),
        });
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
        InitializeEntityAPI();
        InitializeNTTS();
        InitializeSilero();
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        ClearCache();
    }

    private void OnRequestTtsVoiceTest(RequestTtsVoiceTestEvent ev, EntitySessionEventArgs args)
    {
        if (IsVoiceTestCooldowned(args.SenderSession))
            return;

        if (!_prototypeManager.TryIndex(ev.VoiceId, out var voice))
            return;

        var request = new TtsVoiceTestRequest()
        {
            Voice = voice,
            Text = Loc.GetString(_random.Pick(_voiceTestSamplesDataset.Values)),
            Receivers = [args.SenderSession]
        };

        RunTtsRequestHandle(request);
        SetVoiceTestCooldown(args.SenderSession, DefaultVoiceTestRequestCooldown);
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

    private void SendTtsMessage(MsgPlayTts message, ICommonSession receiver)
    {
        _netManager.ServerSendMessage(message, receiver.Channel);
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
            if (Limit == 0)
                return;

            // The same response may be cached by every request that waited for it.
            // Overwriting would lose the reference without disposing it.
            if (_lookup.ContainsKey(key))
                return;

            Trim(Limit - 1);

            var reference = value.GetReference();
            if (_lookup.TryAdd(key, reference))
                _keysQueue.Enqueue(key);
            else
                reference.Dispose();
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
            foreach (var key in _lookup.Keys)
            {
                if (_lookup.TryRemove(key, out var reference))
                    reference.Dispose();
            }

            _keysQueue.Clear();
        }

        public void Trim()
        {
            Trim(Limit);
        }

        /// <summary>
        /// Removes the oldest responses until there are no more than <paramref name="maxCount"/> of them.
        /// </summary>
        private void Trim(int maxCount)
        {
            while (_lookup.Count > maxCount)
            {
                if (!_keysQueue.TryDequeue(out var firstKey))
                    break;

                if (_lookup.TryRemove(firstKey, out var reference))
                    reference.Dispose();
            }
        }
    }
}

[Virtual]
public class ReferenceCounter<T>(T value)
{
    public T Value = value;

    private int _referenceCount;

    public int ReferenceCount => Volatile.Read(ref _referenceCount);

    public Reference GetReference()
    {
        Interlocked.Increment(ref _referenceCount);
        return new(this);
    }

    protected virtual void OnReferenceDisposed()
    {
        if (Interlocked.Decrement(ref _referenceCount) == 0)
            OnAllReferencesDisposed();
    }

    protected virtual void OnAllReferencesDisposed() { }

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
            return !_disposed && value != null;
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
