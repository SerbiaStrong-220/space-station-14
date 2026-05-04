// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Power.Components;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Database;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.GameTicking;
using Content.Shared.PDA;
using Content.Shared.SS220.CartridgeLoader.Cartridges;
using Content.Shared.SS220.Messenger;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server.SS220.Messenger;

/// <summary>
/// System responsible for managing server-side logic for the messenger network.
/// Handles packet routing, contact registration, chat/message sync, and client updates.
/// </summary>
public sealed class MessengerServerSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly DeviceNetworkSystem _deviceNetworkSystem = default!;
    [Dependency] private readonly AccessReaderSystem _accessSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedGameTicker _gameTicker = default!;

    private TimeSpan _nextUpdate;
    private readonly TimeSpan _coolDown = TimeSpan.FromSeconds(30);

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MessengerServerComponent, DeviceNetworkPacketEvent>(OnNetworkPacket);
    }

    public override void Update(float frameTime)
    {
        if (_nextUpdate >= _gameTiming.CurTime)
            return;

        _nextUpdate = _gameTiming.CurTime.Add(_coolDown);

        var query = EntityQuery<MessengerServerComponent>();

        // update contact info if changed
        foreach (var server in query)
        {
            foreach (var (entityUid, contactId) in server.GetClientToContact())
            {
                if (!TryComp<IdCardComponent>(entityUid, out var card))
                    continue;

                if (string.IsNullOrEmpty(card.FullName))
                    continue;

                server.UpdateContactName(contactId, card.FullName);
            }
        }
    }

    private void BroadcastToChat(Entity<MessengerServerComponent> ent, ChatId chat, IMessengerCommand cmd)
    {
        var members = ent.Comp.GetChat(chat).MembersId;
        foreach (var memberId in members)
        {
            var contact = ent.Comp.GetContact(new ContactId(memberId));
            foreach (var addr in contact.ActiveAddresses)
            {
                Send(ent, addr, cmd);
            }
        }
    }

    private void HandleCheckServerPacket(Entity<MessengerServerComponent> server,
        CheckServerCommand cmd,
        DeviceNetworkPacketEvent args)
    {
        if (!AccessCheck(server, cmd.DeviceUid))
            return;

        if (!TryRegisterByIdCard(server, cmd.DeviceUid, args.SenderAddress, out var contactId))
            return;

        Send(server.Owner,
            args.SenderAddress,
            new ServerInfoCommand
            {
                ServerName = server.Comp.Name,
                ContactId = contactId.Id,
            });
    }

    private void HandleRequestStatePacket(
        Entity<MessengerServerComponent> server,
        RequestStateCommand cmd,
        DeviceNetworkPacketEvent args)
    {
        if (!AuthByIdCard(ref server.Comp, cmd.DeviceUid, out var contactId))
            return;

        if (ActiveServersFrequency(Transform(server).MapID).Count == 0)
            return;

        var accessedChats = server.Comp.GetPublicChats();
        accessedChats.UnionWith(server.Comp.GetPrivateChats(contactId));

        var chatsList = accessedChats.Select(server.Comp.GetChat).ToList();

        //
        // --- CONTACTS SYNC ---
        //

        var knownContacts = cmd.KnownContacts ?? [];
        var actualContactIds = new HashSet<uint>();

        foreach (var chat in chatsList)
        {
            actualContactIds.UnionWith(chat.MembersId);
        }

        actualContactIds.ExceptWith(knownContacts);

        if (actualContactIds.Count > 0)
        {
            var contacts = actualContactIds
                .Select(id => server.Comp.GetContact(new ContactId(id)))
                .ToList();

            Send(server,
                args.SenderAddress,
                new UpdateContactsCommand
                {
                    Contacts = contacts,
                });
        }

        //
        // --- MESSAGES SYNC ---
        //

        var knownMessages = cmd.KnownMessages ?? new HashSet<uint>();
        var requiredMessageIds = new HashSet<uint>();

        foreach (var chat in chatsList)
        {
            requiredMessageIds.UnionWith(chat.MessagesId);
        }

        requiredMessageIds.ExceptWith(knownMessages);

        if (requiredMessageIds.Count > 0)
        {
            var messages = requiredMessageIds
                .Select(id => server.Comp.GetMessage(new MessageId(id)))
                .ToList();

            Send(server,
                args.SenderAddress,
                new UpdateMessagesCommand
                {
                    Messages = messages,
                });
        }

        //
        // --- CHATS SYNC ---
        //

        var knownChats = cmd.KnownChats ?? new HashSet<uint>();
        var missingChats = new List<MessengerChat>();

        foreach (var chatId in accessedChats)
        {
            if (!knownChats.Contains(chatId.Id))
                missingChats.Add(server.Comp.GetChat(chatId));
        }

        if (missingChats.Count > 0)
        {
            Send(server,
                args.SenderAddress,
                new UpdateChatsCommand
                {
                    Chats = missingChats,
                });
        }
    }

    private void HandleSendMessage(Entity<MessengerServerComponent> ent,
        SendMessageCommand cmd,
        DeviceNetworkPacketEvent args)
    {
        // 1. auth
        if (!AuthByIdCard(ref ent.Comp, cmd.DeviceUid, out var contactId))
            return;

        if (ActiveServersFrequency(Transform(ent).MapID).Count == 0)
            return;

        var chatId = new ChatId(cmd.ChatId);

        var accessible = ent.Comp.GetPublicChats();
        accessible.UnionWith(ent.Comp.GetPrivateChats(contactId));

        if (!accessible.Contains(chatId))
        {
            _adminLogger.Add(LogType.MessengerServer,
                LogImpact.Low,
                $"Unauthorized message from {args.Sender}: chat {cmd.ChatId}");
            return;
        }

        // 2. build message
        var message = new MessengerMessage(
            cmd.ChatId,
            contactId.Id,
            _gameTiming.CurTime.Subtract(_gameTicker.RoundStartTimeSpan),
            cmd.MessageText);

        var msgKey = ent.Comp.AddMessage(message);
        message.Id = msgKey.Id;

        var chat = ent.Comp.GetChat(chatId);
        chat.MessagesId.Add(msgKey.Id);
        chat.LastMessageId = msgKey.Id;

        // 3. broadcast
        BroadcastToChat(ent,
            chatId,
            new NewMessageCommand
            {
                ChatId = chatId.Id,
                Message = message,
            });
    }

    private void HandleDeleteMessagesInChat(
        Entity<MessengerServerComponent> server,
        DeleteChatMessagesCommand cmd,
        DeviceNetworkPacketEvent args)
    {
        if (!AuthByIdCard(ref server.Comp, cmd.DeviceUid, out _))
            return;

        var chat = server.Comp.GetChat(new ChatId(cmd.ChatId));

        foreach (var messageId in chat.MessagesId)
        {
            server.Comp.DeleteMessage(new MessageId(messageId));
        }

        chat.MessagesId.Clear();
        chat.LastMessageId = null;

        Send(server,
            args.SenderAddress,
            new ChatMessageClearedCommand
            {
                ChatId = cmd.ChatId,
            });
    }

    private void OnNetworkPacket(Entity<MessengerServerComponent> ent, ref DeviceNetworkPacketEvent args)
    {
        if (!args.Data.TryGetValue(MessengerNetKeys.Command, out IMessengerCommand? msg))
            return;

        switch (msg)
        {
            case CheckServerCommand checkServer:
                HandleCheckServerPacket(ent, checkServer, args);
                break;

            case SendMessageCommand send:
                HandleSendMessage(ent, send, args);
                break;

            case RequestStateCommand req:
                HandleRequestStatePacket(ent, req, args);
                break;

            case DeleteChatMessagesCommand del:
                HandleDeleteMessagesInChat(ent, del, args);
                break;
        }
    }

    private bool AuthByIdCard(
        ref MessengerServerComponent component,
        EntityUid device,
        [NotNullWhen(true)] out ContactId? contactId)
    {
        contactId = null;

        if (!TryGetIdCard(device, out var idCardUid, out _))
            return false;

        return component.GetContactId(idCardUid.Value, out contactId);
    }

    private bool TryRegisterByIdCard(
        Entity<MessengerServerComponent> server,
        EntityUid device,
        string netAddress,
        [NotNullWhen(true)] out ContactId? contactId)
    {
        contactId = null;

        if (!TryGetIdCard(device, out var idCardUid, out var idCardComponent))
            return false;

        if (server.Comp.GetContactId(idCardUid.Value, out contactId))
            return true;

        RegisterAndInitNewClient(server.Owner,
            idCardUid.Value,
            idCardComponent.FullName ?? Loc.GetString("game-ticker-unknown-role"),
            netAddress,
            ref server.Comp,
            out contactId);

        return true;
    }

    private void Send(EntityUid server, string address, IMessengerCommand cmd)
    {
        var payload = new NetworkPayload
        {
            [MessengerNetKeys.Command] = cmd,
        };

        _deviceNetworkSystem.QueuePacket(server, address, payload);
    }

    public List<uint> ActiveServersFrequency(MapId mapId)
    {
        var activeServersFrequency = new List<uint>();

        var servers =
            EntityQuery<DeviceNetworkComponent, MessengerServerComponent, ApcPowerReceiverComponent,
                TransformComponent>();

        foreach (var (deviceNet, _, power, transform) in servers)
        {
            if (transform.MapID != mapId || !power.Powered)
                continue;

            if (deviceNet.ReceiveFrequency != null)
                activeServersFrequency.Add(deviceNet.ReceiveFrequency.Value);
        }

        return activeServersFrequency;
    }

    private bool AccessCheck(Entity<MessengerServerComponent> server, EntityUid device)
    {
        if (!TryGetIdCard(device, out var idCardUid, out _))
            return false;

        return _accessSystem.IsAllowed(idCardUid.Value, server.Owner);
    }

    private bool TryGetIdCard(
        EntityUid device,
        [NotNullWhen(true)] out EntityUid? idCardUid,
        [NotNullWhen(true)] out IdCardComponent? idCard)
    {
        idCardUid = null;
        idCard = null;

        if (!_containerSystem.TryGetContainer(device, PdaComponent.PdaIdSlotId, out var container))
            return false;

        foreach (var ent in container.ContainedEntities)
        {
            if (!TryComp(ent, out idCard))
                continue;

            idCardUid = ent;
            return true;
        }

        return false;
    }

    /// <summary>
    /// create new contact by clientEntityUid and init private chats for it
    /// </summary>
    /// <param name="serverUid">The EntityUid of the messenger server</param>
    /// <param name="clientEntityUid">Client EntityUid</param>
    /// <param name="fullName">Contact full name</param>
    /// <param name="netAddress">Installed client network address</param>
    /// <param name="component">MessengerServerComponent</param>
    /// <param name="newContactId">out newContactId of client</param>
    private void RegisterAndInitNewClient(
        EntityUid serverUid,
        EntityUid clientEntityUid,
        string fullName,
        string netAddress,
        ref MessengerServerComponent component,
        out ContactId newContactId)
    {
        newContactId = component.AddEntityContact(clientEntityUid, fullName, netAddress);

        // for now all entity can write others
        // add chat with yourself to private chats
        // and chats with every entity client
        var chatsList = new List<ChatId>();

        foreach (var (_, contact) in component.GetClientToContact())
        {
            var contactData = component.GetContact(contact);
            var newContactData = component.GetContact(newContactId);

            var chatName = $"{contactData.Name} and {newContactData.Name}";

            // when we add chat for contact its self, change name to favorites chat where user can write notes
            if (contact.Id == newContactId.Id)
            {
                chatName = Loc.GetString("messenger-ui-favorites-chat");
            }

            // create new chat
            var chatId = component.AddChat(
                new MessengerChat(chatName,
                    MessengerChatKind.Contact,
                    [contact.Id, newContactId.Id]));

            // add to list for later insert
            chatsList.Add(chatId);

            // add new chat to existed contact
            component.AddPrivateChats(contact, chatId);

            var chatState = component.GetChat(chatId);

            // sending net packet with new chat + new contact info для всех адресов контакта
            var updateChatCmd = new UpdateChatsCommand
            {
                Chats = [chatState]
            };

            var updateContactCmd = new UpdateContactsCommand
            {
                Contacts = [newContactData]
            };

            foreach (var activeAddress in contactData.ActiveAddresses)
            {
                Send(serverUid, activeAddress, updateChatCmd);
                Send(serverUid, activeAddress, updateContactCmd);
            }
        }

        // add created chats to new contact private chats
        component.AddPrivateChats(newContactId, chatsList);
    }

    /// <summary>
    /// If client already registered ui state can be fast restored to minimize network load
    /// </summary>
    /// <param name="loader">Device with loaded id card (for now only id card can be authenticator)</param>
    /// <param name="component">MessengerServerComponent</param>
    /// <param name="messengerUiState">out MessengerUiState</param>
    /// <returns>Returns true when the state was successfully restored</returns>
    public bool RestoreContactUIStateIdCard(
        EntityUid loader,
        ref MessengerServerComponent component,
        [NotNullWhen(true)] out MessengerUiState? messengerUiState)
    {
        messengerUiState = null;

        if (!TryGetIdCard(loader, out var idCardUid, out _))
            return false;

        if (!component.GetContactId(idCardUid.Value, out var contactId))
            return false;

        messengerUiState = RestoreContactUIState(ref component, contactId);

        return true;
    }

    private static MessengerUiState RestoreContactUIState(ref MessengerServerComponent component, ContactId contactId)
    {
        // get client contact
        var clientContact = component.GetContact(contactId);

        // get not active but accessed chats
        var accessedChats = component.GetPublicChats();
        accessedChats.UnionWith(component.GetPrivateChats(contactId));

        // select chats by accessedChats
        var chatsList = accessedChats.Select(component.GetChat).ToList();

        // select last messages and chat members contact keys
        var lastMessages = new List<MessengerMessage>();
        var membersContactIds = new HashSet<uint>();

        foreach (var messengerChat in chatsList)
        {
            if (messengerChat.LastMessageId != null)
                lastMessages.Add(component.GetMessage(new MessageId(messengerChat.LastMessageId.Value)));

            membersContactIds.UnionWith(messengerChat.MembersId);
        }

        // select contacts by contactIds
        var contacts = new List<MessengerContact>();
        foreach (var memberKey in membersContactIds)
        {
            contacts.Add(component.GetContact(new ContactId(memberKey)));
        }

        // start to make messengerUiState
        var messengerUi = new MessengerUiState
        {
            // add client contact
            ClientContact = clientContact,
        };

        // add available chats
        for (var i = 0; i < chatsList.Count; i++)
        {
            var state = new MessengerChatUiState(
                chatsList[i].Id,
                chatsList[i].Name,
                chatsList[i].Kind,
                chatsList[i].MembersId,
                chatsList[i].MessagesId,
                chatsList[i].LastMessageId,
                i);

            messengerUi.Chats.Add(chatsList[i].Id, state);
        }

        // add last messages to messages state store
        foreach (var msg in lastMessages)
        {
            messengerUi.Messages.Add(msg.Id, msg);
        }

        // add contacts
        foreach (var contact in contacts)
        {
            messengerUi.Contacts.Add(contact.Id, contact);
        }

        return messengerUi;
    }
}
