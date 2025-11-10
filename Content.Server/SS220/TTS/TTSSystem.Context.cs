// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Chat.Systems;
using Content.Shared.SS220.TTS;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.SS220.TTS;

public static class TtsContextMaker
{
    #region Speak context

    public static TtsContext New(IEntityManager entityManager, EntitySpokeEvent args)
    {
        return new()
        {
            ChannelPrototype = args.Channel?.ID,
            IsRadio = args.IsRadio,
            SpeakerContext = New(entityManager, args.Source)
        };
    }

    public static TtsContext New(IEntityManager entityManager, RadioSpokeEvent args)
    {
        return new()
        {
            ChannelPrototype = args.Channel.ID,
            IsRadio = true,
            SpeakerContext = New(entityManager, args.Source)
        };
    }

    public static TtsContext New(IEntityManager entityManager, TelepathySpokeEvent args)
    {
        return new()
        {
            ChannelPrototype = args.Channel,
            IsRadio = true,
            SpeakerContext = New(entityManager, args.Source)
        };
    }
    #endregion

    #region Speaker context

    public static TtsSpeakerContext New(IEntityManager entityManager, EntityUid speaker)
    {
        entityManager.System<TTSContextSystem>().TryGetVoiceID(speaker, out var voiceId);

        return new()
        {
            Speaker = speaker,
            NetSpeaker = entityManager.GetNetEntity(speaker),
            InternalVoiceId = voiceId
        };
    }

    #endregion
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
    private static readonly ProtoId<TTSVoicePrototype> FallbackVoiceId = "father_grigori";

    public EntityUid Speaker { get; init; }
    public NetEntity NetSpeaker { get; init; }
    public ProtoId<TTSVoicePrototype> VoiceId
    {
        get
        {
            DebugTools.Assert(InternalVoiceId.HasValue);
            return InternalVoiceId ?? FallbackVoiceId;
        }
    }

    public ProtoId<TTSVoicePrototype>? InternalVoiceId { init; private get; }

    public readonly bool Valid => InternalVoiceId is not null;
}
