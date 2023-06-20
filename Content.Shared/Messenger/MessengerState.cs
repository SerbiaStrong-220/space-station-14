// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Serialization;

namespace Content.Shared.Messenger;

[Serializable, NetSerializable]
public sealed class MessengerChat
{
    public uint Id;
    public string? Name;
    public HashSet<uint> Members = new();
    public HashSet<uint> Messages = new();
    public MessengerChatKind Kind;
    public uint? LastMessage;

    public MessengerChat(string? name, MessengerChatKind kind, HashSet<uint> members)
    {
        Name = name ?? "unknown";
        Members = members;
        Kind = kind;
    }

    public MessengerChat()
    {
        Name = "";
        Kind = MessengerChatKind.Contact;
        Members = new();
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
public sealed class MessengerContact
{
    public uint Id;
    public string? Name;
    public HashSet<string> ActiveAddresses = new();

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
    public uint Id;
    public uint ChatId;
    public uint From;
    public TimeSpan Time;
    public string Text;

    public MessengerMessage(uint chatId, uint from, TimeSpan time, string text)
    {
        ChatId = chatId;
        From = from;
        Time = time;
        Text = text;
    }

    public MessengerMessage()
    {
        From = 0;
        Time = new TimeSpan();
        Text = "";
    }
}

