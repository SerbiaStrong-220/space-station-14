// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Messenger;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class MessengerUiState : BoundUserInterfaceState
{
    public MessengerContact ClientContact = new();
    public Dictionary<ChatUid, MessengerChatUiState> Chats = new();
    public Dictionary<MessageUid, MessengerMessage> Messages = new();
    public Dictionary<ContactUid, MessengerContact> Contacts = new();
}

[Serializable, NetSerializable]
public sealed class MessengerChatUiState
{
    public ChatUid Id;
    public string Name;
    public MessengerChatKind Kind;
    public HashSet<ContactUid> Members;
    public HashSet<MessageUid> Messages;
    public MessageUid? LastMessage;
    public int SortNumber;
    public int Unread;
    public bool ForceUpdate;

    public MessengerChatUiState(ChatUid id, string? name, MessengerChatKind kind, HashSet<ContactUid> members,
        HashSet<MessageUid> messages, MessageUid? lastMessage, int sortNumber)
    {
        Id = id;
        Name = name ?? "unknown";
        Kind = kind;
        Members = members;
        Messages = messages;
        LastMessage = lastMessage;
        SortNumber = sortNumber;
    }
}

[Serializable, NetSerializable]
public sealed class MessengerClientContactUiState(MessengerContact clientContact) : BoundUserInterfaceState
{
    public MessengerContact ClientContact = clientContact;
}

[Serializable, NetSerializable]
public sealed class MessengerContactUiState(List<MessengerContact> contacts) : BoundUserInterfaceState
{
    public List<MessengerContact> Contacts = contacts;
}

[Serializable, NetSerializable]
public sealed class MessengerMessagesUiState(List<MessengerMessage> messages) : BoundUserInterfaceState
{
    public List<MessengerMessage> Messages = messages;
}

[Serializable, NetSerializable]
public sealed class MessengerChatUpdateUiState(List<MessengerChat> chats) : BoundUserInterfaceState
{
    public List<MessengerChat> Chats = chats;
}

[Serializable, NetSerializable]
public sealed class MessengerErrorUiState(LocId locId) : BoundUserInterfaceState
{
    public LocId LocId = locId;
}

[Serializable, NetSerializable]
public sealed class MessengerNewChatMessageUiState(ChatUid chatId, MessengerMessage message) : BoundUserInterfaceState
{
    public ChatUid ChatId = chatId;
    public MessengerMessage Message = message;
}
