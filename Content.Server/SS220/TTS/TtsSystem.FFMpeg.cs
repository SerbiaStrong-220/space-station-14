using Content.Server.SS220.TTS.FFMPegArguments;
using Content.Shared.SS220.CCVars;
using Content.Shared.SS220.TTS.Systems;
using FFMpegCore;
using FFMpegCore.Pipes;
using Microsoft.IO;
using System.Threading.Tasks;

namespace Content.Server.SS220.TTS;

public partial class TtsSystem
{
    private bool _useFFMpegProcessing = true;

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
            FFMpegProcessingTimings.WithLabels(kind.ToString(), "error").Observe((DateTime.UtcNow - startTime).TotalSeconds);

            outputStream.Dispose();
            return null;
        }

        FFMpegProcessingTimings.WithLabels(kind.ToString(), "success").Observe((DateTime.UtcNow - startTime).TotalSeconds);
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
}
