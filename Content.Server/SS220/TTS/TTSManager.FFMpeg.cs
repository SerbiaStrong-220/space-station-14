// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.SS220.TTS.FFMPegArguments;
using Content.Shared.SS220.CCVars;
using Content.Shared.SS220.TTS;
using FFMpegCore;
using FFMpegCore.Pipes;
using Microsoft.IO;
using Prometheus;
using System.Threading.Tasks;

namespace Content.Server.SS220.TTS;

// ReSharper disable once InconsistentNaming
public sealed partial class TTSManager
{
    private bool _useFFMpegProcessing = true;

    private void InitializeFFMpeg()
    {
        _cfg.OnValueChanged(CCVars220.UseFFMpegProcessing, (x) => _useFFMpegProcessing = x, true);
    }

    private static readonly Histogram ProcessEffectsTimings = Metrics.CreateHistogram("tts_ffmpeg_usage_time",
        "Milliseconds spent for ffmpeg processing (and pipe operations) on tts", new HistogramConfiguration
        {
            LabelNames = new[] { "effect" },
            Buckets = Histogram.ExponentialBuckets(.1, 1.5, 10),
        });


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
                .OutputToPipe(new StreamPipeSink(outputStream), options => GetFilterOptionsFromKind(options, kind))
                .ProcessAsynchronously();
        }
        catch (Exception e)
        {
            _sawmill.Error($"Got exception while adding effects by ffmpeg for tts kind {kind}\n [Exception]\n{e}");
            ProcessEffectsTimings.WithLabels("exception").Observe((DateTime.UtcNow - startTime).TotalMilliseconds);

            outputStream.Dispose();
            return null;
        }
        finally
        {
            ProcessEffectsTimings
                .WithLabels($"{kind}/{PrettyPrintBufferLength(audioDataStream.Length)}")
                .Observe((DateTime.UtcNow - startTime).TotalMilliseconds);
        }

        return outputStream;
    }

    private void GetFilterOptionsFromKind(FFMpegArgumentOptions options, TtsKind kind)
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

    private static readonly string[] SizeSuffixes =
    ["B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB"];

    private string PrettyPrintBufferLength(long length, int decimalPlaces = 0)
    {
        if (length == 0)
            return string.Format("{0:n" + decimalPlaces + "} bytes", 0);

        int magnitude = (int)Math.Log(length, 1024);
        double scaledValue = (double)length / Math.Pow(1024, magnitude);

        return string.Format("{0:n" + decimalPlaces + "} {1}", scaledValue, SizeSuffixes[magnitude]);
    }
}
