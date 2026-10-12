// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.CartridgeLoader;
using Content.Shared.SS220.Messenger;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class MessengerSendMessageUiEvent : CartridgeMessageEvent
{
    public readonly ChatUid ChatId;
    public readonly string MessageText;
    public readonly MessageUid? ReplyToId;

    public MessengerSendMessageUiEvent(ChatUid chatId, string messageText, MessageUid? replyToId = null)
    {
        ChatId = chatId;
        MessageText = messageText;
        ReplyToId = replyToId;
    }
}

/// <summary>
/// The user opened a chat, so everything in it is read.
/// </summary>
[Serializable, NetSerializable]
public sealed class MessengerMarkReadUiEvent(ChatUid chatId) : CartridgeMessageEvent
{
    public readonly ChatUid ChatId = chatId;
}

[Serializable, NetSerializable]
public sealed class MessengerUpdateStateUiEvent : CartridgeMessageEvent
{
    public readonly HashSet<ContactUid>? CurrentContacts;
    public readonly HashSet<MessageUid>? CurrentMessages;
    public readonly HashSet<ChatUid>? CurrentChats;

    public readonly bool IsFullState;

    public MessengerUpdateStateUiEvent(bool isFullState)
    {
        IsFullState = isFullState;
    }

    public MessengerUpdateStateUiEvent(HashSet<ContactUid>? currentContacts, HashSet<MessageUid>? currentMessages,
        HashSet<ChatUid>? currentChats)
    {
        if (currentContacts is { Count: > 0 })
            CurrentContacts = currentContacts;

        if (currentMessages is { Count: > 0 })
            CurrentMessages = currentMessages;

        if (currentChats is { Count: > 0 })
            CurrentChats = currentChats;

        IsFullState = false;
    }
}
