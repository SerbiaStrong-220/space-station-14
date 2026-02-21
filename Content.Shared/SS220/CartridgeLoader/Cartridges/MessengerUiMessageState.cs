// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Messenger;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class MessengerUiState : BoundUserInterfaceState
{
    public MessengerContact ClientContact = new();
    public Dictionary<uint, MessengerChatUiState> Chats = new();
    public Dictionary<uint, MessengerMessage> Messages = new();
    public Dictionary<uint, MessengerContact> Contacts = new();
}

[Serializable, NetSerializable]
public sealed class MessengerChatUiState(
    uint id,
    string? name,
    MessengerChatKind kind,
    HashSet<uint> members,
    HashSet<uint> messages,
    uint? lastMessage,
    int sortNumber)
{
    public uint Id = id;
    public string Name = name ?? "unknown";
    public MessengerChatKind Kind = kind;
    public HashSet<uint> Members = members;
    public HashSet<uint> Messages = messages;
    public uint? LastMessage = lastMessage;
    public int SortNumber = sortNumber;
    public bool NewMessages = true;
    public bool ForceUpdate;
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
public sealed class MessengerErrorUiState(string text) : BoundUserInterfaceState
{
    public string Text = text;
}

[Serializable, NetSerializable]
public sealed class MessengerNewChatMessageUiState(uint chatId, MessengerMessage message) : BoundUserInterfaceState
{
    public uint ChatId = chatId;
    public MessengerMessage Message = message;
}

[Serializable, NetSerializable]
public sealed class MessengerDeleteMsgInChatUiState(uint? chatId) : BoundUserInterfaceState
{
    public uint? ChatId = chatId;
}
