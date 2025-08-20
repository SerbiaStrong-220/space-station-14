using Content.Server.Chat.Systems;
using Content.Shared.Chat;
using Content.Shared.Radio;
using Content.Shared.SS220.Language.Systems;

namespace Content.Server.Radio;

[ByRefEvent]
public readonly record struct RadioReceiveEvent(string Message, EntityUid MessageSource, RadioChannelPrototype Channel, EntityUid RadioSource, MsgChatMessage ChatMsg, List<RadioEventReceiver> Receivers, LanguageMessage? LanguageMessage = null);

/// <summary>
/// Event raised on the parent entity of a headset radio when a radio message is received
/// </summary>
[ByRefEvent]
public readonly record struct HeadsetRadioReceiveRelayEvent(RadioReceiveEvent RelayedEvent);

/// <summary>
/// Use this event to cancel sending message per receiver
/// </summary>
[ByRefEvent]
public record struct RadioReceiveAttemptEvent(RadioChannelPrototype Channel, EntityUid RadioSource, EntityUid RadioReceiver)
{
    public readonly RadioChannelPrototype Channel = Channel;
    public readonly EntityUid RadioSource = RadioSource;
    public readonly EntityUid RadioReceiver = RadioReceiver;
    public bool Cancelled = false;
}

/// <summary>
/// Use this event to cancel sending message to every receiver
/// </summary>
[ByRefEvent]
public record struct RadioSendAttemptEvent(RadioChannelPrototype Channel, EntityUid RadioSource)
{
    public readonly RadioChannelPrototype Channel = Channel;
    public readonly EntityUid RadioSource = RadioSource;
    public bool Cancelled = false;
}
