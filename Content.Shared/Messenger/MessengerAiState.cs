// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Actions;
using Robust.Shared.Serialization;

namespace Content.Shared.Messenger;

[Serializable, NetSerializable]
public enum MessengerAiUiKey : byte
{
    Key,
}

/// <summary>
/// Asks the server for the chats overview, or for the history of one chat if <see cref="ChatId"/> is set.
/// </summary>
[Serializable, NetSerializable]
public sealed class MessengerAiRequestMessage(uint? chatId) : BoundUserInterfaceMessage
{
    public uint? ChatId = chatId;
}

/// <summary>
/// Read-only snapshot of a messenger server for the station AI.
/// Contains only chats with messages, <see cref="MessengerChat.MessagesId"/> is always empty.
/// </summary>
[Serializable, NetSerializable]
public sealed class MessengerAiViewMessage : BoundUserInterfaceMessage
{
    public string ServerName = "";

    /// <summary>
    /// Contact id to its name. Net addresses of the contacts are not sent.
    /// </summary>
    public Dictionary<uint, string> ContactNames = new();

    /// <summary>
    /// Chats with at least one message, the most recent first.
    /// </summary>
    public List<MessengerChat> Chats = new();

    /// <summary>
    /// Last message of every chat and the recent history of <see cref="OpenedChatId"/>.
    /// </summary>
    public Dictionary<uint, MessengerMessage> Messages = new();

    public uint? OpenedChatId;
}

/// <summary>
/// Key of the messenger window of the station AI. The window is hosted by the AI entity itself.
/// </summary>
[Serializable, NetSerializable]
public enum MessengerAiClientUiKey : byte
{
    Key,
}

/// <summary>
/// Action of the station AI that opens its messenger.
/// </summary>
public sealed partial class MessengerAiOpenEvent : InstantActionEvent;

/// <summary>
/// Carries a regular messenger ui state to the AI window. States of a bound ui are shared by all its users,
/// while the messages can be sent to one actor.
/// </summary>
[Serializable, NetSerializable]
public sealed class MessengerAiStateMessage(BoundUserInterfaceState state) : BoundUserInterfaceMessage
{
    public BoundUserInterfaceState State = state;
}
