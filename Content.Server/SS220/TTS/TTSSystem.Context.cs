// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Chat.Systems;
using Content.Shared.Chat;
using Content.Shared.SS220.TTS;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.TTS;

public partial class TTSSystem
{
    public TtsContext GetContext(EntitySpokeEvent args)
    {
        return new()
        {
            ChannelPrototype = args.Channel?.ID + args.Frequency?.ToString(),
            IsRadio = args.IsRadio,
            SpeakerContext = GetSpeakerContext(args.Source),
        };
    }

    public TtsContext GetContext(RadioSpokeEvent args)
    {
        return new()
        {
            ChannelPrototype = args.Channel.ID + args.Frequency?.ToString(),
            IsRadio = true,
            SpeakerContext = GetSpeakerContext(args.Source)
        };
    }

    public TtsContext GetContext(TelepathySpokeEvent args)
    {
        return new()
        {
            ChannelPrototype = args.Channel,
            IsRadio = true,
            SpeakerContext = GetSpeakerContext(args.Source)
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

public readonly record struct TtsContext
{
    public bool IsRadio { get; init; }
    public string? ChannelPrototype { init; get; }
    public TtsSpeakerContext SpeakerContext { get; init; }

    public bool Valid => SpeakerContext.Valid;
}

public readonly record struct TtsSpeakerContext
{
    public required EntityUid Speaker { get; init; }
    public required NetEntity NetSpeaker { get; init; }
    public required ProtoId<TTSVoicePrototype>? VoiceId { get; init; }

    public readonly bool Valid => VoiceId is not null;
}
