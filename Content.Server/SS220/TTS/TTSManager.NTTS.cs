using Content.Shared.SS220.TTS;
using System.Collections.Specialized;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Content.Server.SS220.TTS;

public sealed partial class TTSManager
{
    private string _nttsApiUrl = string.Empty;

    public async Task<ReferenceCounter<TtsAudioData>.Handle?> SendNttsRequest(string speaker, string text, TtsKind kind)
    {
        WantedCount.Inc();

        return await StartTtsRequest(new(TTSProvider.NTTS, speaker, text, kind),
            async (request, response) =>
            {
                _sawmill.Verbose($"Generate new sound for '{text}' speech by '{speaker}' speaker with kind '{kind}'");

                var reqTime = DateTime.UtcNow;
                try
                {
                    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_timeout));

                    var requestUrl = $"{_nttsApiUrl}" + ToQueryString(new NameValueCollection() {
                    { "speaker", speaker },
                    { "text", text },
                    { "ext", AudioFileExtension }});

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

                    memoryStream.Position = 0;
                    using var effectStream = await AddFFMpegEffect(memoryStream, request.Kind);
                    var streamToRead = effectStream ?? memoryStream;

                    streamToRead.Position = 0;
                    _responseManager.AllocBuffer(response, (int)streamToRead.Length);
                    streamToRead.ReadExactly(response.Value.Buffer, 0, response.Value.Length);

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
}
