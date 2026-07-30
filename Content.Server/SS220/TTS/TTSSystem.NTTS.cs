using Content.Shared.SS220.CCVars;
using Content.Shared.SS220.TTS;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Content.Server.SS220.TTS;

public partial class TTSSystem
{
    private void InitializeNTTS()
    {
        _cfg.OnValueChanged(CCVars220.NTTSApiUrl, v => NTTSHandler.ApiUrl = v, true);
        _cfg.OnValueChanged(CCVars220.NTTSMaxCache, v =>
        {
            NTTSHandler.Cache.Limit = v;
            NTTSHandler.Cache.Trim();
        }, true);
    }

    private static class NTTSHandler
    {
        public static string ApiUrl = string.Empty;
        public static ISawmill? Sawmill = null;

        public static readonly HttpClient HttpClient = new();
        public static readonly TtsCache Cache = new();

        private static readonly ConcurrentDictionary<TtsCacheKey, TTSResponse> ResponsesInProgress = new();

        public static async Task<ReferenceCounter<TtsAudioData>.Handle?> ConvertTextToSpeech(string speaker, string text, TtsKind kind)
        {
            WantedCount.Inc();

            var cacheKey = new TtsCacheKey(TtsCacheKey.DefaultDivider, text, speaker, kind.ToString());

            if (Cache.TryGet(cacheKey, out var data))
            {
                Sawmill?.Debug($"Use cached sound for '{text}' speech by '{speaker}' speaker");
                return data.GetHandle();
            }

            try
            {
                if (!ResponsesInProgress.TryGetValue(cacheKey, out var response) || response.Task is null)
                {
                    response = TTSResponseManager.Rent();
                    var task = StartRequest(response);
                    response.Task = task;
                    ResponsesInProgress[cacheKey] = response;
                }

                var isSuccess = await response.Task;

                if (isSuccess)
                {
                    Cache.Cache(cacheKey, response);
                    return response.GetHandle();
                }
                else
                {
                    return null;
                }
            }
            finally
            {
                ResponsesInProgress.TryRemove(cacheKey, out _);
            }

            async Task<bool> StartRequest(TTSResponse response)
            {
                Sawmill?.Verbose($"Generate new sound for '{text}' speech by '{speaker}' speaker with kind '{kind}'");

                var reqTime = DateTime.UtcNow;
                try
                {
                    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_requestTimeout));

                    var requestUrl = $"{ApiUrl}" + ToQueryString(new NameValueCollection() {
                        { "speaker", speaker },
                        { "text", text },
                        { "ext", AudioFileExtension }
                    });

                    if (!_useFFMpegProcessing && kind == TtsKind.Radio)
                    {
                        requestUrl += "&effect=radio";
                    }

                    if (kind == TtsKind.Announce)
                    {
                        requestUrl += "&effect=announce";
                    }

                    if (!_useFFMpegProcessing && kind == TtsKind.Telepathy)
                    {
                        requestUrl += "&effect=announce";
                    }

                    var httpRequest = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                    var httpResponse = await HttpClient.SendAsync(httpRequest, cts.Token);
                    if (!httpResponse.IsSuccessStatusCode)
                    {
                        if (httpResponse.StatusCode == HttpStatusCode.TooManyRequests)
                        {
                            Sawmill?.Warning("TTS request was rate limited");
                            return false;
                        }

                        Sawmill?.Error($"TTS request returned bad status code: {httpResponse.StatusCode}");
                        return false;
                    }

                    using var memoryStream = MemoryStreamPool.GetStream("TtsStream", 1024 * 64);

                    memoryStream.Position = 0;
                    memoryStream.SetLength(0);

                    await httpResponse.Content.CopyToAsync(memoryStream, cts.Token);

                    memoryStream.Position = 0;
                    using var effectStream = await AddFFMpegEffect(memoryStream, kind, Sawmill);
                    var streamToRead = effectStream ?? memoryStream;

                    streamToRead.Position = 0;
                    TTSResponseManager.AllocBuffer(response, (int)streamToRead.Length);
                    streamToRead.ReadExactly(response.Value.Buffer, 0, response.Value.Length);

                    Sawmill?.Verbose($"Generated new sound for '{text}' speech by '{speaker}' speaker with kind '{kind}' ({response.Value.Length} bytes)");
                    RequestTimings.WithLabels("Success").Observe((DateTime.UtcNow - reqTime).TotalSeconds);
                    return true;
                }
                catch (TaskCanceledException)
                {
                    RequestTimings.WithLabels("Timeout").Observe((DateTime.UtcNow - reqTime).TotalSeconds);
                    Sawmill?.Error($"Timeout of request generation new audio for '{text}' speech by '{speaker}' speaker");
                    return false;
                }
                catch (Exception e)
                {
                    RequestTimings.WithLabels("Error").Observe((DateTime.UtcNow - reqTime).TotalSeconds);
                    Sawmill?.Error($"Failed of request generation new sound for '{text}' speech by '{speaker}' speaker\n{e}");
                    return false;
                }
            }
        }
    }
}
