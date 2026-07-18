using Content.Shared.SS220.TTS;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Content.Server.SS220.TTS;

public sealed partial class TTSManager
{
    private string _sileroApiUrl = string.Empty;
    private string _sileroApiToken = string.Empty;

    private async Task<ReferenceCounter<TtsAudioData>.Handle?> SendSileroRequest(string speaker, string text, TtsKind kind)
    {
        WantedCount.Inc();

        return await StartTtsRequest(new(TTSProvider.Silero, speaker, text, kind),
           async (request, response) =>
           {
               _sawmill.Verbose($"Generate new sound for '{text}' speech by '{speaker}' speaker with kind '{kind}'");

               var body = new SileroHttpRequestBody()
               {
                   ApiToken = _sileroApiToken,
                   Text = text,
                   Speaker = speaker
               };

               var reqTime = DateTime.UtcNow;
               try
               {
                   var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_timeout));

                   var httpResponse = await _httpClient.PostAsJsonAsync(_sileroApiUrl, body, cts.Token);
                   if (!httpResponse.IsSuccessStatusCode)
                   {
                       if (httpResponse.StatusCode == HttpStatusCode.TooManyRequests)
                       {
                           _sawmill.Warning("Silero TTS request was rate limited");
                           return false;
                       }

                       _sawmill.Error($"Silero TTS request returned bad status code: {httpResponse.StatusCode}");
                       return false;
                   }

                   using var jsonStream = _memoryStreamPool.GetStream("SileroJsonStream", 1024 * 16);
                   jsonStream.Position = 0;
                   jsonStream.SetLength(0);

                   await httpResponse.Content.CopyToAsync(jsonStream, cts.Token);
                   jsonStream.Position = 0;

                   var json = await JsonSerializer.DeserializeAsync<SileroHttpResponseBody>(jsonStream, cancellationToken: cts.Token);

                   if (json.Results.Count == 0)
                   {
                       _sawmill.Error("Silero response missing results");
                       return false;
                   }

                   var audioBase64 = json.Results.First().Audio;
                   if (string.IsNullOrEmpty(audioBase64))
                   {
                       _sawmill.Error("Silero response missing audio data");
                       return false;
                   }

                   var soundData = Convert.FromBase64String(audioBase64);

                   using var audioStream = _memoryStreamPool.GetStream("SileroAudioStream", soundData.Length);
                   audioStream.Position = 0;
                   audioStream.SetLength(0);

                   await audioStream.WriteAsync(soundData, cts.Token);
                   audioStream.Position = 0;

                   using var effectStream = await AddFFMpegEffect(audioStream, kind);
                   var streamToRead = effectStream ?? audioStream;

                   streamToRead.Position = 0;
                   _responseManager.AllocBuffer(response, (int)streamToRead.Length);
                   await streamToRead.ReadExactlyAsync(response.Value.Buffer, 0, response.Value.Length, cts.Token);

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
        public string Format { get; private set; } = "ogg";
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
