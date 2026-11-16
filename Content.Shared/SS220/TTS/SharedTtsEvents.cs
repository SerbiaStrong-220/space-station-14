// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Radio;
using Content.Shared.SS220.Telepathy;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.TTS;

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
