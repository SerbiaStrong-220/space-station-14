// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Serialization;

namespace Content.Shared.SS220.TTS;

[Serializable, NetSerializable]
public record struct TtsMetadata
{
    public static readonly string NullChannel = string.Empty;

    public TtsKind Kind;
    public string Subkind;

    public TtsMetadata(TtsKind kind, string? subkind)
    {
        Kind = kind;
        Subkind = subkind ?? NullChannel;
    }
}

public enum TtsKind
{
    Default = 0,
    Radio,
    Whisper,
    Announce,
    Telepathy,
}
