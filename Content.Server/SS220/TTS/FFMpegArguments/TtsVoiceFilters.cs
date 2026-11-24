// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using FFMpegCore.Arguments;

namespace Content.Server.SS220.TTS.FFMPegArguments;

/// <summary>
/// An audio filter taken as is: <c>key=value</c>, where the key is the ffmpeg filter name.
/// </summary>
public sealed class RawFilterArgument(string key, string value) : IAudioFilterArgument
{
    public string Key { get; } = key;
    public string Value { get; } = value;
}

/// <summary>
/// ffmpeg filters applied to the voice of a TTS provider, before the effects of the TTS kind (radio, telepathy).
/// </summary>
public static class TtsVoiceFilters
{
    /// <summary>
    /// Makes the Silero voice less hoarse: cuts the rumble and adds high frequencies.
    /// </summary>
    public static readonly IReadOnlyList<IAudioFilterArgument> Silero =
    [
        new RawFilterArgument("highpass", "f=80"),
        new RawFilterArgument("treble", "g=4:f=4500"),
        new RawFilterArgument("equalizer", "f=9000:t=q:w=1:g=3"),
        new RawFilterArgument("alimiter", "limit=0.95"),
    ];
}
