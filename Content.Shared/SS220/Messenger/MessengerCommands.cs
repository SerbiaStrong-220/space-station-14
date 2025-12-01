using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Messenger;

public static class MessengerNetKeys
{
    public const string Command = "MessengerCommand";
}

public interface IMessengerCommand;

[Serializable, NetSerializable]
public sealed class CheckServerCommand : IMessengerCommand
{
    [NonSerialized]
    public EntityUid DeviceUid;
}

[Serializable, NetSerializable]
public sealed class ServerInfoCommand : IMessengerCommand
{
    public string ServerName = string.Empty;
    public uint ContactId;
}

[Serializable, NetSerializable]
public sealed class SendMessageCommand : IMessengerCommand
{
    [NonSerialized]
    public EntityUid DeviceUid;

    public uint ChatId;
    public string MessageText = string.Empty;
}

[Serializable, NetSerializable]
public sealed class RequestStateCommand : IMessengerCommand
{
    [NonSerialized]
    public EntityUid DeviceUid;

    public HashSet<uint>? KnownChats;
    public HashSet<uint>? KnownContacts;
    public HashSet<uint>? KnownMessages;

    public bool FullState;
}

[Serializable, NetSerializable]
public sealed class DeleteChatMessagesCommand : IMessengerCommand
{
    [NonSerialized]
    public EntityUid DeviceUid;

    public uint ChatId;
}

[Serializable, NetSerializable]
public sealed class ChatMessageClearedCommand : IMessengerCommand
{
    public uint ChatId;
}

[Serializable, NetSerializable]
public sealed class UpdateChatsCommand : IMessengerCommand
{
    public List<MessengerChat> Chats = null!;
}

[Serializable, NetSerializable]
public sealed class UpdateContactsCommand : IMessengerCommand
{
    public List<MessengerContact> Contacts = null!;
}

[Serializable, NetSerializable]
public sealed class UpdateMessagesCommand : IMessengerCommand
{
    public List<MessengerMessage> Messages = null!;
}

[Serializable, NetSerializable]
public sealed class NewMessageCommand : IMessengerCommand
{
    public uint ChatId;
    public MessengerMessage Message = null!;
}

[Serializable, NetSerializable]
public sealed class ErrorCommand : IMessengerCommand
{
    public string Text = null!;
}

