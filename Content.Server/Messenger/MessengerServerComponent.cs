// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Messenger;

namespace Content.Server.Messenger;

[RegisterComponent]
public sealed class MessengerServerComponent : Component
{
    [DataField(("serverName"))] public string Name = "";

    // store EntityUid as authentication entity to contactKey
    private readonly Dictionary<EntityUid, ContactKey> _clientToContact = new();
    // store contact info by contactKey
    private readonly SequenceDataStore<ContactKey, MessengerContact> _contactsStore = new();
    // store chat info by chat key
    private readonly SequenceDataStore<ChatKey, MessengerChat> _chatsStore = new();
    // store message info by message key
    private readonly SequenceDataStore<MessageKey, MessengerMessage> _messagesStore = new();

    // store chats which can find any contact connected to server
    private readonly HashSet<ChatKey> _publicChats = new();
    // store chats which can find only invited contact, like private chats
    private readonly SequenceDataStore<ContactKey, HashSet<ChatKey>> _privateChats = new();

    public IEnumerable<KeyValuePair<EntityUid, ContactKey>> GetClientToContact()
    {
        return _clientToContact.AsEnumerable();
    }

    public bool GetContactKey(EntityUid client, [NotNullWhen(true)] out ContactKey? key)
    {
        key = null;

        if (!_clientToContact.ContainsKey(client))
            return false;

        key = _clientToContact[client];

        return true;
    }

    public MessengerContact GetContact(ContactKey key)
    {
        if (!_contactsStore.Get(key, out var messengerContact))
            return new();

        messengerContact.Id = key.Id;
        return messengerContact;
    }

    public HashSet<ChatKey> GetPrivateChats(ContactKey key)
    {
        return _privateChats.Get(key, out var chats) ? new HashSet<ChatKey>(chats) : new();
    }

    public HashSet<ChatKey> GetPublicChats()
    {
        return new HashSet<ChatKey>(_publicChats);
    }

    public ContactKey AddEntityContact(EntityUid uid, string name, string netAddress)
    {
        if (_clientToContact.TryGetValue(uid, out var contact))
            return contact;

        var contactKey = _contactsStore.Add(new MessengerContact(name, netAddress));
        _clientToContact.Add(uid, contactKey);

        return contactKey;
    }

    public void UpdateContactName(ContactKey key, string? name)
    {
        if (name == null)
            return;

        if (!_contactsStore.Get(key, out var contact))
            return;

        if (contact.Name == name)
            return;

        contact.Name = name;
        _contactsStore.Set(key, contact);
    }

    public ChatKey AddChat( MessengerChat chat)
    {
        return _chatsStore.Add(chat);
    }

    public MessengerChat GetChat(   ChatKey key)
    {
        if (!_chatsStore.Get(key, out var chat))
            return new();
        chat.Id = key.Id;
        return chat;
    }

    public MessageKey AddMessage( MessengerMessage message)
    {
        return _messagesStore.Add(message);
    }

    public MessengerMessage GetMessage(  MessageKey key)
    {
        if (!_messagesStore.Get(key, out var message))
            return new();
        message.Id = key.Id;

        return message;
    }

    public void AddPublicChat(ChatKey chatKey)
    {
        _publicChats.Add(chatKey);
    }

    public void AddPrivateChats(ContactKey contact, IEnumerable<ChatKey> chats)
    {
        AddKeyToHasSet(_privateChats, contact, chats);
    }

    public void AddPrivateChats(ContactKey contact, ChatKey chat)
    {
        AddKeyToHasSet(_privateChats, contact, chat);
    }

    private void AddKeyToHasSet<TKey, TValue>(SequenceDataStore<TKey, HashSet<TValue>> storage, TKey key, IEnumerable<TValue> list) where TKey : IId, new()
    {
        if (!storage.Get(key, out var existList))
        {
            storage.Set(key, new HashSet<TValue>(list));
            return;
        }

        foreach (var value in list)
        {
            existList.Add(value);
        }

        storage.Set(key, existList);
    }

    private void AddKeyToHasSet<TKey, TValue>(SequenceDataStore<TKey, HashSet<TValue>> storage, TKey key, TValue val) where TKey : IId, new()
    {
        if (!storage.Get(key, out var existList))
        {
            storage.Set(key,new HashSet<TValue>{val});
            return;
        }

        existList.Add(val);

        storage.Set(key, existList);
    }
}

public sealed class ContactKey : IId
{
    public uint Id { get; init;}

    public ContactKey()
    {
        Id = 0;
    }
    public ContactKey(uint id)
    {
        Id = id;
    }

    public override int GetHashCode()
    {
        return (int) Id;
    }

    public override bool Equals(object? obj)
    {
        return obj is ChatKey && Equals((ChatKey) obj);
    }

    private bool Equals(ChatKey p)
    {
        return Id == p.Id;
    }
}

public sealed class MessageKey : IId
{
    public uint Id { get; init;}
    public MessageKey()
    {
        Id = 0;
    }
    public MessageKey(uint id)
    {
        Id = id;
    }

    public override int GetHashCode()
    {
        return (int) Id;
    }

    public override bool Equals(object? obj)
    {
        return obj is ChatKey && Equals((ChatKey) obj);
    }

    public bool Equals(ChatKey p)
    {
        return Id == p.Id;
    }
}

public sealed class ChatKey : IId
{
    public uint Id { get; init; }
    public ChatKey()
    {
        Id = 0;
    }
    public ChatKey(uint id)
    {
        Id = id;
    }

    public override int GetHashCode()
    {
        return (int) Id;
    }

    public override bool Equals(object? obj)
    {
        return obj is ChatKey && Equals((ChatKey) obj);
    }

    private bool Equals(ChatKey p)
    {
        return Id == p.Id;
    }
}

public sealed class SequenceDataStore<TKey, TValue> where TKey : IId, new() where TValue : notnull, new()
{
    private uint _sequence;
    private readonly Dictionary<uint, TValue> _storage = new();

    public TKey Add(TValue value)
    {
        if (!_storage.TryAdd(_sequence, value))
        {
            _sequence++;
            Add(value);
        }

        var t = new TKey
        {
            Id = _sequence
        };

        _sequence++;
        return t;
    }

    public void Set(TKey key, TValue value)
    {
        _storage[key.Id] = value;
    }

    public bool Get(TKey key, [NotNullWhen(true)] out TValue? value)
    {
        value = default;

        if (!_storage.ContainsKey(key.Id))
            return false;

        value = _storage[key.Id];
        return true;
    }

    public bool Delete(TKey key)
    {
        return _storage.Remove(key.Id);
    }
}

public interface IId
{
    public uint Id { get; init; }
}
