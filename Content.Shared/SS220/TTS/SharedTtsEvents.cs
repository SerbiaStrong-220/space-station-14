// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Radio;
using Content.Shared.SS220.Telepathy;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.TTS;

[Serializable, NetSerializable]
public sealed class PlayTtsMessage(params PlayTtsMessageData[] datas) : EntityEventArgs
{
    public List<PlayTtsMessageData> Datas = [.. datas];

    public PlayTtsMessage() : this([]) { }
}

[Serializable, NetSerializable]
public struct PlayTtsMessageData
{
    public required ITtsData TtsData;
    public required TtsMetadata TtsMetadata;
}

[Serializable, NetSerializable]
public sealed class RequestTtsVoiceTestEvent(ProtoId<TtsVoicePrototype> voiceId) : EntityEventArgs
{
    public readonly ProtoId<TtsVoicePrototype> VoiceId = voiceId;
}

[Serializable, NetSerializable]
public sealed class TtsClearAllQueuesMessage : EntityEventArgs { }

[Serializable, NetSerializable]
public sealed class ReceiveTtsCVarChanged(bool value) : EntityEventArgs
{
    public bool Value { get; init; } = value;
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

public sealed partial class RadioTtsSendAttemptEvent(RadioChannelPrototype channel) : CancellableEntityEventArgs
{
    public readonly RadioChannelPrototype Channel = channel;
}
