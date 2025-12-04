// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.SS220.Messenger;

namespace Content.Server.SS220.Messenger;

[RegisterComponent]
public sealed partial class MessengerServerComponent : Component
{
    [DataField("serverName")]
    public string Name = "";

    // store EntityUid as authentication entity to contactKey
    private readonly Dictionary<EntityUid, ContactId> _clientToContact = new();

    // store contact info by contactKey
    private readonly SequencedStore<ContactId, MessengerContact> _contactsStore = new();

    // store chat info by chat id
    private readonly SequencedStore<ChatId, MessengerChat> _chatsStore = new();

    // store message info by message id
    private readonly SequencedStore<MessageId, MessengerMessage> _messagesStore = new();

    // store chats which can find any contact connected to server
    private readonly HashSet<ChatId> _publicChats = new();

    // store chats which can find only invited contact, like private chats
    private readonly SequencedStore<ContactId, HashSet<ChatId>> _privateChats = new();

    public List<KeyValuePair<EntityUid, ContactId>> GetClientToContact()
    {
        return [.._clientToContact];
    }

    public bool GetContactId(EntityUid client, [NotNullWhen(true)] out ContactId? id)
    {
        id = null;

        if (!_clientToContact.TryGetValue(client, out var value))
            return false;

        id = value;
        return true;
    }

    public MessengerContact GetContact(ContactId id)
    {
        var contact = _contactsStore.Get(id);
        if (contact == null)
            return new MessengerContact();

        contact.Id = id.Id;
        return contact;
    }

    public HashSet<ChatId> GetPrivateChats(ContactId contact)
    {
        var chats = _privateChats.Get(contact);
        return chats == null ? [] : [..chats];
    }

    public HashSet<ChatId> GetPublicChats()
    {
        return [.._publicChats];
    }

    public ContactId AddEntityContact(EntityUid uid, string name, string netAddress)
    {
        if (_clientToContact.TryGetValue(uid, out var existing))
            return existing;

        var contactId = _contactsStore.Add(new MessengerContact(name, netAddress));
        _clientToContact.Add(uid, contactId);

        return contactId;
    }

    public void UpdateContactName(ContactId id, string? name)
    {
        if (name == null)
            return;

        var contact = _contactsStore.Get(id);
        if (contact == null)
            return;

        contact.Name = name;
    }

    public ChatId AddChat(MessengerChat chat)
    {
        return _chatsStore.Add(chat);
    }

    public MessengerChat GetChat(ChatId id)
    {
        var chat = _chatsStore.Get(id);
        if (chat == null)
            return new MessengerChat();

        chat.Id = id.Id;
        return chat;
    }

    public MessageId AddMessage(MessengerMessage message)
    {
        return _messagesStore.Add(message);
    }

    public bool DeleteMessage(MessageId id)
    {
        return _messagesStore.Delete(id);
    }

    public void ClearAllMessages()
    {
        foreach (var msgId in _messagesStore.GetAllKeys())
        {
            _messagesStore.Delete(msgId);
        }

        foreach (var chatId in _chatsStore.GetAllKeys())
        {
            var chat = _chatsStore.Get(chatId);
            if (chat == null)
                continue;

            chat.MessagesId.Clear();
            chat.LastMessageId = null;

            _chatsStore.Set(chatId, chat);
        }
    }

    public MessengerMessage GetMessage(MessageId id)
    {
        var message = _messagesStore.Get(id);
        if (message == null)
            return new MessengerMessage();

        message.Id = id.Id;
        return message;
    }

    public void AddPublicChat(ChatId chatId)
    {
        _publicChats.Add(chatId);
    }

    public void AddPrivateChats(ContactId contact, List<ChatId> chats)
    {
        AddKeyToHashSet(_privateChats, contact, chats);
    }

    public void AddPrivateChats(ContactId contact, ChatId chat)
    {
        AddPrivateChats(contact, [chat]);
    }

    private void AddKeyToHashSet<TKey, TValue>(
        SequencedStore<TKey, HashSet<TValue>> storage,
        TKey key,
        List<TValue> list)
        where TKey : ISequenceKey, new()
    {
        var existing = storage.Get(key);

        if (existing == null)
        {
            storage.Set(key, [..list]);
            return;
        }

        existing.UnionWith(list);
        storage.Set(key, existing);
    }
}

public interface ISequenceKey
{
    uint Id { get; init; }
}

public abstract class SequenceKey : ISequenceKey
{
    public uint Id { get; init; }

    protected SequenceKey()
    {
        Id = 0;
    }

    protected SequenceKey(uint id)
    {
        Id = id;
    }

    public override int GetHashCode()
    {
        return (int)Id;
    }

    public override bool Equals(object? obj)
    {
        return obj is SequenceKey other && Id == other.Id;
    }
}

public sealed class ContactId : SequenceKey
{
    public ContactId() { }
    public ContactId(uint id) : base(id) { }
}

public sealed class MessageId : SequenceKey
{
    public MessageId() { }
    public MessageId(uint id) : base(id) { }
}

public sealed class ChatId : SequenceKey
{
    public ChatId() { }
    public ChatId(uint id) : base(id) { }
}

public sealed class SequencedStore<TKey, TValue>
    where TKey : ISequenceKey, new()
    where TValue : notnull
{
    private uint _sequence;
    private readonly Dictionary<uint, TValue> _storage = new();

    public TKey Add(TValue value)
    {
        while (!_storage.TryAdd(_sequence, value))
        {
            _sequence++;
        }

        var key = new TKey
        {
            Id = _sequence,
        };

        _sequence++;
        return key;
    }

    public void Set(TKey key, TValue value)
    {
        _storage[key.Id] = value;
    }

    public TValue? Get(TKey key)
    {
        return _storage.TryGetValue(key.Id, out var value)
            ? value
            : default;
    }

    public bool Delete(TKey key)
    {
        return _storage.Remove(key.Id);
    }

    public IEnumerable<TKey> GetAllKeys()
    {
        foreach (var id in _storage.Keys)
        {
            yield return new TKey { Id = id };
        }
    }
}
