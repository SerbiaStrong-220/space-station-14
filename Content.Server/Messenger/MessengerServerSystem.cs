// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.CartridgeLoader.Cartridges;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Power.Components;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.Messenger;
using Content.Shared.PDA;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server.Messenger;

public sealed class MessengerServerSystem : EntitySystem
{
    [Dependency] private readonly DeviceNetworkSystem? _deviceNetworkSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly AccessReaderSystem _accessSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;

    public const string MessengerServerCommand = "messenger_command";
    public const string MessengerServerCommandInfo = "messenger_command_info";
    public const string MessengerServerCommandContact = "messenger_command_contact";
    public const string MessengerServerCommandClientContact = "messenger_command_client_contact";

    public const string MessengerServerCommandChat = "messenger_command_chat";

    public const string MessengerServerCommandMessages = "messenger_command_messages";
    public const string MessengerServerCommandNewMessage = "messenger_command_new_message";

    public const string MessengerServerName = "messenger_server_name";

    public const string MessengerChat = "messenger_chat";
    public const string MessengerChatList = "messenger_chat_list";

    public const string MessengerMessage = "messenger_message";
    public const string MessengerMessageList = "messenger_message_list";

    public const string MessengerContact = "messenger_contact_info";
    public const string MessengerContactList = "messenger_contact_info_list";

    public const string MessengerChatId = "messenger_chat_id";

    private TimeSpan _nexUpdate;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MessengerServerComponent, DeviceNetworkPacketEvent>(OnNetworkPacket);
    }

    public override void Update(float frameTime)
    {
        if (_nexUpdate >= _gameTiming.CurTime)
            return;

        _nexUpdate = _gameTiming.CurTime.Add(TimeSpan.FromSeconds(30));

        // update contact info if changed
        foreach (var server in _entityManager.EntityQuery<MessengerServerComponent>())
        {
            foreach (var (entityUid, contactKey) in server.GetClientToContact())
            {
                if (!_entityManager.TryGetComponent<IdCardComponent>(entityUid, out var card))
                    continue;

                server.UpdateContactName(contactKey, card.FullName);
            }
        }
    }

    private void OnNetworkPacket(EntityUid uid, MessengerServerComponent? component, DeviceNetworkPacketEvent args)
    {
        if (!Resolve(uid, ref component))
            return;

        if (!args.Data.TryGetValue(MessengerClientCartridgeSystem.MessengerClientCommand, out string? msg))
            return;

        switch (msg)
        {
            case MessengerClientCartridgeSystem.MessengerClientCommandCheckServer:
            {
                // check authorization to server before registration
                if (!AccessCheck(uid, args.Data))
                    break;
                // register client by it id card, which must be inserted in device
                if (!RegisterByIdCard(uid, args.SenderAddress, ref component, args.Data, out _))
                    break;

                SendResponse(uid, args, new NetworkPayload
                {
                    [MessengerServerCommand] = MessengerServerCommandInfo,
                    [MessengerServerName] = component.Name,
                });
                break;
            }
            case MessengerClientCartridgeSystem.MessengerClientCommandStateUpdate:
            {
                if (!AuthByIdCard(ref component, args.Data, out var contactKey))
                    break;

                var accessedChats = component.GetPublicChats();
                accessedChats.UnionWith(component.GetPrivateChats(contactKey));

                var chatsList = accessedChats.Select(component.GetChat).ToList();

                /*contacts*/
                if (!ParseIdHashSet(MessengerClientCartridgeSystem.MessengerClientContactsIds, args.Data,
                        out var contacts))
                    contacts = new HashSet<uint>();

                var membersContactKeys = new HashSet<uint>();

                foreach (var messengerChat in chatsList)
                {
                    membersContactKeys.UnionWith(messengerChat.Members);
                }

                membersContactKeys.ExceptWith(contacts);

                var contactsInfo = membersContactKeys.Select(key => component.GetContact(new ContactKey(key))).ToList();

                if (contactsInfo.Count > 0)
                {
                    SendResponse(uid, args, new NetworkPayload
                    {
                        [MessengerServerCommand] = MessengerServerCommandContact,
                        [MessengerContactList] = contactsInfo,
                    });
                }
                /*contacts*/

                /*messages*/
                if (!ParseIdHashSet(MessengerClientCartridgeSystem.MessengerClientMessagesIds, args.Data,
                            out var messages))
                    messages = new HashSet<uint>();

                var messagesKeys = new HashSet<uint>();

                foreach (var chat in chatsList)
                {
                    messagesKeys.UnionWith(chat.Messages);
                }

                messagesKeys.ExceptWith(messages);

                var messagesList = messagesKeys.Select(messageId => component.GetMessage(new MessageKey(messageId)))
                    .ToList();

                if (messagesList.Count > 0)
                {
                    SendResponse(uid, args, new NetworkPayload
                    {
                        [MessengerServerCommand] = MessengerServerCommandMessages,
                        [MessengerMessageList] = messagesList,
                    });
                }
                /*messages*/

                /*chats*/
                if (!ParseIdHashSet(MessengerClientCartridgeSystem.MessengerClientCurrentChatIds, args.Data,
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

                var updateChatsList = updateChats.Select(component.GetChat).ToList();

                if (updateChatsList.Count > 0)
                {
                    SendResponse(uid, args, new NetworkPayload
                    {
                        [MessengerServerCommand] = MessengerServerCommandChat,
                        [MessengerChatList] = updateChatsList,
                    });
                }
                /*chats*/

                break;
            }

            case MessengerClientCartridgeSystem.MessengerClientCommandMessageSend:
            {
                if (!AuthByIdCard(ref component, args.Data, out var contactKey))
                    break; // can not authenticate

                if (!ParseId(MessengerClientCartridgeSystem.MessengerClientChatId, args.Data, out var chatId))
                    break; // no chat id received

                if (!ParseMessageText(MessengerClientCartridgeSystem.MessengerClientMessage, args.Data,
                        out var messageText))
                    break; // no message received

                var chatKey = new ChatKey(chatId.Value);

                var accessedChats = component.GetPublicChats();
                accessedChats.UnionWith(component.GetPrivateChats(contactKey));

                if (!accessedChats.Contains(chatKey))
                    break; // no access

                // create new message
                var message = new MessengerMessage(chatKey.Id, contactKey.Id, _gameTiming.CurTime, messageText);
                var messageKey =
                    component.AddMessage(message);
                message.Id = messageKey.Id;

                // assign new message to chat index
                var chat = component.GetChat(chatKey);
                chat.LastMessage = messageKey.Id;
                chat.Messages.Add(messageKey.Id);

                foreach (var chatMember in chat.Members)
                {
                    foreach (var activeAddress in component.GetContact(new ContactKey(chatMember)).ActiveAddresses)
                    {
                        _deviceNetworkSystem?.QueuePacket(uid, activeAddress, new NetworkPayload
                        {
                            [MessengerServerCommand] = MessengerServerCommandNewMessage,
                            [MessengerChatId] = chatKey.Id,
                            [MessengerMessage] = message,
                        });
                    }
                }

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
    private bool AuthByIdCard(ref MessengerServerComponent component, NetworkPayload payload,
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
    private bool RegisterByIdCard(EntityUid serverUid, string netAddress, ref MessengerServerComponent component,
        NetworkPayload payload, [NotNullWhen(true)] out ContactKey? contactKey)
    {
        contactKey = null;

        if (!GetIdCardComponent(payload, out var idCardUid, out var idCardComponent))
            return false;

        if (component.GetContactKey(idCardUid.Value, out contactKey))
            return true;

        RegisterAndInitNewClient(serverUid, idCardUid.Value, idCardComponent.FullName ?? "", netAddress, ref component,
            out contactKey);

        return true;
    }

    private bool GetIdCardComponent(NetworkPayload payload, [NotNullWhen(true)] out EntityUid? idCardUid, [NotNullWhen(true)] out IdCardComponent? idCardComponent)
    {
        idCardUid = null;
        idCardComponent = null;

        if (!payload.TryGetValue(MessengerClientCartridgeSystem.MessengerClientDevice, out EntityUid? loader))
            return false;

        return GetIdCardComponent(loader, out idCardUid, out idCardComponent);
    }

    private void SendResponse(EntityUid uid, DeviceNetworkPacketEvent args, NetworkPayload payload)
    {
        _deviceNetworkSystem?.QueuePacket(uid, args.SenderAddress, payload);
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

        if (!EntityManager.TryGetComponent(serverUid, out AccessReaderComponent? reader))
            return false;

        if (!_accessSystem.IsAllowed(idCardUid.Value, reader))
            return false;


        return true;
    }

    private bool GetIdCardComponent(EntityUid? loaderUid, [NotNullWhen(true)] out EntityUid? idCardUid,
        [NotNullWhen(true)] out IdCardComponent? idCardComponent)
    {
        idCardComponent = null;
        idCardUid = null;

        if (loaderUid == null)
            return false;

        if (!_containerSystem.TryGetContainer(loaderUid.Value, PDAComponent.PDAIdSlotId, out var container))
            return false;

        foreach (var idCard in container.ContainedEntities)
        {
            if (!_entityManager.TryGetComponent(idCard, out idCardComponent))
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
    private void RegisterAndInitNewClient(EntityUid serverUid, EntityUid clientEntityUid, string fullName, string netAddress,
        ref MessengerServerComponent component, out ContactKey newContactKey)
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
            var chatKey = component.AddChat(new MessengerChat(chatName, MessengerChatKind.Contact,
                new HashSet<uint> { contact.Id, newContactKey.Id }));

            // add to list for letter insert
            chatsList.Add(chatKey);

            // add new chat to existed contact
            component.AddPrivateChats(contact, chatKey);

            // sending net packet with new chat to every contact address
            // contact can have many clients connected
            foreach (var activeAddress in component.GetContact(contact).ActiveAddresses)
            {
                _deviceNetworkSystem?.QueuePacket(serverUid, activeAddress, new NetworkPayload
                {
                    [MessengerServerCommand] = MessengerServerCommandChat,
                    [MessengerChat] = component.GetChat(chatKey),
                });
            }

            // sending new contact info to contact client
            foreach (var activeAddress in component.GetContact(contact).ActiveAddresses)
            {
                _deviceNetworkSystem?.QueuePacket(serverUid, activeAddress, new NetworkPayload
                {
                    [MessengerServerCommand] = MessengerServerCommandContact,
                    [MessengerContact] = component.GetContact(newContactKey),
                });
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
    public bool RestoreContactUIStateIdCard(EntityUid loader, ref MessengerServerComponent component,
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
            if (messengerChat.LastMessage != null)
                lastMessages.Add(component.GetMessage(new MessageKey(messengerChat.LastMessage.Value)));

            membersContactKeys.UnionWith(messengerChat.Members);
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
            ClientContact = clientContact
        };

        // add available chats
        for (var i = 0; i < chatsList.Count; i++)
        {
            messengerUi.Chats.Add(chatsList[i].Id,
                new MessengerChatUiState(chatsList[i].Id, chatsList[i].Name, chatsList[i].Kind, chatsList[i].Members,
                    chatsList[i].Messages, chatsList[i].LastMessage, i));
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
