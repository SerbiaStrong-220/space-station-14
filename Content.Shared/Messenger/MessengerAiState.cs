// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

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
