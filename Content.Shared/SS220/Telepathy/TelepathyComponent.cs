// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Actions;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Telepathy;

/// <summary>
/// This is used for giving telepathy ability
/// </summary>
[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class TelepathyComponent : Component
{
    /// <summary>
    /// Whether the entity can send telepathic messages.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public bool CanSend;

    /// <summary>
    /// The entity's channel, including channels allocated dynamically by the server.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<TelepathyChannelPrototype>? TelepathyChannelPrototype;

    /// <summary>
    /// Whether the entity receives messages from every telepathy channel.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ReceiveAllChannels;

    public override bool SendOnlyToOwner => true;
}

/// <summary>
/// Raised locally when a telepathy component starts, is removed, or receives networked state.
/// Removal listeners must wait until removal finishes before querying component presence.
/// </summary>
[ByRefEvent]
public readonly record struct TelepathyChangedEvent(EntityUid Entity);

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
