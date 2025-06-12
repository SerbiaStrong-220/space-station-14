// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Telepathy;

/// <summary>
/// This is used for giving telepathy ability
/// </summary>
[RegisterComponent]
[NetworkedComponent]
public sealed partial class TelepathyComponent : Component
{
    [DataField("canSend", required: true)]
    public bool CanSend;

    [DataField]
    public ProtoId<TelepathyChannelPrototype>? TelepathyChannelPrototype;

    [DataField]
    public bool ReceiveAllChannels = false;
}

public sealed partial class TelepathySendEvent : InstantActionEvent
{
    public string Message { get; init; }

    public TelepathySendEvent(string message)
    {
        Message = message;
    }
}

public sealed partial class TelepathyAnnouncementSendEvent : InstantActionEvent
{
    public string Message { get; init; }
    public string TelepathyChannel { get; init; }

    public TelepathyAnnouncementSendEvent(string message, string telepathyChannel)
    {
        Message = message;
        TelepathyChannel = telepathyChannel;
    }
}

[ByRefEvent]
public record struct TelepathySendAttemptEvent(EntityUid Sender, bool Cancelled);
