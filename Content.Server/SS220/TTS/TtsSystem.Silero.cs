using Content.Shared.SS220.CCVars;
using Content.Shared.SS220.TTS;
using Robust.Shared.Configuration;
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

public partial class TtsSystem
{
    private void InitializeSilero()
    {
        RegisterProviderHandler(TtsProvider.Silero, new TtsSileroHandler(this, _cfg));
    }

    private sealed class TtsSileroHandler : TtsProviderHandler
    {
        private string _apiUrl = string.Empty;
        private string _apiToken = string.Empty;

        private readonly HttpClient _httpClient = new();

        private readonly ConcurrentDictionary<TtsCacheKey, TtsResponse> _responsesInProgress = new();

        protected override string SawmillName => "silero_handler";

        public TtsSileroHandler(TtsSystem ttsSystem, IConfigurationManager cfg) : base(ttsSystem, cfg)
        {
            ConfigurationManager.OnValueChanged(CCVars220.TTSSileroApiUrl, v => _apiUrl = v, true);
            ConfigurationManager.OnValueChanged(CCVars220.TTSSileroApiToken, v =>
            {
                _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", v);
                _apiToken = v;
            }, true);
            ConfigurationManager.OnValueChanged(CCVars220.TTSSileroMaxCache, v =>
            {
                Cache.Limit = v;
                Cache.Trim();
            }, true);
        }

        public override async Task<TtsResponse.Reference?> ConvertTextToSpeech(string text, string speaker, TtsKind kind)
        {
            if (string.IsNullOrEmpty(_apiUrl) || string.IsNullOrEmpty(_apiToken))
                return null;

            WantedCount.Inc();

            var cacheKey = new TtsCacheKey(TtsCacheKey.DefaultDivider, text, speaker, kind.ToString());

            if (Cache.TryGet(cacheKey, out var data))
            {
                Log.Debug($"Use cached sound for '{text}' speech by '{speaker}' speaker");
                return data.Value.GetReference();
            }

            try
            {
                if (!_responsesInProgress.TryGetValue(cacheKey, out var response) || response.Task is null)
                {
                    response = TtsResponseManager.Rent();
                    var task = StartRequest(text, speaker, kind, response);
                    response.Task = task;
                    _responsesInProgress[cacheKey] = response;
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
                _responsesInProgress.TryRemove(cacheKey, out _);
            }
        }

        private async Task<bool> StartRequest(string text, string speaker, TtsKind kind, TtsResponse response)
        {
            Log.Verbose($"Generate new sound for '{text}' speech by '{speaker}' speaker with kind '{kind}'");
            var body = new SileroHttpRequestBody()
            {
                ApiToken = _apiToken,
                Text = text,
                Speaker = speaker
            };

            var reqTime = DateTime.UtcNow;
            try
            {
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(TtsSystem._requestTimeout));

                var httpResponse = await _httpClient.PostAsJsonAsync(_apiUrl, body, cts.Token);
                if (!httpResponse.IsSuccessStatusCode)
                {
                    if (httpResponse.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        Log.Warning("Silero TTS request was rate limited");
                        return false;
                    }

                    Log.Error($"Silero TTS request returned bad status code: {httpResponse.StatusCode}");
                    return false;
                }

                using var jsonStream = TtsSystem._memoryStreamPool.GetStream("SileroJsonStream", 1024 * 16);
                jsonStream.Position = 0;
                jsonStream.SetLength(0);

                await httpResponse.Content.CopyToAsync(jsonStream, cts.Token);
                jsonStream.Position = 0;

                var json = await JsonSerializer.DeserializeAsync<SileroHttpResponseBody>(jsonStream, cancellationToken: cts.Token);

                if (json.Results.Count == 0)
                {
                    Log.Error("Silero response missing results");
                    return false;
                }

                var audioBase64 = json.Results.First().Audio;
                if (string.IsNullOrEmpty(audioBase64))
                {
                    Log.Error("Silero response missing audio data");
                    return false;
                }

                var soundData = Convert.FromBase64String(audioBase64);

                using var audioStream = TtsSystem._memoryStreamPool.GetStream("SileroAudioStream", soundData.Length);
                audioStream.Position = 0;
                audioStream.SetLength(0);

                await audioStream.WriteAsync(soundData, cts.Token);
                audioStream.Position = 0;

                using var effectStream = await TtsSystem.AddFFMpegEffect(audioStream, kind);
                var streamToRead = effectStream ?? audioStream;

                streamToRead.Position = 0;
                TtsResponseManager.AllocBuffer(response, (int)streamToRead.Length);
                await streamToRead.ReadExactlyAsync(response.Value.Buffer, 0, response.Value.Length, cts.Token);

                Log.Verbose($"Generated new sound for '{text}' speech by '{speaker}' speaker with kind '{kind}' ({response.Value.Length} bytes)");
                RequestTimings.WithLabels("Success").Observe((DateTime.UtcNow - reqTime).TotalSeconds);
                return true;
            }
            catch (TaskCanceledException)
            {
                RequestTimings.WithLabels("Timeout").Observe((DateTime.UtcNow - reqTime).TotalSeconds);
                Log.Error($"Timeout of request generation new audio for '{text}' speech by '{speaker}' speaker");
                return false;
            }
            catch (Exception e)
            {
                RequestTimings.WithLabels("Error").Observe((DateTime.UtcNow - reqTime).TotalSeconds);
                Log.Error($"Failed of request generation new sound for '{text}' speech by '{speaker}' speaker\n{e}");
                return false;
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
