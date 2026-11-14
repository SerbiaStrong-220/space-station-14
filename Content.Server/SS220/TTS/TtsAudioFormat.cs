// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
namespace Content.Server.SS220.TTS;

public static class TtsAudioFormat
{
    /// <summary>
    /// The number of the first bytes of the audio that is enough for <see cref="IsOggVorbis"/>.
    /// </summary>
    public const int HeaderLength = 64;

    /// <summary>
    /// Checks the beginning of the audio: an Ogg page whose first packet is the Vorbis identification header.
    /// The client plays only Ogg Vorbis, so e.g. Ogg Opus is not suitable.
    /// </summary>
    public static bool IsOggVorbis(ReadOnlySpan<byte> header)
    {
        return header.StartsWith("OggS"u8) && header.IndexOf("vorbis"u8) >= 0;
    }
}
