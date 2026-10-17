using Content.Server.SS220.TTS.FFMPegArguments;
using Content.Shared.SS220.CCVars;
using Content.Shared.SS220.TTS.Systems;
using FFMpegCore;
using FFMpegCore.Pipes;
using Microsoft.IO;
using Prometheus;
using System.Threading.Tasks;

namespace Content.Server.SS220.TTS;

public partial class TtsSystem
{
    private bool _useFFMpegProcessing = true;

    private readonly string[] _sizeSuffixes = ["B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB"];

    private readonly Histogram _ffmpegProcessEffectsTimings = Metrics.CreateHistogram("tts_ffmpeg_usage_time",
        "Milliseconds spent for ffmpeg processing (and pipe operations) on tts", new HistogramConfiguration
        {
            LabelNames = ["effect"],
            Buckets = Histogram.ExponentialBuckets(.1, 1.5, 10),
        });

    private void InitializeFFMpeg()
    {
        _cfg.OnValueChanged(CCVars220.TtsUseFFMpegProcessing, (x) => _useFFMpegProcessing = x, true);
    }

    private async Task<RecyclableMemoryStream?> AddFFMpegEffect(RecyclableMemoryStream audioDataStream, TtsKind kind)
    {
        if (!_useFFMpegProcessing)
            return null;

        var outputStream = _memoryStreamPool.GetStream("TtsFFMpegStream", audioDataStream.Length);

        var startTime = DateTime.UtcNow;
        try
        {
            await FFMpegArguments
                .FromPipeInput(new StreamPipeSource(audioDataStream))
                .OutputToPipe(new StreamPipeSink(outputStream), options => FFMpeg_GetFilterOptionsFromKind(options, kind))
                .ProcessAsynchronously();
        }
        catch (Exception e)
        {
            Log.Error($"Got exception while adding effects by ffmpeg for tts kind {kind}\n [Exception]\n{e}");
            _ffmpegProcessEffectsTimings.WithLabels("exception").Observe((DateTime.UtcNow - startTime).TotalMilliseconds);

            outputStream.Dispose();
            return null;
        }
        finally
        {
            _ffmpegProcessEffectsTimings
                .WithLabels($"{kind}/{FFMpeg_PrettyPrintBufferLength(audioDataStream.Length)}")
                .Observe((DateTime.UtcNow - startTime).TotalMilliseconds);
        }

        return outputStream;
    }

    private void FFMpeg_GetFilterOptionsFromKind(FFMpegArgumentOptions options, TtsKind kind)
    {
        switch (kind)
        {
            case TtsKind.Radio:
                options.WithAudioFilters(filterOptions =>
                {
                    filterOptions
                        .HighPass(frequency: 5e2D)
                        .LowPass(frequency: 1e4D);
                    filterOptions.Arguments
                        .Add(new CrusherFilterArgument(levelIn: 1f, levelOut: 1f, bits: 45, mix: 0, mode: "log"));
                });
                break;

            case TtsKind.Telepathy:
                options.WithAudioFilters(filterOptions =>
                {
                    filterOptions
                        .LowPass(frequency: 1e4D);
                    filterOptions.Arguments
                        .Add(new EchoFilterArgument());
                });
                break;
        }

        options.ForceFormat(AudioFileExtension);
    }

    private string FFMpeg_PrettyPrintBufferLength(long length, int decimalPlaces = 0)
    {
        if (length == 0)
            return string.Format("{0:n" + decimalPlaces + "} bytes", 0);

        var magnitude = (int)Math.Log(length, 1024);
        var scaledValue = length / Math.Pow(1024, magnitude);

        return string.Format("{0:n" + decimalPlaces + "} {1}", scaledValue, _sizeSuffixes[magnitude]);
    }
}
