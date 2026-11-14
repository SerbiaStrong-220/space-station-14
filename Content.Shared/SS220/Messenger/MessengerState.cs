// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Messenger;

[Serializable, NetSerializable]
public sealed class MessengerChat
{
    public ChatUid Id;
    public string? Name;
    public HashSet<ContactUid> MembersId = new();
    public HashSet<MessageUid> MessagesId = new();
    public MessengerChatKind Kind;
    public MessageUid? LastMessageId;

    public MessengerChat(string? name, MessengerChatKind kind, HashSet<ContactUid> membersId)
    {
        Name = name ?? "unknown";
        MembersId = membersId;
        Kind = kind;
    }

    public MessengerChat()
    {
        Name = "";
        Kind = MessengerChatKind.Contact;
        MembersId = new();
    }
}

[Serializable, NetSerializable]
public enum MessengerChatKind
{
    Contact,
    Chat,
    Channel,
    Bot,
}

[Serializable, NetSerializable]
public enum MessengerContactStatus : byte
{
    Online,
    OutOfRange,
    Cryo,
}

[Serializable, NetSerializable]
public sealed class MessengerContact
{
    public ContactUid Id;
    public string? Name;
    public HashSet<string> ActiveAddresses = new();
    public MessengerContactStatus Status;

    public MessengerContact(string name, string netAddress)
    {
        Name = name;
        ActiveAddresses.Add(netAddress);
    }

    public MessengerContact()
    {
        Name = "";
    }
}

[Serializable, NetSerializable]
public sealed class MessengerMessage
{
    public MessageUid Id;
    public ChatUid ChatId;
    public ContactUid FromContactId;
    public TimeSpan Time;
    public string Text;

    /// <summary>
    /// Message of the same chat this one answers.
    /// </summary>
    public MessageUid? ReplyToId;

    public MessengerMessage(ChatUid chatId, ContactUid fromContactId, TimeSpan time, string text)
    {
        ChatId = chatId;
        FromContactId = fromContactId;
        Time = time;
        Text = text;
    }

    public MessengerMessage()
    {
        FromContactId = default;
        Time = new TimeSpan();
        Text = "";
    }
}
