namespace Content.Server.SS220.TTS;

public partial class TTSSystem
{
    private static string ToSsmlText(string text, SoundTraits traits = SoundTraits.None)
    {
        var result = text;
        if (traits.HasFlag(SoundTraits.RateFast))
            result = $"<prosody rate=\"fast\">{result}</prosody>";
        if (traits.HasFlag(SoundTraits.PitchVerylow))
            result = $"<prosody pitch=\"x-low\">{result}</prosody>";
        return $"<speak>{result}</speak>";
    }

    [Flags]
    private enum SoundTraits : ushort
    {
        None = 0,
        RateFast = 1 << 0,
        PitchVerylow = 1 << 1,
    }
}
