// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Serialization;
using System.Linq;

namespace Content.Shared.SS220.TTS;

[Serializable, NetSerializable]
public struct TtsMetadata
{
    public required TtsKind Kind;

    public TtsProvider? Provider;
    public string? ChannelPrototype;
    public NetEntity? Source;
    public NetEntity? PlayEntity;
}

[Serializable, NetSerializable]
public record struct SharedTtsMetadata(TtsProvider Provider, TtsKind Kind, string? ChannelPrototype = null, NetEntity? Source = null);

public enum TtsKind
{
    Say = 0,
    Radio,
    Whisper,
    Announce,
    Telepathy,
    VoiceTest
}

public struct TtsCacheKey()
{
    public const string DefaultDivider = "/";

    public readonly string Key = string.Empty;

    public TtsCacheKey(string key) : this()
    {
        Key = key;
    }

    public TtsCacheKey(string? divider, params string?[] keys) : this()
    {
        Key = string.Join(divider, keys.Where(x => !string.IsNullOrEmpty(x)));
    }

    public readonly TtsCacheKey With(string? info, string divider = DefaultDivider)
    {
        if (info == null)
            return this;

        var newKey = Key;
        if (!string.IsNullOrEmpty(newKey))
            newKey += divider + info;
        else
            newKey += info;

        return new TtsCacheKey(newKey);
    }

    public static TtsCacheKey New(string? text = null, TtsProvider? provider = null, string? speaker = null, TtsKind? kind = null)
    {
        var key = new TtsCacheKey()
            .With(text)
            .With(provider?.ToString())
            .With(speaker)
            .With(kind?.ToString());

        return key;
    }
}
