// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using Content.Server.Administration.Logs;
using Content.Server.CartridgeLoader.Cartridges;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Power.Components;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Bed.Cryostorage;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.Database;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.GameTicking;
using Content.Shared.Messenger;
using Content.Shared.PDA;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.Messenger;

public sealed partial class MessengerServerSystem : EntitySystem
{
    [Dependency] private DeviceNetworkSystem _deviceNetwork = default!;
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private AccessReaderSystem _accessSystem = default!;
    [Dependency] private SharedContainerSystem _containerSystem = default!;
    [Dependency] private SharedGameTicker _gameTicker = default!;
    [Dependency] private IAdminLogManager _adminLogger = default!;

    // Every new contact creates chats with all existing ones, so registration is spread over several updates
    private const int MaxRegistrationsPerUpdate = 8;

    public enum NetworkCommand
    {
        Info,
        Contact,
        ClientContact,
        Chat,
        Messages,
        NewMessage,
    }

    public enum NetworkKey
    {
        Command,
        ServerName,
        Chat,
        ChatList,
        Message,
        MessageList,
        Contact,
        ContactList,
        ChatId,
    }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MessengerServerComponent, DeviceNetworkPacketEvent>(OnNetworkPacket);
    }

    public override void Update(float frameTime)
    {
        var curTime = _gameTiming.CurTime;
        var cardQuery = GetEntityQuery<IdCardComponent>();
        var query = EntityQueryEnumerator<MessengerServerComponent>();

        while (query.MoveNext(out var uid, out var server))
        {
            if (server.NextContactUpdate > curTime)
                continue;

            server.NextContactUpdate = curTime + server.ContactUpdateInterval;
            UpdateContactNames(server, cardQuery);
            RegisterCartridgeOwners((uid, server));
            UpdateContactStatuses((uid, server));
        }
    }

    private static void UpdateContactNames(MessengerServerComponent server, EntityQuery<IdCardComponent> cardQuery)
    {
        foreach (var (idCardUid, contactId) in server.ClientToContact)
        {
            if (!cardQuery.TryGetComponent(idCardUid, out var card) || card.FullName == null)
                continue;

            if (server.Contacts.TryGetValue(contactId, out var contact))
                contact.Name = card.FullName;
        }
    }

    /// <summary>
    /// Registers owners of all messenger cartridges that have access to the server,
    /// even if nobody has opened the cartridge. Contacts are never removed to keep the chat history.
    /// </summary>
    private void RegisterCartridgeOwners(Entity<MessengerServerComponent> ent)
    {
        if (!IsPowered(ent))
            return;

        var registered = 0;
        var query = EntityQueryEnumerator<MessengerClientCartridgeComponent, DeviceNetworkComponent>();

        while (query.MoveNext(out _, out var cartridge, out var deviceNet))
        {
            if (string.IsNullOrEmpty(deviceNet.Address)
                || !TryGetIdCard(cartridge.Loader, out var idCardUid, out var idCard))
                continue;

            if (ent.Comp.ClientToContact.ContainsKey(idCardUid.Value) || !HasServerAccess(ent, idCardUid.Value))
                continue;

            var contactId = RegisterNewClient(ent, idCardUid.Value, idCard.FullName ?? "", deviceNet.Address);
            SendToContact(ent, contactId, CreateInfoPayload(ent.Comp));

            if (++registered >= MaxRegistrationsPerUpdate)
                return;
        }
    }

    /// <summary>
    /// Recalculates statuses of all contacts and sends the changed ones to every client.
    /// A contact that returned to the server range also gets the server info, so its client resyncs missed messages.
    /// </summary>
    private void UpdateContactStatuses(Entity<MessengerServerComponent> ent)
    {
        var serverMap = Transform(ent.Owner).MapID;
        var powered = IsPowered(ent);
        List<MessengerContact>? changed = null;

        foreach (var (idCardUid, contactId) in ent.Comp.ClientToContact)
        {
            if (!ent.Comp.Contacts.TryGetValue(contactId, out var contact))
                continue;

            var status = GetContactStatus(idCardUid, serverMap, powered);

            if (contact.Status == status)
                continue;

            contact.Status = status;
            changed ??= new List<MessengerContact>();
            changed.Add(contact);

            if (status == MessengerContactStatus.Online)
                SendToContact(ent, contactId, CreateInfoPayload(ent.Comp));
        }

        if (changed == null)
            return;

        var payload = new NetworkPayload
        {
            [NetworkKey.Command.ToString()] = NetworkCommand.Contact,
            [NetworkKey.ContactList.ToString()] = changed,
        };

        foreach (var contactId in ent.Comp.ClientToContact.Values)
        {
            SendToContact(ent, contactId, payload);
        }
    }

    private MessengerContactStatus GetContactStatus(EntityUid idCardUid, MapId serverMap, bool serverPowered)
    {
        if (Deleted(idCardUid))
            return MessengerContactStatus.OutOfRange;

        if (IsInCryostorage(idCardUid))
            return MessengerContactStatus.Cryo;

        // assume contact is reachable only while its id card is inside a pda on the server map
        if (!serverPowered
            || !_containerSystem.TryGetContainingContainer(idCardUid, out var container)
            || container.ID != PdaComponent.PdaIdSlotId)
            return MessengerContactStatus.OutOfRange;

        return Transform(container.Owner).MapID == serverMap
            ? MessengerContactStatus.Online
            : MessengerContactStatus.OutOfRange;
    }

    /// <summary>
    /// A body that left for cryostorage keeps its items, so the outermost container owner is the body itself.
    /// </summary>
    private bool IsInCryostorage(EntityUid uid)
    {
        var current = uid;

        while (_containerSystem.TryGetContainingContainer(current, out var container))
        {
            current = container.Owner;
        }

        return HasComp<CryostorageContainedComponent>(current);
    }

    private bool IsPowered(EntityUid uid)
    {
        return TryComp<ApcPowerReceiverComponent>(uid, out var power) && power.Powered;
    }

    private static NetworkPayload CreateInfoPayload(MessengerServerComponent server)
    {
        return new NetworkPayload
        {
            [NetworkKey.Command.ToString()] = NetworkCommand.Info,
            [NetworkKey.ServerName.ToString()] = server.Name,
        };
    }

    private void OnNetworkPacket(Entity<MessengerServerComponent> ent, ref DeviceNetworkPacketEvent args)
    {
        if (!args.Data.TryGetValue(MessengerClientCartridgeSystem.NetworkKey.Command.ToString(),
                out MessengerClientCartridgeSystem.NetworkCommand? command))
            return;

        switch (command)
        {
            case MessengerClientCartridgeSystem.NetworkCommand.CheckServer:
                OnCheckServer(ent, args);
                break;
            case MessengerClientCartridgeSystem.NetworkCommand.StateUpdate:
                OnStateUpdate(ent, args);
                break;
            case MessengerClientCartridgeSystem.NetworkCommand.MessageSend:
                OnMessageSend(ent, args);
                break;
        }
    }

    private void OnCheckServer(Entity<MessengerServerComponent> ent, DeviceNetworkPacketEvent args)
    {
        if (!HasServerAccess(ent, args.Data))
            return;

        if (!TryRegisterByIdCard(ent, args.SenderAddress, args.Data, out _))
            return;

        SendResponse(ent, args, CreateInfoPayload(ent.Comp));
    }

    private void OnStateUpdate(Entity<MessengerServerComponent> ent, DeviceNetworkPacketEvent args)
    {
        if (!TryAuthByIdCard(ent.Comp, args.Data, out var contactId))
            return;

        var chats = GetAccessedChats(ent.Comp, contactId);

        SendMissingContacts(ent, args, chats);
        SendMissingMessages(ent, args, chats);
        SendMissingChats(ent, args, chats);
    }

    /// <summary>
    /// Sends contacts of chat members that the client does not know yet.
    /// </summary>
    private void SendMissingContacts(Entity<MessengerServerComponent> ent,
        DeviceNetworkPacketEvent args,
        List<MessengerChat> chats)
    {
        // known ids are also used to skip ids already queued for sending
        var knownIds = GetKnownIds(args.Data, MessengerClientCartridgeSystem.NetworkKey.ContactsIds);
        var contacts = new List<MessengerContact>();

        foreach (var chat in chats)
        {
            foreach (var memberId in chat.MembersId)
            {
                if (knownIds.Add(memberId) && ent.Comp.Contacts.TryGetValue(memberId, out var contact))
                    contacts.Add(contact);
            }
        }

        if (contacts.Count == 0)
            return;

        SendResponse(ent, args, new NetworkPayload
        {
            [NetworkKey.Command.ToString()] = NetworkCommand.Contact,
            [NetworkKey.ContactList.ToString()] = contacts,
        });
    }

    /// <summary>
    /// Sends messages of accessed chats that the client does not know yet.
    /// </summary>
    private void SendMissingMessages(Entity<MessengerServerComponent> ent,
        DeviceNetworkPacketEvent args,
        List<MessengerChat> chats)
    {
        var knownIds = GetKnownIds(args.Data, MessengerClientCartridgeSystem.NetworkKey.MessagesIds);
        var messages = new List<MessengerMessage>();

        foreach (var chat in chats)
        {
            foreach (var messageId in chat.MessagesId)
            {
                if (knownIds.Add(messageId) && ent.Comp.Messages.TryGetValue(messageId, out var message))
                    messages.Add(message);
            }
        }

        if (messages.Count == 0)
            return;

        SendResponse(ent, args, new NetworkPayload
        {
            [NetworkKey.Command.ToString()] = NetworkCommand.Messages,
            [NetworkKey.MessageList.ToString()] = messages,
        });
    }

    /// <summary>
    /// Sends accessed chats that the client does not know yet.
    /// </summary>
    private void SendMissingChats(Entity<MessengerServerComponent> ent,
        DeviceNetworkPacketEvent args,
        List<MessengerChat> chats)
    {
        var knownIds = GetKnownIds(args.Data, MessengerClientCartridgeSystem.NetworkKey.CurrentChatIds);
        var missingChats = new List<MessengerChat>();

        foreach (var chat in chats)
        {
            if (!knownIds.Contains(chat.Id))
                missingChats.Add(chat);
        }

        if (missingChats.Count == 0)
            return;

        SendResponse(ent, args, new NetworkPayload
        {
            [NetworkKey.Command.ToString()] = NetworkCommand.Chat,
            [NetworkKey.ChatList.ToString()] = missingChats,
        });
    }

    private void OnMessageSend(Entity<MessengerServerComponent> ent, DeviceNetworkPacketEvent args)
    {
        var server = ent.Comp;

        if (!TryAuthByIdCard(server, args.Data, out var senderId))
            return;

        if (!args.Data.TryGetValue(MessengerClientCartridgeSystem.NetworkKey.ChatId.ToString(), out uint? chatId))
            return;

        if (!args.Data.TryGetValue(MessengerClientCartridgeSystem.NetworkKey.MessageText.ToString(),
                out string? messageText))
            return;

        if (!server.Chats.TryGetValue(chatId.Value, out var chat) || !CanAccessChat(server, senderId, chat.Id))
        {
            _adminLogger.Add(LogType.MessengerServer, LogImpact.Low,
                $"No access: sender entity: {args.Sender}, chat: {chat?.Name}, chatID: {chatId}, msg: {messageText}");
            return;
        }

        // ss220 messenger time fix start
        var message = new MessengerMessage(chat.Id, senderId,
            _gameTiming.CurTime - _gameTicker.RoundStartTimeSpan, messageText)
        {
            Id = server.NextId++,
        };
        // ss220 messenger time fix end
        server.Messages[message.Id] = message;

        chat.LastMessageId = message.Id;
        chat.MessagesId.Add(message.Id);

        var payload = new NetworkPayload
        {
            [NetworkKey.Command.ToString()] = NetworkCommand.NewMessage,
            [NetworkKey.ChatId.ToString()] = chat.Id,
            [NetworkKey.Message.ToString()] = message,
        };

        foreach (var memberId in chat.MembersId)
        {
            SendToContact(ent, memberId, payload);
        }

        _adminLogger.Add(LogType.MessengerServer, LogImpact.Low,
            $"Send: sender entity: {args.Sender}, chat: {chat.Name}, chatID: {chat.Id}, msg: {messageText}");
    }

    private static bool CanAccessChat(MessengerServerComponent server, uint contactId, uint chatId)
    {
        if (server.PublicChats.Contains(chatId))
            return true;

        return server.PrivateChats.TryGetValue(contactId, out var privateChats) && privateChats.Contains(chatId);
    }

    /// <summary>
    /// Returns public chats and chats where the contact was invited.
    /// </summary>
    private static List<MessengerChat> GetAccessedChats(MessengerServerComponent server, uint contactId)
    {
        var chatIds = new HashSet<uint>(server.PublicChats);

        if (server.PrivateChats.TryGetValue(contactId, out var privateChats))
            chatIds.UnionWith(privateChats);

        var chats = new List<MessengerChat>(chatIds.Count);

        foreach (var chatId in chatIds)
        {
            if (server.Chats.TryGetValue(chatId, out var chat))
                chats.Add(chat);
        }

        return chats;
    }

    private static void AddPrivateChat(MessengerServerComponent server, uint contactId, uint chatId)
    {
        if (!server.PrivateChats.TryGetValue(contactId, out var privateChats))
        {
            privateChats = new HashSet<uint>();
            server.PrivateChats[contactId] = privateChats;
        }

        privateChats.Add(chatId);
    }

    /// <summary>
    /// Returns a modifiable copy of the id set sent by the client, or an empty set if there is none.
    /// </summary>
    private static HashSet<uint> GetKnownIds(NetworkPayload payload, MessengerClientCartridgeSystem.NetworkKey key)
    {
        return payload.TryGetValue(key.ToString(), out HashSet<uint>? ids)
            ? new HashSet<uint>(ids)
            : new HashSet<uint>();
    }

    /// <summary>
    /// Finds the id card in the device from the payload. The id card is the client authentication entity.
    /// </summary>
    /// <returns>True if the id card is already registered on the server.</returns>
    private bool TryAuthByIdCard(MessengerServerComponent server, NetworkPayload payload, out uint contactId)
    {
        contactId = 0;

        return TryGetIdCard(payload, out var idCardUid, out _)
               && server.ClientToContact.TryGetValue(idCardUid.Value, out contactId);
    }

    /// <summary>
    /// Same as <see cref="TryAuthByIdCard"/>, but creates a new contact if the id card is not registered yet.
    /// </summary>
    /// <returns>True if the client is registered (now or before).</returns>
    private bool TryRegisterByIdCard(Entity<MessengerServerComponent> ent,
        string netAddress,
        NetworkPayload payload,
        out uint contactId)
    {
        contactId = 0;

        if (!TryGetIdCard(payload, out var idCardUid, out var idCard))
            return false;

        if (ent.Comp.ClientToContact.TryGetValue(idCardUid.Value, out contactId))
            return true;

        contactId = RegisterNewClient(ent, idCardUid.Value, idCard.FullName ?? "", netAddress);

        return true;
    }

    private bool TryGetIdCard(NetworkPayload payload,
        [NotNullWhen(true)] out EntityUid? idCardUid,
        [NotNullWhen(true)] out IdCardComponent? idCard)
    {
        idCardUid = null;
        idCard = null;

        if (payload.TryGetValue(MessengerClientCartridgeSystem.NetworkKey.DeviceUid.ToString(), out NetEntity? netLoader))
            return TryGetIdCard(GetEntity(netLoader), out idCardUid, out idCard);

        if (payload.TryGetValue(MessengerClientCartridgeSystem.NetworkKey.DeviceUid.ToString(), out EntityUid? loader))
            return TryGetIdCard(loader, out idCardUid, out idCard);

        return false;
    }

    private bool TryGetIdCard(EntityUid? loaderUid,
        [NotNullWhen(true)] out EntityUid? idCardUid,
        [NotNullWhen(true)] out IdCardComponent? idCard)
    {
        idCardUid = null;
        idCard = null;

        if (loaderUid == null)
            return false;

        if (!_containerSystem.TryGetContainer(loaderUid.Value, PdaComponent.PdaIdSlotId, out var container))
            return false;

        foreach (var containedUid in container.ContainedEntities)
        {
            if (!TryComp(containedUid, out idCard))
                continue;

            idCardUid = containedUid;

            return true;
        }

        return false;
    }

    private bool HasServerAccess(Entity<MessengerServerComponent> ent, NetworkPayload payload)
    {
        return TryGetIdCard(payload, out var idCardUid, out _) && HasServerAccess(ent, idCardUid.Value);
    }

    private bool HasServerAccess(Entity<MessengerServerComponent> ent, EntityUid idCardUid)
    {
        return TryComp<AccessReaderComponent>(ent, out var reader) && _accessSystem.IsAllowed(idCardUid, ent, reader);
    }

    private void SendResponse(Entity<MessengerServerComponent> ent, DeviceNetworkPacketEvent args, NetworkPayload payload)
    {
        _deviceNetwork.QueuePacket(ent, args.SenderAddress, payload);
    }

    /// <summary>
    /// Sends the packet to every client address of the contact (a contact can have many connected clients).
    /// </summary>
    private void SendToContact(Entity<MessengerServerComponent> ent, uint contactId, NetworkPayload payload)
    {
        var hasContact = ent.Comp.Contacts.TryGetValue(contactId, out var contact);
        DebugTools.Assert(hasContact, $"Messenger contact {contactId} is referenced but does not exist");

        if (contact == null)
            return;

        foreach (var address in contact.ActiveAddresses)
        {
            _deviceNetwork.QueuePacket(ent, address, payload);
        }
    }

    /// <summary>
    /// Creates a contact for the id card and private chats between it and every contact of the server
    /// (including a "favorites" chat with itself), then notifies connected clients.
    /// </summary>
    /// <returns>Id of the new contact.</returns>
    private uint RegisterNewClient(Entity<MessengerServerComponent> ent,
        EntityUid idCardUid,
        string fullName,
        string netAddress)
    {
        var server = ent.Comp;
        var newContact = new MessengerContact(fullName, netAddress)
        {
            Id = server.NextId++,
        };
        server.Contacts[newContact.Id] = newContact;
        server.ClientToContact[idCardUid] = newContact.Id;

        // for now every contact can write to every other contact
        foreach (var contactId in server.ClientToContact.Values)
        {
            if (!server.Contacts.TryGetValue(contactId, out var contact))
                continue;

            var chatName = contactId == newContact.Id
                ? Loc.GetString("messenger-ui-favorites-chat")
                : Loc.GetString("messenger-server-chat-pair", ("first", contact.Name ?? "unknown"), ("second", newContact.Name ?? "unknown"));

            var chat = new MessengerChat(chatName,
                MessengerChatKind.Contact,
                new HashSet<uint> { contactId, newContact.Id })
            {
                Id = server.NextId++,
            };
            server.Chats[chat.Id] = chat;

            AddPrivateChat(server, contactId, chat.Id);
            AddPrivateChat(server, newContact.Id, chat.Id);

            SendToContact(ent, contactId, new NetworkPayload
            {
                [NetworkKey.Command.ToString()] = NetworkCommand.Chat,
                [NetworkKey.Chat.ToString()] = chat,
            });

            SendToContact(ent, contactId, new NetworkPayload
            {
                [NetworkKey.Command.ToString()] = NetworkCommand.Contact,
                [NetworkKey.Contact.ToString()] = newContact,
            });
        }

        return newContact.Id;
    }

    /// <summary>
    /// Restores the whole ui state of an already registered client to minimize network load.
    /// </summary>
    /// <param name="ent">Messenger server</param>
    /// <param name="loader">Device with loaded id card (for now only id card can be authenticator)</param>
    /// <param name="uiState">Restored state</param>
    /// <returns>True if the client is registered and the state was restored.</returns>
    public bool TryRestoreContactUiState(Entity<MessengerServerComponent> ent,
        EntityUid loader,
        [NotNullWhen(true)] out MessengerUiState? uiState)
    {
        uiState = null;

        if (!TryGetIdCard(loader, out var idCardUid, out _))
            return false;

        if (!ent.Comp.ClientToContact.TryGetValue(idCardUid.Value, out var contactId))
            return false;

        if (!ent.Comp.Contacts.TryGetValue(contactId, out var clientContact))
            return false;

        uiState = new MessengerUiState
        {
            ClientContact = clientContact,
        };

        var index = 0;

        foreach (var chat in GetAccessedChats(ent.Comp, contactId))
        {
            uiState.Chats[chat.Id] = new MessengerChatUiState(chat.Id,
                chat.Name,
                chat.Kind,
                chat.MembersId,
                chat.MessagesId,
                chat.LastMessageId,
                index);

            index++;

            if (chat.LastMessageId != null && ent.Comp.Messages.TryGetValue(chat.LastMessageId.Value, out var lastMessage))
                uiState.Messages[lastMessage.Id] = lastMessage;

            foreach (var memberId in chat.MembersId)
            {
                if (ent.Comp.Contacts.TryGetValue(memberId, out var member))
                    uiState.Contacts[member.Id] = member;
            }
        }

        return true;
    }

    public List<uint> ActiveServersFrequency(MapId mapId)
    {
        var activeServersFrequency = new List<uint>();
        var query = EntityQueryEnumerator<DeviceNetworkComponent,
            MessengerServerComponent,
            ApcPowerReceiverComponent,
            TransformComponent>();

        while (query.MoveNext(out _, out var deviceNet, out _, out var power, out var transform))
        {
            if (transform.MapID != mapId || !power.Powered)
                continue;

            if (deviceNet.ReceiveFrequency != null)
                activeServersFrequency.Add(deviceNet.ReceiveFrequency.Value);
        }

        return activeServersFrequency;
    }
}
