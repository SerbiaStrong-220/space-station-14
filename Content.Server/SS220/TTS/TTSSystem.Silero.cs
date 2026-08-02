using Content.Shared.SS220.CCVars;
using Content.Shared.SS220.TTS;
using Microsoft.IO;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Content.Server.SS220.TTS;

public partial class TTSSystem
{
    private static readonly HttpClient HttpClient = new();
    private static readonly RecyclableMemoryStreamManager MemoryStreamPool = new();

    private void InitializeSilero()
    {
        SileroTTSHandler.Sawmill = Logger.GetSawmill("silero_tts");

        _cfg.OnValueChanged(CCVars220.TTSSileroApiUrl, v => SileroTTSHandler.ApiUrl = v, true);
        _cfg.OnValueChanged(CCVars220.TTSSileroApiToken, v =>
        {
            SileroTTSHandler.HttpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", v);
            SileroTTSHandler.ApiToken = v;
        }, true);
        _cfg.OnValueChanged(CCVars220.TTSSileroMaxCache, v =>
        {
            SileroTTSHandler.Cache.Limit = v;
            SileroTTSHandler.Cache.Trim();
        }, true);
    }

    private static class SileroTTSHandler
    {
        public static string ApiUrl = string.Empty;
        public static string ApiToken = string.Empty;
        public static ISawmill? Sawmill = null;

        public static readonly HttpClient HttpClient = new();
        public static readonly TtsCache Cache = new();

        private static readonly ConcurrentDictionary<TtsCacheKey, TtsResponse> ResponsesInProgress = new();

        public static async Task<TtsResponse.Reference?> ConvertTextToSpeech(string speaker, string text, TtsKind kind)
        {
            WantedCount.Inc();

            var cacheKey = new TtsCacheKey(TtsCacheKey.DefaultDivider, text, speaker, kind.ToString());

            if (Cache.TryGet(cacheKey, out var data))
            {
                Sawmill?.Debug($"Use cached sound for '{text}' speech by '{speaker}' speaker");
                return data.Value.GetReference();
            }

            try
            {
                if (!ResponsesInProgress.TryGetValue(cacheKey, out var response) || response.Task is null)
                {
                    response = TtsResponseManager.Rent();
                    var task = StartRequest(response);
                    response.Task = task;
                    ResponsesInProgress[cacheKey] = response;
                }

                var isSuccess = await response.Task;

                if (isSuccess)
                {
                    Cache.Cache(cacheKey, response);
                    return response.GetReference();
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

            async Task<bool> StartRequest(TtsResponse response)
            {
                Sawmill?.Verbose($"Generate new sound for '{text}' speech by '{speaker}' speaker with kind '{kind}'");
                var body = new SileroHttpRequestBody()
                {
                    ApiToken = ApiToken,
                    Text = text,
                    Speaker = speaker
                };

                var reqTime = DateTime.UtcNow;
                try
                {
                    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_requestTimeout));

                    var httpResponse = await HttpClient.PostAsJsonAsync(ApiUrl, body, cts.Token);
                    if (!httpResponse.IsSuccessStatusCode)
                    {
                        if (httpResponse.StatusCode == HttpStatusCode.TooManyRequests)
                        {
                            Sawmill?.Warning("Silero TTS request was rate limited");
                            return false;
                        }

                        Sawmill?.Error($"Silero TTS request returned bad status code: {httpResponse.StatusCode}");
                        return false;
                    }

                    using var jsonStream = MemoryStreamPool.GetStream("SileroJsonStream", 1024 * 16);
                    jsonStream.Position = 0;
                    jsonStream.SetLength(0);

                    await httpResponse.Content.CopyToAsync(jsonStream, cts.Token);
                    jsonStream.Position = 0;

                    var json = await JsonSerializer.DeserializeAsync<SileroHttpResponseBody>(jsonStream, cancellationToken: cts.Token);

                    if (json.Results.Count == 0)
                    {
                        Sawmill?.Error("Silero response missing results");
                        return false;
                    }

                    var audioBase64 = json.Results.First().Audio;
                    if (string.IsNullOrEmpty(audioBase64))
                    {
                        Sawmill?.Error("Silero response missing audio data");
                        return false;
                    }

                    var soundData = Convert.FromBase64String(audioBase64);

                    using var audioStream = MemoryStreamPool.GetStream("SileroAudioStream", soundData.Length);
                    audioStream.Position = 0;
                    audioStream.SetLength(0);

                    await audioStream.WriteAsync(soundData, cts.Token);
                    audioStream.Position = 0;

                    using var effectStream = await AddFFMpegEffect(audioStream, kind, Sawmill);
                    var streamToRead = effectStream ?? audioStream;

                    streamToRead.Position = 0;
                    TtsResponseManager.AllocBuffer(response, (int)streamToRead.Length);
                    await streamToRead.ReadExactlyAsync(response.Value.Buffer, 0, response.Value.Length, cts.Token);

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

        private struct SileroHttpRequestBody()
        {
            [JsonPropertyName("api_token")]
            public string ApiToken { get; set; } = "";

            [JsonPropertyName("text")]
            public string Text { get; set; } = "";

            [JsonPropertyName("speaker")]
            public string Speaker { get; set; } = "";

            [JsonPropertyName("ssml")]
            public bool SSML { get; private set; } = true;

            [JsonPropertyName("word_ts")]
            public bool WordTS { get; private set; } = false;

            [JsonPropertyName("put_accent")]
            public bool PutAccent { get; private set; } = true;

            [JsonPropertyName("put_yo")]
            public bool PutYo { get; private set; } = false;

            [JsonPropertyName("sample_rate")]
            public int SampleRate { get; private set; } = 24000;

            [JsonPropertyName("format")]
            public string Format { get; private set; } = AudioFileExtension;
        }

        private struct SileroHttpResponseBody
        {
            [JsonPropertyName("results")]
            public List<SileroVoiceResult> Results { get; set; }

            [JsonPropertyName("original_sha1")]
            public string Hash { get; set; }

            public struct SileroVoiceResult
            {
                [JsonPropertyName("audio")]
                public string Audio { get; set; }
            }
        }
    }
}
