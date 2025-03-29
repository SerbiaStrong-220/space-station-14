// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Content.Shared.Corvax.CCCVars;
using Content.Shared.SS220.TTS;
using FFMpegCore;
using FFMpegCore.Arguments;
using FFMpegCore.Pipes;
using Microsoft.IO;
using Prometheus;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Server.SS220.TTS;

// ReSharper disable once InconsistentNaming
public sealed class TTSManager
{
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

    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IServerNetManager _netManager = default!;

    private readonly HttpClient _httpClient = new();

    private ISawmill _sawmill = default!;
    private readonly TtsCache _cache = new(0);
    private readonly TtsResponseManager _responseManager = new();
    private readonly RecyclableMemoryStreamManager _memoryStreamPool = new();

    private static readonly ConcurrentDictionary<string, TtsResponse> ResponsesInProgress = new();
    private float _timeout = 1;

    private string _apiUrl = string.Empty;
    private string _apiToken = string.Empty;

    public void Initialize()
    {
        _sawmill = Logger.GetSawmill("tts");
        _cfg.OnValueChanged(CCCVars.TTSMaxCache, val =>
        {
            _cache.Limit = val;
            ResetCache();
        }, true);
        _cfg.OnValueChanged(CCCVars.TTSRequestTimeout, val => _timeout = val, true);
        _cfg.OnValueChanged(CCCVars.TTSApiUrl, v => _apiUrl = v, true);
        _cfg.OnValueChanged(CCCVars.TTSApiToken, v =>
        {
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", v);
            _apiToken = v;
        },
        true);

        _netManager.RegisterNetMessage<MsgPlayTts>();
        _netManager.RegisterNetMessage<MsgPlayAnnounceTts>();
    }

    /// <summary>
    /// Generates audio with passed text by API
    /// </summary>
    /// <param name="speaker">Identifier of speaker</param>
    /// <param name="text">SSML formatted text</param>
    /// <returns>File audio bytes or empty if failed</returns>
    public async Task<ReferenceCounter<TtsAudioData>.Handle?> ConvertTextToSpeech(string speaker, string text, TtsKind kind)
    {
        WantedCount.Inc();

        return await StartTtsRequest(new(speaker, text, kind),
            async (request, response) =>
        {
            _sawmill.Verbose($"Generate new sound for '{text}' speech by '{speaker}' speaker with kind '{kind}'");

            var reqTime = DateTime.UtcNow;
            try
            {
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_timeout));

                var requestUrl = $"{_apiUrl}" + ToQueryString(new NameValueCollection() {
                    { "speaker", speaker },
                    { "text", text },
                    { "ext", AudioFileExtension }});

                if (kind == TtsKind.Radio)
                {
                    requestUrl += "&effect=radio";
                }

                if (kind == TtsKind.Announce)
                {
                    requestUrl += "&effect=announce";
                }

                var httpRequest = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                var httpResponse = await _httpClient.SendAsync(httpRequest, cts.Token);
                if (!httpResponse.IsSuccessStatusCode)
                {
                    if (httpResponse.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        _sawmill.Warning("TTS request was rate limited");
                        return false;
                    }

                    _sawmill.Error($"TTS request returned bad status code: {httpResponse.StatusCode}");
                    return false;
                }

                using var memoryStream = _memoryStreamPool.GetStream("TtsStream", 1024 * 64);

                memoryStream.Position = 0;
                memoryStream.SetLength(0);

                await httpResponse.Content.CopyToAsync(memoryStream, cts.Token);
                _responseManager.AllocBuffer(response, (int)memoryStream.Length);

                memoryStream.Position = 0;
                memoryStream.ReadExactly(response.Value.Buffer, 0, response.Value.Length);

                _sawmill.Verbose($"Generated new sound for '{text}' speech by '{speaker}' speaker with kind '{kind}' ({response.Value.Length} bytes)");
                RequestTimings.WithLabels("Success").Observe((DateTime.UtcNow - reqTime).TotalSeconds);
                return true;
            }
            catch (TaskCanceledException)
            {
                RequestTimings.WithLabels("Timeout").Observe((DateTime.UtcNow - reqTime).TotalSeconds);
                _sawmill.Error($"Timeout of request generation new audio for '{text}' speech by '{speaker}' speaker");
                return false;
            }
            catch (Exception e)
            {
                RequestTimings.WithLabels("Error").Observe((DateTime.UtcNow - reqTime).TotalSeconds);
                _sawmill.Error(
                    $"Failed of request generation new sound for '{text}' speech by '{speaker}' speaker\n{e}");
                return false;
            }
        });
    }

    public async Task<ReferenceCounter<TtsAudioData>.Handle?> ConvertTextToSpeechRadio(string speaker, string text)
    {
        WantedRadioCount.Inc();

        return await StartTtsRequest(new($"radio-{speaker}", text, TtsKind.Radio),
            async (request, response) =>
        {
            using var innerResponse = await ConvertTextToSpeech(speaker, text, TtsKind.Radio);

            if (innerResponse is null
                || innerResponse.Value.TryGetValue(out var innerBuffer))
                return false;

            using var memoryStream = _memoryStreamPool.GetStream("TtsStream", innerBuffer.Length);
            memoryStream.Write(innerBuffer.AsMemory().Span);
            memoryStream.Position = 0;

            var reqTime = DateTime.UtcNow;
            try
            {
                var outputFilename = $"{Path.GetTempPath()}{Guid.NewGuid()}.{AudioFileExtension}";
                await FFMpegArguments
                    .FromPipeInput(new StreamPipeSource(memoryStream))
                    .OutputToFile(outputFilename, true, options =>
                        options.WithAudioFilters(filterOptions =>
                            {
                                filterOptions
                                    .HighPass(frequency: 1000D)
                                    .LowPass();
                                filterOptions.Arguments.Add(
                                    new CrusherFilterArgument(levelIn: 1, levelOut: 1, bits: 50, mix: 0, mode: "log")
                                );
                            }
                        )
                    ).ProcessAsynchronously();

                memoryStream.SetLength(0);
                memoryStream.Position = 0;

                try
                {
                    using var file = new FileStream(outputFilename, FileMode.Open);
                    await file.CopyToAsync(memoryStream);
                    _responseManager.AllocBuffer(response, (int)memoryStream.Length);
                    memoryStream.ReadExact(response.Value.AsMemory().Span);
                }
                catch (Exception)
                {
                    // ignored
                }

                try
                {
                    File.Delete(outputFilename);
                }
                catch (Exception)
                {
                    // ignored
                }

                return true;
            }
            catch (TaskCanceledException)
            {
                RequestTimings.WithLabels("Timeout").Observe((DateTime.UtcNow - reqTime).TotalSeconds);
                _sawmill.Error(
                    $"Timeout of request generation new radio sound for '{text}' speech by '{speaker}' speaker");
                throw new Exception("TTS request timeout");
            }
            catch (Win32Exception)
            {
                _sawmill.Error($"FFMpeg is not installed");
                throw new Exception("ffmpeg is not installed!");
            }
            catch (Exception e)
            {
                RequestTimings.WithLabels("Error").Observe((DateTime.UtcNow - reqTime).TotalSeconds);
                _sawmill.Error(
                    $"Failed of request generation new radio sound for '{text}' speech by '{speaker}' speaker\n{e}");
                throw new Exception("TTS request failed");
            }
        });
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

    private static string GenerateCacheKey(string speaker, string text, TtsKind kind)
    {
        var key = $"{speaker}/{text}/{(int)kind}";
        var keyData = Encoding.UTF8.GetBytes(key);
        var bytes = System.Security.Cryptography.SHA256.HashData(keyData);
        return Convert.ToHexString(bytes);
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

    private sealed class CrusherFilterArgument : IAudioFilterArgument
    {
        private readonly Dictionary<string, string> _arguments = new Dictionary<string, string>();

        public CrusherFilterArgument(
            double levelIn = 1f,
            double levelOut = 1f,
            int bits = 1,
            double mix = 1.0,
            string mode = "log")
        {
            _arguments.Add("level_in", levelIn.ToString("0", CultureInfo.InvariantCulture));
            _arguments.Add("level_out", levelOut.ToString("0", CultureInfo.InvariantCulture));
            _arguments.Add("bits", bits.ToString("0", CultureInfo.InvariantCulture));
            _arguments.Add("mix", mix.ToString("0", CultureInfo.InvariantCulture));
            _arguments.Add("mode", mode);
        }

        public string Key => "acrusher";

        public string Value => string.Join(":", _arguments.Select<KeyValuePair<string, string>, string>(pair => pair.Key + "=" + pair.Value));
    }

    private readonly struct TtsRequest
    {
        public string Speaker { get; }
        public string Text { get; }
        public TtsKind Kind { get; }
        public string Key { get; }

        public TtsRequest(string speaker, string text, TtsKind kind) : this()
        {
            Speaker = speaker;
            Text = text;
            Kind = kind;
            Key = GenerateCacheKey(speaker, text, kind);
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

public sealed class TtsResponseManager
{
    private readonly Stack<TtsResponse> _responsePool = new();
    private readonly ArrayPool<byte> _arrayPool;

    public TtsResponseManager() : this(ArrayPool<byte>.Shared) { }

    public TtsResponseManager(ArrayPool<byte> arrayPool)
    {
        _arrayPool = arrayPool;
    }

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

public sealed class TtsResponse : ReferenceCounter<TtsAudioData>
{
    public Task<bool>? Task { get; set; }

    private readonly TtsResponseManager _manager;

    public TtsResponse(TtsResponseManager manager) : base(new())
    {
        _manager = manager;
    }

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
