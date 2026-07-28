// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Radio;
using Content.Shared.SS220.Telepathy;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.TTS;

public sealed class PlayTtsMessage : EntityEventArgs
{
    public required TtsAudioData AudioData;
    public TtsMetadata Metadata;
    public NetEntity? Source;
    public float VolumeModifier = 1f;
}

public sealed class PlayAnnounceTtsMessage : EntityEventArgs
{
    public TtsAudioData AudioData;
    public SoundSpecifier AnnouncementSound = new SoundPathSpecifier("");
    public AudioWithTTSPlayOperation PlayAudioMask = AudioWithTTSPlayOperation.PlayAll;
}

[Flags]
public enum AudioWithTTSPlayOperation : byte
{
    NotPlay = 1 << 0,
    PlayAudio = 1 << 1,
    PlayTTS = 1 << 2,

    PlayAll = PlayAudio | PlayTTS,
}

public sealed class TelepathySpokeEvent(EntityUid source, string message, EntityUid[] receivers, ProtoId<TelepathyChannelPrototype>? channel) : EntityEventArgs
{
    public readonly EntityUid Source = source;
    public readonly string Message = message;
    public readonly EntityUid[] Receivers = receivers;
    public readonly ProtoId<TelepathyChannelPrototype>? Channel = channel;
}

public sealed class TelepathyTtsSendAttemptEvent(EntityUid user, ProtoId<TelepathyChannelPrototype>? channel) : CancellableEntityEventArgs
{
    public EntityUid User = user;
    public readonly ProtoId<TelepathyChannelPrototype>? Channel = channel;
}

public sealed partial class RadioTtsSendAttemptEvent : CancellableEntityEventArgs
{
    public readonly RadioChannelPrototype Channel;

    public RadioTtsSendAttemptEvent(RadioChannelPrototype channel)
    {
        Channel = channel;
    }
}
