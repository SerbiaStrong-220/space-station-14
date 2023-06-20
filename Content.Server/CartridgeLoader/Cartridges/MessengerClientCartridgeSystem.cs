// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Messenger;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.Messenger;
using Content.Shared.PDA.Ringer;

namespace Content.Server.CartridgeLoader.Cartridges;

public sealed class MessengerClientCartridgeSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly CartridgeLoaderSystem? _cartridgeLoaderSystem = default!;
    [Dependency] private readonly DeviceNetworkSystem? _deviceNetworkSystem = default!;
    [Dependency] private readonly MessengerServerSystem _messengerServerSystem = default!;


    public const string MessengerClientCommand = "messenger_command";
    public const string MessengerClientDevice = "messenger_command_loader";

    public const string MessengerClientCommandCheckServer = "messenger_command_check_server";

    public const string MessengerClientCommandStateUpdate = "messenger_command_state_update";

    public const string MessengerClientCommandMessageSend = "messenger_command_message_send";

    public const string MessengerClientChatId = "messenger_client_chat_id";
    public const string MessengerClientMessage = "messenger_client_message";

    public const string MessengerClientCurrentChatIds = "messenger_client_current_chat_ids";
    public const string MessengerClientContactsIds = "messenger_client_contacts_ids";
    public const string MessengerClientMessagesIds = "messenger_client_messages_ids";

    // queue for ui states, if sent too often, then some states are lost,
    // send one state per update
    private readonly Queue<QueueBoundUserInterfaceState> _statesQueue = new();

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MessengerClientCartridgeComponent, CartridgeMessageEvent>(OnUiMessage);
        SubscribeLocalEvent<MessengerClientCartridgeComponent, CartridgeUiReadyEvent>(OnUiReady);
        SubscribeLocalEvent<MessengerClientCartridgeComponent, CartridgeAddedEvent>(OnInstall);
        SubscribeLocalEvent<MessengerClientCartridgeComponent, DeviceNetworkPacketEvent>(OnNetworkPacket);
    }

    public override void Update(float frameTime)
    {
        // send one state per update
        if (_statesQueue.TryDequeue(out var state))
        {
            if (state.State != null)
                _cartridgeLoaderSystem?.UpdateCartridgeUiState(state.LoaderUid, state.State);
        }

        // is component request full state, try to get it from server
        foreach (var clientCartridgeComponent in _entityManager.EntityQuery<MessengerClientCartridgeComponent>())
        {
            if (!clientCartridgeComponent.SendState)
                continue;

            if (clientCartridgeComponent.ActiveServer == null)
                continue;

            if (!_entityManager.TryGetComponent<MessengerServerComponent>(
                    clientCartridgeComponent.ActiveServer.Value, out var server))
                continue;

            if (!_messengerServerSystem.RestoreContactUIStateIdCard(clientCartridgeComponent.Loader, ref server,
                    out var messengerUiState))
                continue;

            UpdateMessengerUiState(clientCartridgeComponent.Loader, messengerUiState);
            clientCartridgeComponent.SendState = false;
        }
    }

    private void OnNetworkPacket(EntityUid uid, MessengerClientCartridgeComponent? component,
        DeviceNetworkPacketEvent args)
    {
        if (!args.Data.TryGetValue(MessengerServerSystem.MessengerServerCommand, out string? command))
            return;

        if (!Resolve(uid, ref component))
            return;

        switch (command)
        {
            // when receive msg about server info add this server to servers list, and if received add server name
            case MessengerServerSystem.MessengerServerCommandInfo:
            {
                var serverInfo = new ServerInfo();

                if (args.Data.TryGetValue(MessengerServerSystem.MessengerServerName, out string? serverName))
                    serverInfo.Name = serverName;

                serverInfo.Address = args.SenderAddress;

                component.ActiveServer ??= args.Sender;

                if (!component.Servers.TryAdd(args.Sender, serverInfo))
                {
                    component.Servers.Remove(args.Sender);
                    component.Servers.Add(args.Sender, serverInfo);
                }

                component.SendState = true;
                break;
            }
            // receive client contact info
            case MessengerServerSystem.MessengerServerCommandClientContact:
            {
                if (!args.Data.TryGetValue(MessengerServerSystem.MessengerContact, out MessengerContact? contact))
                    break;

                UpdateMessengerUiState(component.Loader, new MessengerClientContactUiState(contact));
                break;
            }
            // receive contact info
            case MessengerServerSystem.MessengerServerCommandContact:
            {
                var contactsUpdate = new List<MessengerContact>();

                if (args.Data.TryGetValue(MessengerServerSystem.MessengerContact, out MessengerContact? contact))
                {
                    contactsUpdate.Add(contact);
                }

                if (args.Data.TryGetValue(MessengerServerSystem.MessengerContactList,
                        out List<MessengerContact>? contacts))
                {
                    contactsUpdate.AddRange(contacts);
                }

                if (contactsUpdate.Count > 0)
                    UpdateMessengerUiState(component.Loader, new MessengerContactUiState(contactsUpdate));
                break;
            }
            // receive chat info
            case MessengerServerSystem.MessengerServerCommandChat:
            {
                var updateChats = new List<MessengerChat>();

                if (args.Data.TryGetValue(MessengerServerSystem.MessengerChat, out MessengerChat? chat))
                {
                    updateChats.Add(chat);
                }

                if (args.Data.TryGetValue(MessengerServerSystem.MessengerChatList, out List<MessengerChat>? chats))
                {
                    updateChats.AddRange(chats);
                }

                if (updateChats.Count > 0)
                    UpdateMessengerUiState(component.Loader, new MessengerChatUpdateUiState(updateChats));
                break;
            }
            // receive message info
            case MessengerServerSystem.MessengerServerCommandMessages:
            {
                var updateMessages = new List<MessengerMessage>();

                if (args.Data.TryGetValue(MessengerServerSystem.MessengerMessage,
                        out MessengerMessage? message))
                {
                    updateMessages.Add(message);
                }

                if (args.Data.TryGetValue(MessengerServerSystem.MessengerMessageList,
                        out List<MessengerMessage>? messages))
                {
                    updateMessages.AddRange(messages);
                }

                if (updateMessages.Count > 0)
                    UpdateMessengerUiState(component.Loader, new MessengerMessagesUiState(updateMessages));

                break;
            }
            // receive new chat message
            case MessengerServerSystem.MessengerServerCommandNewMessage:
            {
                if (!args.Data.TryGetValue(MessengerServerSystem.MessengerMessage, out MessengerMessage? message))
                    break;
                if (!args.Data.TryGetValue(MessengerServerSystem.MessengerChatId, out uint? chatId))
                    break;

                RaiseLocalEvent(component.Loader, new RingerPlayRingtoneMessage());

                UpdateMessengerUiState(component.Loader, new MessengerNewChatMessageUiState(chatId.Value, message));
                break;
            }
        }
    }

    private void OnInstall(EntityUid uid, MessengerClientCartridgeComponent component, CartridgeAddedEvent args)
    {
        component.Loader = args.Loader;

        if (component.IsInstalled)
            return;

        component.IsInstalled = true;

        BroadcastCommand(uid, args.Loader, MessengerClientCommandCheckServer);
    }

    private void BroadcastCommand(EntityUid senderUid, EntityUid loaderUid, string command)
    {
        var deviceMapId = Transform(loaderUid).MapID;
        var activeServersFrequency = _messengerServerSystem.ActiveServersFrequency(deviceMapId);

        foreach (var frequency in activeServersFrequency)
        {
            _deviceNetworkSystem?.QueuePacket(senderUid, null, new NetworkPayload
            {
                [MessengerClientCommand] = command,
                [MessengerClientDevice] = loaderUid
            }, frequency: frequency);
        }
    }

    private void OnUiReady(EntityUid uid, MessengerClientCartridgeComponent component, CartridgeUiReadyEvent args)
    {
        if (component.ActiveServer != null)
            return;

        BroadcastCommand(uid, args.Loader, MessengerClientCommandCheckServer);
        UpdateMessengerUiState(args.Loader,
            new MessengerErrorUiState(Loc.GetString("messenger-error-server-not-found")));
    }

    private void OnUiMessage(EntityUid uid, MessengerClientCartridgeComponent? component, CartridgeMessageEvent args)
    {
        if (!Resolve(uid, ref component))
            return;

        var serverAddress = ServerAddress(ref component);

        switch (args)
        {
            // to sync state on UI and on server, client send current state,
            // and if server decide it will send missing state
            case MessengerUpdateStateUiEvent e:
            {
                // client could request full state sync
                if (e.IsFullState)
                {
                    component.SendState = true;
                    break;
                }

                var payload = new NetworkPayload
                {
                    [MessengerClientCommand] = MessengerClientCommandStateUpdate,
                    [MessengerClientDevice] = args.LoaderUid,
                };

                if (e.CurrentContacts is { Count: > 0 })
                    payload[MessengerClientContactsIds] = e.CurrentContacts;

                if (e.CurrentMessages is { Count: > 0 })
                    payload[MessengerClientMessagesIds] = e.CurrentMessages;

                if (e.CurrentChats is { Count: > 0 })
                    payload[MessengerClientCurrentChatIds] = e.CurrentChats;

                _deviceNetworkSystem?.QueuePacket(uid, serverAddress, payload);

                break;
            }
            case MessengerSendMessageUiEvent e:
            {
                if (e.MessageText == "")
                    break;

                _deviceNetworkSystem?.QueuePacket(uid, serverAddress, new NetworkPayload
                {
                    [MessengerClientCommand] = MessengerClientCommandMessageSend,
                    [MessengerClientDevice] = args.LoaderUid,
                    [MessengerClientChatId] = e.ChatId,
                    [MessengerClientMessage] = e.MessageText,
                });
                break;
            }
        }
    }

    private void UpdateMessengerUiState(EntityUid loaderUid, BoundUserInterfaceState messengerUiState)
    {
        _statesQueue.Enqueue(new QueueBoundUserInterfaceState(loaderUid, messengerUiState));
    }

    private static string? ServerAddress(ref MessengerClientCartridgeComponent component)
    {
        return component.ActiveServer == null ? null : component.Servers[component.ActiveServer.Value].Address;
    }
}

public sealed class QueueBoundUserInterfaceState
{
    public EntityUid LoaderUid;
    public readonly BoundUserInterfaceState? State;

    public QueueBoundUserInterfaceState(EntityUid loaderUid, BoundUserInterfaceState? state)
    {
        LoaderUid = loaderUid;
        State = state;
    }
}
