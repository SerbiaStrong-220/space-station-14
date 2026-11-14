using Content.Server.SS220.TTS.FFMPegArguments;
using Content.Shared.SS220.CCVars;
using Content.Shared.SS220.TTS.Systems;
using FFMpegCore;
using FFMpegCore.Arguments;
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

    private IReadOnlyList<IAudioFilterArgument> FFMpeg_GetProviderFilters(TtsProvider provider)
    {
        return provider == TtsProvider.Silero ? TtsVoiceFilters.Silero : [];
    }

    /// <summary>
    /// Adds the effects of the <paramref name="kind"/> and the <paramref name="provider"/> filters to the audio.
    /// </summary>
    /// <returns>The processed audio or null if the audio should be used as is.</returns>
    private async Task<RecyclableMemoryStream?> AddFFMpegEffect(RecyclableMemoryStream audioDataStream, TtsKind kind, TtsProvider provider)
    {
        if (!_useFFMpegProcessing)
            return null;

        var providerFilters = FFMpeg_GetProviderFilters(provider);

        // Nothing to apply, so avoid the second lossy encoding.
        // The client plays only Ogg Vorbis, so any other format still has to be converted.
        if (providerFilters.Count == 0 && !FFMpeg_HasKindFilters(kind) && FFMpeg_IsOggVorbis(audioDataStream))
            return null;

        var outputStream = _memoryStreamPool.GetStream("TtsFFMpegStream", audioDataStream.Length);

        var startTime = DateTime.UtcNow;
        try
        {
            await FFMpegArguments
                .FromPipeInput(new StreamPipeSource(audioDataStream))
                .OutputToPipe(new StreamPipeSink(outputStream), options => FFMpeg_GetFilterOptions(options, kind, providerFilters))
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

    private static bool FFMpeg_HasKindFilters(TtsKind kind)
    {
        return kind is TtsKind.Radio or TtsKind.Telepathy;
    }

    private static bool FFMpeg_IsOggVorbis(RecyclableMemoryStream audioDataStream)
    {
        Span<byte> header = stackalloc byte[TtsAudioFormat.HeaderLength];

        var position = audioDataStream.Position;
        audioDataStream.Position = 0;
        var read = audioDataStream.Read(header);
        audioDataStream.Position = position;

        return TtsAudioFormat.IsOggVorbis(header[..read]);
    }

    private void FFMpeg_GetFilterOptions(FFMpegArgumentOptions options, TtsKind kind, IReadOnlyList<IAudioFilterArgument> providerFilters)
    {
        if (providerFilters.Count > 0 || FFMpeg_HasKindFilters(kind))
        {
            options.WithAudioFilters(filterOptions =>
            {
                foreach (var filter in providerFilters)
                    filterOptions.Arguments.Add(filter);

                switch (kind)
                {
                    case TtsKind.Radio:
                        filterOptions
                            .HighPass(frequency: 5e2D)
                            .LowPass(frequency: 1e4D);
                        filterOptions.Arguments
                            .Add(new CrusherFilterArgument(levelIn: 1f, levelOut: 1f, bits: 45, mix: 0, mode: "log"));
                        break;

                    case TtsKind.Telepathy:
                        filterOptions
                            .LowPass(frequency: 1e4D);
                        filterOptions.Arguments
                            .Add(new EchoFilterArgument());
                        break;
                }
            });
        }

        options.ForceFormat(AudioFileExtension);
    }
}
