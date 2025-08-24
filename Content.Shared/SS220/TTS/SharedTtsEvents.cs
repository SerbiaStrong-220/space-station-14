// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Radio;
using Content.Shared.SS220.Telepathy;

namespace Content.Shared.SS220.TTS;

public sealed class TelepathySpokeEvent(EntityUid source, string message, EntityUid[] receivers, TelepathyChannelPrototype? channel) : EntityEventArgs
{
    public readonly EntityUid Source = source;
    public readonly string Message = message;
    public readonly EntityUid[] Receivers = receivers;
    public readonly TelepathyChannelPrototype? Channel = channel;
}

public sealed class TelepathyTtsSendAttemptEvent(EntityUid user, TelepathyChannelPrototype? channel) : CancellableEntityEventArgs
{
    public EntityUid User = user;
    public readonly TelepathyChannelPrototype? Channel = channel;
}

public sealed partial class RadioTtsSendAttemptEvent : CancellableEntityEventArgs
{
    public readonly RadioChannelPrototype Channel;

    public RadioTtsSendAttemptEvent(RadioChannelPrototype channel)
    {
        Channel = channel;
    }
}
