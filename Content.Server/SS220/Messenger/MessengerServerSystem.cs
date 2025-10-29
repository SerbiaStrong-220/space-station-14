// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Power.Components;
using Content.Server.SS220.CartridgeLoader.Cartridges;
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
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly DeviceNetworkSystem _deviceNetworkSystem = default!;
    [Dependency] private readonly AccessReaderSystem _accessSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedGameTicker _gameTicker = default!;

    /// <summary>
    /// Enum representing different command types transmitted via messenger packets.
    /// </summary>
    public enum NetworkCommand
    {
        /// <summary>
        /// Response containing basic server info (e.g., name).
        /// </summary>
        Info,

        /// <summary>
        /// Server pushes contact info to the client.
        /// </summary>
        Contact,

        /// <summary>
        /// Sends the client's own contact identity or registration data.
        /// Used to confirm or propagate client identity.
        /// </summary>
        ClientContact,

        /// <summary>
        /// Sends chat information to the client.
        /// Used to update or initialize chat sessions.
        /// </summary>
        Chat,

        /// <summary>
        /// Sends message history or new messages to the client for one or more chats.
        /// Used to synchronize conversation content.
        /// </summary>
        Messages,

        /// <summary>
        /// Indicates that the client has sent a new message.
        /// Triggers server-side handling and redistribution.
        /// </summary>
        NewMessage,

        /// <summary>
        /// Requests deletion of all messages within a specific chat.
        /// Intended for targeted cleanup by the client.
        /// </summary>
        DeleteAllMessageInOneChat,

        /// <summary>
        /// Requests deletion of all messages across all chats associated with the client.
        /// Used for full chat history cleanup.
        /// </summary>
        DeleteAllMessageInAllChat,
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

        // update contact info if changed
        foreach (var server in _entityManager.EntityQuery<MessengerServerComponent>())
        {
            foreach (var (entityUid, contactKey) in server.GetClientToContact())
            {
                if (!_entityManager.TryGetComponent<IdCardComponent>(entityUid, out var card))
                    continue;

                if (string.IsNullOrEmpty(card.FullName))
                    continue;

                server.UpdateContactName(contactKey, card.FullName);
            }
        }
    }

    /// <summary>
    /// Handles the server info request (NetworkCommand.Info) from a client device.
    /// Performs access authorization and registration via ID card, then sends back server name.
    /// Uses TryGetValue internally to validate client payload and network identity.
    /// </summary>
    private void HandleCheckServerPacket(Entity<MessengerServerComponent> server, DeviceNetworkPacketEvent args)
    {
        // check authorization to server before registration
        if (!AccessCheck(server, args.Data))
            return;

        // register client by it id card, which must be inserted in device
        if (!TryRegisterByIdCard(server, args.SenderAddress, ref server.Comp, args.Data, out _))
            return;

        var payload = new NetworkPayload
        {
            [nameof(NetworkKey.Command)] = NetworkCommand.Info,
            [nameof(NetworkKey.ServerName)] = server.Comp.Name,
        };

        SendResponse(server, args, payload);
    }

    /// <summary>
    /// Handles a client state synchronization request sent via network packet.
    /// Authenticates the client using their ID card, determines which contacts, messages, and chats the client is missing,
    /// and sends only the delta data required for synchronization.
    /// Uses NetworkCommand.Contact, Messages, and Chat, and inspects packet contents using TryGetValue with keys such as
    /// ContactsIds, MessagesIds, and CurrentChatIds.
    /// </summary>
    /// <param name="server">The messenger server entity and its component.</param>
    /// <param name="args">The network packet event containing data and sender information.</param>
    private void HandleStateUpdatePacket(Entity<MessengerServerComponent> server, DeviceNetworkPacketEvent args)
    {
        if (!AuthByIdCard(ref server.Comp, args.Data, out var contactKey))
            return;

        var accessedChats = server.Comp.GetPublicChats();
        accessedChats.UnionWith(server.Comp.GetPrivateChats(contactKey));

        var chatsList = accessedChats.Select(server.Comp.GetChat).ToList();

        /*contacts*/
        if (!ParseIdHashSet(
                nameof(MessengerClientCartridgeSystem.NetworkKey.ContactsIds),
                args.Data,
                out var contacts))
            contacts = new HashSet<uint>();

        var membersContactKeys = new HashSet<uint>();

        foreach (var messengerChat in chatsList)
        {
            membersContactKeys.UnionWith(messengerChat.MembersId);
        }

        membersContactKeys.ExceptWith(contacts);

        var contactsInfo = membersContactKeys.Select(key => server.Comp.GetContact(new ContactKey(key))).ToList();

        if (contactsInfo.Count > 0)
        {
            var payload = new NetworkPayload
            {
                [nameof(NetworkKey.Command)] = NetworkCommand.Contact,
                [nameof(NetworkKey.ContactList)] = contactsInfo,
            };

            SendResponse(server, args, payload);
        }
        /*contacts*/

        /*messages*/
        if (!ParseIdHashSet(nameof(MessengerClientCartridgeSystem.NetworkKey.MessagesIds),
                args.Data,
                out var messages))
            messages = new HashSet<uint>();

        var messagesKeys = new HashSet<uint>();

        foreach (var chat in chatsList)
        {
            messagesKeys.UnionWith(chat.MessagesId);
        }

        messagesKeys.ExceptWith(messages);

        var messagesList = messagesKeys.Select(messageId => server.Comp.GetMessage(new MessageKey(messageId)))
            .ToList();

        if (messagesList.Count > 0)
        {
            var payload = new NetworkPayload
            {
                [nameof(NetworkKey.Command)] = NetworkCommand.Messages,
                [nameof(NetworkKey.MessageList)] = messagesList,
            };

            SendResponse(server, args, payload);
        }
        /*messages*/

        /*chats*/
        if (!ParseIdHashSet(
                nameof(MessengerClientCartridgeSystem.NetworkKey.CurrentChatIds),
                args.Data,
                out var chats))
            chats = new HashSet<uint>();

        var updateChats = new HashSet<ChatKey>();

        foreach (var accessedChat in accessedChats)
        {
            if (!chats.Contains(accessedChat.Id))
            {
                updateChats.Add(accessedChat);
            }
        }

        var updateChatsList = updateChats.Select(server.Comp.GetChat).ToList();

        if (updateChatsList.Count > 0)
        {
            var payload = new NetworkPayload
            {
                [nameof(NetworkKey.Command)] = NetworkCommand.Chat,
                [nameof(NetworkKey.ChatList)] = updateChatsList,
            };

            SendResponse(server, args, payload);
        }
        /*chats*/
    }

    private void HandleMessageCheckPacket(Entity<MessengerServerComponent> server, DeviceNetworkPacketEvent args)
    {
        if (!AuthByIdCard(ref server.Comp, args.Data, out var contactKey))
            return; // cannot authenticate

        if (!ParseId(
                nameof(MessengerClientCartridgeSystem.NetworkKey.ChatId),
                args.Data,
                out var chatId))
            return; // no chat id received

        if (!ParseMessageText(
                nameof(MessengerClientCartridgeSystem.NetworkKey.MessageText),
                args.Data,
                out var messageText))
            return; // no message received

        var chatKey = new ChatKey(chatId.Value);

        var accessedChats = server.Comp.GetPublicChats();
        accessedChats.UnionWith(server.Comp.GetPrivateChats(contactKey));

        var chat = server.Comp.GetChat(chatKey);

        if (!accessedChats.Contains(chatKey))
        {
            _adminLogger.Add(LogType.MessengerServer,
                LogImpact.Low,
                $"No access: sender entity: {args.Sender}, chat: {chat.Name}, chatID: {chat.Id}, msg: {messageText}");
            return; // no access
        }

        // create new message
        var message = new MessengerMessage(
            chatKey.Id,
            contactKey.Id,
            _gameTiming.CurTime.Subtract(_gameTicker.RoundStartTimeSpan),
            messageText);

        var messageKey = server.Comp.AddMessage(message);
        message.Id = messageKey.Id;

        // assign new message to chat index
        chat.LastMessageId = messageKey.Id;
        chat.MessagesId.Add(messageKey.Id);

        foreach (var chatMember in chat.MembersId)
        {
            foreach (var activeAddress in server.Comp.GetContact(new ContactKey(chatMember)).ActiveAddresses)
            {
                var payload = new NetworkPayload
                {
                    [nameof(NetworkKey.Command)] = NetworkCommand.NewMessage,
                    [nameof(NetworkKey.ChatId)] = chatKey.Id,
                    [nameof(NetworkKey.Message)] = message,
                };

                _deviceNetworkSystem.QueuePacket(server, activeAddress, payload);
            }
        }

        _adminLogger.Add(LogType.MessengerServer,
            LogImpact.Low,
            $"Send: sender entity: {args.Sender}, chat: {chat.Name}, chatID: {chat.Id}, msg: {messageText}");
    }

    private void HandleDeleteMsgInOneChat(Entity<MessengerServerComponent> server, DeviceNetworkPacketEvent args)
    {
        if (!args.Data.TryGetValue(nameof(NetworkKey.ChatId), out uint chatId))
            return;

        var chat = server.Comp.GetChat(new ChatKey(chatId));

        foreach (var messageId in chat.MessagesId)
        {
            server.Comp.DeleteMessage(new MessageKey(messageId));
        }

        chat.MessagesId.Clear();
        chat.LastMessageId = null;

        var payload = new NetworkPayload
        {
            [nameof(NetworkKey.Command)] = NetworkCommand.DeleteAllMessageInOneChat,
            [nameof(NetworkKey.ChatId)] = chatId
        };

        SendResponse(server, args, payload);
    }

    private void HandleDeleteAllMsgInOneChat(Entity<MessengerServerComponent> server, DeviceNetworkPacketEvent args)
    {
        server.Comp.ClearAllMessages();

        var payload = new NetworkPayload
        {
            [nameof(NetworkKey.Command)] = NetworkCommand.DeleteAllMessageInAllChat,
        };

        SendResponse(server, args, payload);
    }

    private void OnNetworkPacket(EntityUid uid, MessengerServerComponent? component, DeviceNetworkPacketEvent args)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!args.Data.TryGetValue(nameof(MessengerClientCartridgeSystem.NetworkKey.Command),
                out MessengerClientCartridgeSystem.NetworkCommand? msg))
            return;

        switch (msg)
        {
            case MessengerClientCartridgeSystem.NetworkCommand.CheckServer:
            {
                var server = (uid, component);
                HandleCheckServerPacket(server, args);
                break;
            }
            case MessengerClientCartridgeSystem.NetworkCommand.StateUpdate:
            {
                var server = (uid, component);
                HandleStateUpdatePacket(server, args);
                break;
            }

            case MessengerClientCartridgeSystem.NetworkCommand.MessageSend:
            {
                var server = (uid, component);
                HandleMessageCheckPacket(server, args);
                break;
            }

            case MessengerClientCartridgeSystem.NetworkCommand.DeleteMessageInOneChat:
            {
                var server = (uid, component);
                HandleDeleteMsgInOneChat(server, args);
                break;
            }

            case MessengerClientCartridgeSystem.NetworkCommand.DeleteMessageInAllChat:
            {
                var server = (uid, component);
                HandleDeleteAllMsgInOneChat(server, args);
                break;
            }
        }
    }

    /// <summary>
    /// Find loader entity id in payload data and trying to get id card in this device,
    /// id card will be used as client authenticate entity
    /// if id card registered, return contactKey
    /// else return false
    /// </summary>
    /// <param name="component">MessengerServerComponent</param>
    /// <param name="payload">NetworkPayload of network packet</param>
    /// <param name="contactKey">out contactKey of client</param>
    /// <returns>Returns true when the component was successfully received.</returns>
    private bool AuthByIdCard(
        ref MessengerServerComponent component,
        NetworkPayload payload,
        [NotNullWhen(true)] out ContactKey? contactKey)
    {
        contactKey = null;

        if (!GetIdCardComponent(payload, out var idCardUid, out _))
            return false;

        return component.GetContactKey(idCardUid.Value, out contactKey);
    }

    /// <summary>
    /// Find loader entity id in payload data and trying to get id card in this device,
    /// id card will be used as client authenticate entity
    /// if id card registered, return contactKey without registration
    /// if not, create new contact
    /// </summary>
    /// <param name="serverUid">The EntityUid of the messenger server</param>
    /// <param name="netAddress">The network address of the device with installed client</param>
    /// <param name="component">MessengerServerComponent</param>
    /// <param name="payload">NetworkPayload of network packet</param>
    /// <param name="contactKey">out contactKey of client</param>
    /// <returns>Returns true when the component was successfully received.</returns>
    private bool TryRegisterByIdCard(
        EntityUid serverUid,
        string netAddress,
        ref MessengerServerComponent component,
        NetworkPayload payload,
        [NotNullWhen(true)] out ContactKey? contactKey)
    {
        contactKey = null;

        if (!GetIdCardComponent(payload, out var idCardUid, out var idCardComponent))
            return false;

        if (component.GetContactKey(idCardUid.Value, out contactKey))
            return true;

        RegisterAndInitNewClient(serverUid,
            idCardUid.Value,
            idCardComponent.FullName ?? Loc.GetString("game-ticker-unknown-role"),
            netAddress,
            ref component,
            out contactKey);

        return true;
    }

    private bool GetIdCardComponent(
        NetworkPayload payload,
        [NotNullWhen(true)] out EntityUid? idCardUid,
        [NotNullWhen(true)] out IdCardComponent? idCardComponent)
    {
        idCardUid = null;
        idCardComponent = null;

        if (payload.TryGetValue(nameof(MessengerClientCartridgeSystem.NetworkKey.DeviceUid), out NetEntity? netLoader))
            return GetIdCardComponent(GetEntity(netLoader), out idCardUid, out idCardComponent);

        if (payload.TryGetValue(nameof(MessengerClientCartridgeSystem.NetworkKey.DeviceUid), out EntityUid? loader))
            return GetIdCardComponent(loader, out idCardUid, out idCardComponent);

        return false;
    }

    private void SendResponse(EntityUid uid, DeviceNetworkPacketEvent args, NetworkPayload payload)
    {
        _deviceNetworkSystem.QueuePacket(uid, args.SenderAddress, payload);
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

    private bool AccessCheck(EntityUid serverUid, NetworkPayload payload)
    {
        if (!GetIdCardComponent(payload, out var idCardUid, out _))
            return false;

        return _accessSystem.IsAllowed(idCardUid.Value, serverUid);
    }

    private bool GetIdCardComponent(EntityUid? loaderUid,
        [NotNullWhen(true)] out EntityUid? idCardUid,
        [NotNullWhen(true)] out IdCardComponent? idCardComponent)
    {
        idCardComponent = null;
        idCardUid = null;

        if (loaderUid == null)
            return false;

        if (!_containerSystem.TryGetContainer(loaderUid.Value, PdaComponent.PdaIdSlotId, out var container))
            return false;

        foreach (var idCard in container.ContainedEntities)
        {
            if (!TryComp(idCard, out idCardComponent))
                continue;

            idCardUid = idCard;

            return true;
        }

        return false;
    }

    private static bool ParseId(string key, NetworkPayload payload, [NotNullWhen(true)] out uint? v)
    {
        v = null;
        if (!payload.TryGetValue(key, out uint? value))
            return false;
        v = value.Value;
        return true;
    }

    private static bool ParseMessageText(string key, NetworkPayload payload, [NotNullWhen(true)] out string? v)
    {
        v = null;
        if (!payload.TryGetValue(key, out string? value))
            return false;
        v = value;
        return true;
    }

    private static bool ParseIdHashSet(string key, NetworkPayload payload, [NotNullWhen(true)] out HashSet<uint>? v)
    {
        v = null;
        if (!payload.TryGetValue(key, out HashSet<uint>? value))
            return false;
        v = value;
        return true;
    }

    /// <summary>
    /// create new contact by clientEntityUid and init private chats for it
    /// </summary>
    /// <param name="serverUid">The EntityUid of the messenger server</param>
    /// <param name="clientEntityUid">Client EntityUid</param>
    /// <param name="fullName">Contact full name</param>
    /// <param name="netAddress">Installed client network address</param>
    /// <param name="component">MessengerServerComponent</param>
    /// <param name="newContactKey">out newContactKey of client</param>
    /// <returns>Returns true when the component was successfully received.</returns>
    private void RegisterAndInitNewClient(
        EntityUid serverUid,
        EntityUid clientEntityUid,
        string fullName,
        string netAddress,
        ref MessengerServerComponent component,
        out ContactKey newContactKey)
    {
        newContactKey = component.AddEntityContact(clientEntityUid, fullName, netAddress);

        // for now all entity can write others
        // add chat with yourself to private chats
        // and chats with every entity client
        var chatsList = new List<ChatKey>();

        foreach (var (_, contact) in component.GetClientToContact())
        {
            var chatName = component.GetContact(contact).Name + " and " + component.GetContact(newContactKey).Name;

            // when we add chat for contact its self, change name to favorites chat where user can write notes
            if (contact.Id == newContactKey.Id)
            {
                chatName = Loc.GetString("messenger-ui-favorites-chat");
            }

            // create new chat
            var chatKey = component.AddChat(
                new MessengerChat(chatName,
                    MessengerChatKind.Contact,
                    new HashSet<uint> { contact.Id, newContactKey.Id }));

            // add to list for letter insert
            chatsList.Add(chatKey);

            // add new chat to existed contact
            component.AddPrivateChats(contact, chatKey);

            // sending net packet with new chat to every contact address
            // contact can have many clients connected
            foreach (var activeAddress in component.GetContact(contact).ActiveAddresses)
            {
                var payload = new NetworkPayload
                {
                    [nameof(NetworkKey.Command)] = NetworkCommand.Chat,
                    [nameof(NetworkKey.Chat)] = component.GetChat(chatKey),
                };

                _deviceNetworkSystem.QueuePacket(serverUid, activeAddress, payload);
            }

            // sending new contact info to contact client
            foreach (var activeAddress in component.GetContact(contact).ActiveAddresses)
            {
                var payload = new NetworkPayload
                {
                    [nameof(NetworkKey.Command)] = NetworkCommand.Contact,
                    [nameof(NetworkKey.Contact)] = component.GetContact(newContactKey),
                };

                _deviceNetworkSystem.QueuePacket(serverUid, activeAddress, payload);
            }
        }

        // add created chats to new contact private chats
        component.AddPrivateChats(newContactKey, chatsList);
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

        if (!GetIdCardComponent(loader, out var idCardUid, out _))
            return false;

        if (!component.GetContactKey(idCardUid.Value, out var contactKey))
            return false;

        messengerUiState = RestoreContactUIState(ref component, contactKey);

        return true;
    }

    private static MessengerUiState RestoreContactUIState(ref MessengerServerComponent component, ContactKey contactKey)
    {
        // get client contact
        var clientContact = component.GetContact(contactKey);

        // get not active but accessed chats
        var accessedChats = component.GetPublicChats();
        accessedChats.UnionWith(component.GetPrivateChats(contactKey));

        // select chats by accessedChats
        var chatsList = accessedChats.Select(component.GetChat).ToList();

        // select last messages and chat members contact keys
        var lastMessages = new List<MessengerMessage>();
        var membersContactKeys = new HashSet<uint>();

        foreach (var messengerChat in chatsList)
        {
            if (messengerChat.LastMessageId != null)
                lastMessages.Add(component.GetMessage(new MessageKey(messengerChat.LastMessageId.Value)));

            membersContactKeys.UnionWith(messengerChat.MembersId);
        }

        // select contacts by contactKeys
        var contacts = new List<MessengerContact>();
        foreach (var memberKey in membersContactKeys)
        {
            contacts.Add(component.GetContact(new ContactKey(memberKey)));
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
