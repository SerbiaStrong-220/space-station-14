// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.CCVars;
using Content.Shared.SS220.TTS;
using Microsoft.IO;
using Prometheus;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Serilog;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Content.Server.SS220.TTS;

// ReSharper disable once InconsistentNaming
public sealed partial class TTSManager : SharedTTSManager
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IServerNetManager _netManager = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    private static readonly Histogram RequestTimings = Metrics.CreateHistogram(
        "tts_req_timings",
        "Timings of TTS API requests",
        new HistogramConfiguration()
        {
            LabelNames = new[] { "type" },
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

    private const string AudioFileExtension = "ogg";

    private readonly HttpClient _httpClient = new();

    private ISawmill _sawmill = default!;
    private readonly TtsCache _cache = new(0);
    private readonly TtsResponseManager _responseManager = new();
    private readonly RecyclableMemoryStreamManager _memoryStreamPool = new();

    private static readonly ConcurrentDictionary<string, TtsResponse> ResponsesInProgress = new();
    private float _timeout = 1;

    public override void Initialize()
    {
        base.Initialize();

        InitializeFFMpeg();

        _sawmill = Logger.GetSawmill("tts");
        _cfg.OnValueChanged(CCVars220.TTSMaxCache, val =>
        {
            _cache.Limit = val;
            ResetCache();
        }, true);
        _cfg.OnValueChanged(CCVars220.TTSRequestTimeout, val => _timeout = val, true);
        _cfg.OnValueChanged(CCVars220.NTTSApiUrl, v => _nttsApiUrl = v, true);
        _cfg.OnValueChanged(CCVars220.TTSSileroApiUrl, v => _sileroApiUrl = v, true);
        _cfg.OnValueChanged(CCVars220.TTSSileroApiToken, v =>
        {
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", v);
            _sileroApiToken = v;
        },
        true);

        _netManager.RegisterNetMessage<MsgPlayTts>();
        _netManager.RegisterNetMessage<MsgPlayAnnounceTts>();
    }

    public async Task<ReferenceCounter<TtsAudioData>.Handle?> ConvertTextToSpeech(ProtoId<TTSVoicePrototype>? protoId, string text, TtsKind kind)
    {
        if (protoId == null && !TryGetDefaultPreferredVoice(out protoId))
            return default;

        if (!_proto.TryIndex(protoId, out var proto))
            return default;

        return await ConvertTextToSpeech(proto.Provider, proto.Speaker, text, kind);
    }

    public async Task<ReferenceCounter<TtsAudioData>.Handle?> ConvertTextToSpeech(TTSProvider provider, string speaker, string text, TtsKind kind)
    {
        try
        {
            var textSanitized = Sanitize(text);
            if (textSanitized == "") return default;
            if (char.IsLetter(textSanitized[^1]))
                textSanitized += ".";

            var ssmlTraits = SoundTraits.RateFast;
            if (kind == TtsKind.Whisper)
                ssmlTraits |= SoundTraits.PitchVerylow;

            var textSsml = ToSsmlText(textSanitized, ssmlTraits);

            return provider switch
            {
                TTSProvider.NTTS => await SendNttsRequest(speaker, textSsml, kind),
                TTSProvider.Silero => await SendSileroRequest(speaker, textSsml, kind),
                _ => null
            };
        }
        catch (Exception e)
        {
            // Catch TTS exceptions to prevent a server crash.
            Log.Error($"TTS System error: {e.Message}");
        }

        return default;
    }

    public void ResetCache()
    {
        _cache.Clear();
    }

    private static string ToQueryString(NameValueCollection nvc)
    {
        var array = (
            from key in nvc.AllKeys
            from value in nvc.GetValues(key) ?? Array.Empty<string>()
            select $"{key}={HttpUtility.UrlEncode(value)}"
            ).ToArray();

        return "?" + string.Join("&", array);
    }

    private async Task<ReferenceCounter<TtsAudioData>.Handle?> StartTtsRequest(TtsRequest request, Func<TtsRequest, TtsResponse, Task<bool>> core)
    {
        if (_cache.TryGet(request.Key, out var data))
        {
            ReusedCount.Inc();
            _sawmill.Debug($"Use cached sound for '{request.Text}' speech by '{request.Speaker}' speaker");
            return data.GetHandle();
        }

        try
        {
            if (!ResponsesInProgress.TryGetValue(request.Key, out var response) || response.Task is null)
            {
                response = _responseManager.Rent();
                var task = core(request, response);
                response.Task = task;
                ResponsesInProgress[request.Key] = response;
            }

            var isSuccess = await response.Task;

            if (isSuccess)
            {
                _cache.Cache(request.Key, response);
                return response.GetHandle();
            }
            else
            {
                return null;
            }
        }
        finally
        {
            ResponsesInProgress.TryRemove(request.Key, out _);
        }
    }

    private readonly struct TtsRequest
    {
        public readonly TTSProvider Provider;
        public readonly string Speaker;
        public readonly string Text;
        public readonly TtsKind Kind;
        public readonly string Key;

        public TtsRequest(TTSProvider provider, string speaker, string text, TtsKind kind) : this()
        {
            Speaker = speaker;
            Text = text;
            Kind = kind;
            Key = GenerateCacheKey(provider, speaker, text, kind);
        }

        private static string GenerateCacheKey(TTSProvider provider, string speaker, string text, TtsKind kind)
        {
            var key = $"{provider}/{speaker}/{text}/{(int)kind}";
            var keyData = Encoding.UTF8.GetBytes(key);
            var bytes = System.Security.Cryptography.SHA256.HashData(keyData);
            return Convert.ToHexString(bytes);
        }
    }

    private sealed class TtsCache
    {
        private readonly ConcurrentDictionary<string, TtsResponse> _lookup = new();
        private readonly ConcurrentQueue<string> _keysQueue = new();

        public int Limit { get; set; }

        public TtsCache(int limit)
        {
            Limit = limit;
        }

        public void Cache(string key, TtsResponse value)
        {
            var currentCount = _lookup.Count;
            while (currentCount > 0 && currentCount + 1 > Limit)
            {
                if (_keysQueue.TryDequeue(out var firstKey)
                    && _lookup.TryRemove(firstKey, out var reuseBuffer))
                {
                    reuseBuffer.GetHandle().Dispose();
                }
                currentCount = _lookup.Count;
            }
            if (Limit != 0)
            {
                value.GetHandle();
                _lookup[key] = value;
                _keysQueue.Enqueue(key);
            }
        }

        public bool TryGet(string key, [NotNullWhen(true)] out TtsResponse? buffer)
        {
            if (Limit == 0)
            {
                buffer = null;
                return false;
            }
            return _lookup.TryGetValue(key, out buffer);
        }

        public void Clear()
        {
            _lookup.Clear();
            _keysQueue.Clear();
        }
    }
}

public sealed class TtsResponseManager(ArrayPool<byte> arrayPool)
{
    private readonly Stack<TtsResponse> _responsePool = new();
    private readonly ArrayPool<byte> _arrayPool = arrayPool;

    public TtsResponseManager() : this(ArrayPool<byte>.Shared) { }

    public TtsResponse Rent()
    {
        if (!_responsePool.TryPop(out var response))
        {
            response = new(this);
        }

        return response;
    }

    public void Return(TtsResponse response)
    {
        FreeBuffer(response);
        _responsePool.Push(response);
    }

    public void AllocBuffer(TtsResponse response, int length)
    {
        response.Value = new(_arrayPool.Rent(length), length);
    }

    public void FreeBuffer(TtsResponse response)
    {
        if (response.Value.Buffer.Length == 0)
            return;
        _arrayPool.Return(response.Value.Buffer);
        response.Value = new();
    }
}

public sealed class TtsResponse(TtsResponseManager manager) : ReferenceCounter<TtsAudioData>(new())
{
    public Task<bool>? Task { get; set; }

    private readonly TtsResponseManager _manager = manager;

    protected override void OnHandleDisposed()
    {
        base.OnHandleDisposed();
        if (ReferenceCount == 0)
        {
            _manager.Return(this);
        }
    }

    public void Dereference()
    {
        OnHandleDisposed();
    }
}

[Virtual]
public class ReferenceCounter<T>
{
    public T Value { get; set; }
    public int ReferenceCount => _referenceCount;

    private int _referenceCount = 0;

    public ReferenceCounter(T value)
    {
        Value = value;
    }

    public Handle GetHandle()
    {
        _referenceCount++;
        return new(this);
    }

    protected virtual void OnHandleDisposed()
    {
        _referenceCount--;
    }

    public struct Handle : IDisposable
    {
        private readonly ReferenceCounter<T> _counter;
        private bool _isValid;

        public Handle(ReferenceCounter<T> counter)
        {
            _counter = counter;
            _isValid = true;
        }

        public void Dispose()
        {
            if (!_isValid) return;
            _isValid = false;
            _counter.OnHandleDisposed();
        }

        public Handle GetHandle()
        {
            return _counter.GetHandle();
        }

        public bool TryGetValue([NotNullWhen(true)] out T value)
        {
            value = _counter.Value;
            return _isValid;
        }
    }
}
public static class ReferenceCounterExtensions
{
    public static bool TryGetValue<T>(this ReferenceCounter<T>.Handle? handle, [NotNullWhen(true)] out T? value)
    {
        value = default;
        return handle.HasValue && handle.Value.TryGetValue(out value);
    }
}
