// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Messenger;
using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class MessengerUiState : BoundUserInterfaceState
{
    public MessengerContact ClientContact = new();
    public Dictionary<uint, MessengerChatUiState> Chats = new();
    public Dictionary<uint, MessengerMessage> Messages = new();
    public Dictionary<uint, MessengerContact> Contacts = new();
}

[Serializable, NetSerializable]
public sealed class MessengerChatUiState
{
    public uint Id;
    public string Name;
    public MessengerChatKind Kind;
    public HashSet<uint> Members;
    public HashSet<uint> Messages;
    public uint? LastMessage;
    public int SortNumber;
    public int Unread;
    public bool ForceUpdate;

    public MessengerChatUiState(uint id, string? name, MessengerChatKind kind, HashSet<uint> members,
        HashSet<uint> messages, uint? lastMessage, int sortNumber)
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
    // uint - chatId
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
public sealed class MessengerNewChatMessageUiState(uint chatId, MessengerMessage message) : BoundUserInterfaceState
{
    public uint ChatId = chatId;
    public MessengerMessage Message = message;
}
