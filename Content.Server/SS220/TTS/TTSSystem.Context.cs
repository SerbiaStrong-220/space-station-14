// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Chat.Systems;
using Content.Shared.Chat;
using Content.Shared.SS220.TTS;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Server.SS220.TTS;

public partial class TTSSystem
{
    public TtsContext GetContext(RadioSpokeEvent args)
    {
        return new()
        {
            SpeakerContext = GetSpeakerContext(args.Source),
            IsRadio = true,
            ChannelPrototype = args.Channel.ID + args.Frequency?.ToString()
        };
    }

    public TtsContext GetContext(TelepathySpokeEvent args)
    {
        return new()
        {
            SpeakerContext = GetSpeakerContext(args.Source),
            IsRadio = true,
            ChannelPrototype = args.Channel
        };
    }

    private ServerTtsMetadata GetDefaultMeta(EntityUid uid)
    {
        return new() { SpeakerMeta = GetSpeakerMeta(uid) };
    }

    private TtsSpeakerMetadata GetSpeakerMeta(EntityUid uid)
    {
        TryGetVoiceId(uid, out var voiceId);

        return new()
        {
            Speaker = uid,
            NetSpeaker = GetNetEntity(uid),
            VoiceId = voiceId
        };
    }

    private TtsSpeakerContext GetSpeakerContext(EntityUid speaker)
    {
        TryGetVoiceId(speaker, out var voiceId);

        return new()
        {
            Speaker = speaker,
            NetSpeaker = GetNetEntity(speaker),
            VoiceId = voiceId
        };
    }
}

public struct ServerTtsMetadata
{
    public required TtsSpeakerMetadata SpeakerMeta;
    public TtsKind Kind;
    public string? ChannelPrototype;

    public readonly bool Valid => SpeakerMeta.Valid;

    public readonly SharedTtsMetadata ToSharedMetadata()
    {
        return new SharedTtsMetadata(Kind, ChannelPrototype);
    }
}

public struct TtsSpeakerMetadata
{
    public required EntityUid Speaker;
    public required NetEntity NetSpeaker;
    public required ProtoId<TTSVoicePrototype>? VoiceId;

    public readonly bool Valid => VoiceId is not null;
}

public readonly record struct TtsContext
{
    public required TtsSpeakerContext SpeakerContext { get; init; }
    public bool IsRadio { get; init; }
    public string? ChannelPrototype { get; init; }

    public bool Valid => SpeakerContext.Valid;
}

public readonly record struct TtsSpeakerContext
{
    public required EntityUid Speaker { get; init; }
    public required NetEntity NetSpeaker { get; init; }
    public required ProtoId<TTSVoicePrototype>? VoiceId { get; init; }

    public readonly bool Valid => VoiceId is not null;
}
