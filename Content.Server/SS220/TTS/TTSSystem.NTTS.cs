using Content.Shared.SS220.CCVars;
using Content.Shared.SS220.TTS;
using Robust.Shared.Configuration;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Content.Server.SS220.TTS;

public partial class TtsSystem
{
    private void InitializeNTTS()
    {
        RegisterProviderHandler(TtsProvider.NTTS, new TtsNTTSHandler(this, _cfg));
    }

    private sealed class TtsNTTSHandler : TtsProviderHandler
    {
        private string _apiUrl = string.Empty;

        private readonly HttpClient _httpClient = new();

        private readonly ConcurrentDictionary<TtsCacheKey, TtsResponse> _responsesInProgress = new();

        protected override string SawmillName => "ntts_handler";

        public TtsNTTSHandler(TtsSystem ttsSystem, IConfigurationManager cfg) : base(ttsSystem, cfg)
        {
            ConfigurationManager.OnValueChanged(CCVars220.NTTSApiUrl, v => _apiUrl = v, true);
            ConfigurationManager.OnValueChanged(CCVars220.NTTSMaxCache, v =>
            {
                Cache.Limit = v;
                Cache.Trim();
            }, true);
        }

        public override async Task<TtsResponse.Reference?> ConvertTextToSpeech(string text, string speaker, TtsKind kind)
        {
            if (string.IsNullOrEmpty(_apiUrl))
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

            var reqTime = DateTime.UtcNow;
            try
            {
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(TtsSystem._requestTimeout));

                var requestUrl = $"{_apiUrl}" + ToQueryString(new NameValueCollection() {
                        { "speaker", speaker },
                        { "text", text },
                        { "ext", AudioFileExtension }
                    });

                if (!TtsSystem._useFFMpegProcessing && kind == TtsKind.Radio)
                {
                    requestUrl += "&effect=radio";
                }

                if (kind == TtsKind.Announce)
                {
                    requestUrl += "&effect=announce";
                }

                if (!TtsSystem._useFFMpegProcessing && kind == TtsKind.Telepathy)
                {
                    requestUrl += "&effect=announce";
                }

                var httpRequest = new HttpRequestMessage(HttpMethod.Get, requestUrl);
                var httpResponse = await _httpClient.SendAsync(httpRequest, cts.Token);
                if (!httpResponse.IsSuccessStatusCode)
                {
                    if (httpResponse.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        Log.Warning("TTS request was rate limited");
                        return false;
                    }

                    Log.Error($"TTS request returned bad status code: {httpResponse.StatusCode}");
                    return false;
                }

                using var memoryStream = TtsSystem._memoryStreamPool.GetStream("TtsStream", 1024 * 64);

                memoryStream.Position = 0;
                memoryStream.SetLength(0);

                await httpResponse.Content.CopyToAsync(memoryStream, cts.Token);

                memoryStream.Position = 0;
                using var effectStream = await TtsSystem.AddFFMpegEffect(memoryStream, kind);
                var streamToRead = effectStream ?? memoryStream;

                streamToRead.Position = 0;
                TtsResponseManager.AllocBuffer(response, (int)streamToRead.Length);
                streamToRead.ReadExactly(response.Value.Buffer, 0, response.Value.Length);

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
    }
}
