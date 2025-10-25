// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Administration.Logs;
using Content.Server.CartridgeLoader;
using Content.Server.Chat.Systems;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.SS220.Messenger;
using Content.Shared.CartridgeLoader;
using Content.Shared.Database;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.PDA.Ringer;
using Content.Shared.SS220.CartridgeLoader.Cartridges;
using Content.Shared.SS220.Messenger;

namespace Content.Server.SS220.CartridgeLoader.Cartridges;

public sealed class MessengerClientCartridgeSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly CartridgeLoaderSystem _cartridgeLoaderSystem = default!;
    [Dependency] private readonly DeviceNetworkSystem _deviceNetworkSystem = default!;
    [Dependency] private readonly MessengerServerSystem _messengerServerSystem = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    public enum NetworkCommand
    {
        CheckServer,
        StateUpdate,
        MessageSend,
        DeleteMessageInOneChat,
        DeleteMessageInAllChat,
    }

    public enum NetworkKey
    {
        Command,
        DeviceUid,
        ChatId,
        MessageText,
        CurrentChatIds,
        ContactsIds,
        MessagesIds,
    }

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
                _cartridgeLoaderSystem.UpdateCartridgeUiState(state.LoaderUid, state.State);
        }

        // is component request full state, try to get it from server
        foreach (var clientCartridgeComponent in _entityManager.EntityQuery<MessengerClientCartridgeComponent>())
        {
            if (!clientCartridgeComponent.SendState)
                continue;

            if (clientCartridgeComponent.ActiveServer == null)
                continue;

            if (!TryComp<MessengerServerComponent>(clientCartridgeComponent.ActiveServer.Value, out var server))
                continue;

            if (!_messengerServerSystem.RestoreContactUIStateIdCard(
                    clientCartridgeComponent.Loader,
                    ref server,
                    out var messengerUiState))
            {
                continue;
            }

            EnqueueUiState(clientCartridgeComponent.Loader, messengerUiState);
            clientCartridgeComponent.SendState = false;
        }
    }

    private void OnNetworkPacket(
        EntityUid uid,
        MessengerClientCartridgeComponent? component,
        DeviceNetworkPacketEvent args)
    {
        if (!args.Data.TryGetValue(nameof(MessengerServerSystem.NetworkKey.Command),
                out MessengerServerSystem.NetworkCommand? command))
            return;

        if (!Resolve(uid, ref component))
            return;

        switch (command)
        {
            // when receive msg about server info, add this server to a server list, and if received, add server name
            case MessengerServerSystem.NetworkCommand.Info:
            {
                HandleServerInfoPacket(component, args);
                break;
            }
            // receive client contact info
            case MessengerServerSystem.NetworkCommand.ClientContact:
            {
                HandleClientContactPacket(component, args);
                break;
            }
            // receive contact info
            case MessengerServerSystem.NetworkCommand.Contact:
            {
                HandleContactPacket(component.Loader, args.Data);
                break;
            }
            // receive chat info
            case MessengerServerSystem.NetworkCommand.Chat:
            {
                HandleChatPacket(component, args);
                break;
            }
            // receive message info
            case MessengerServerSystem.NetworkCommand.Messages:
            {
                HandleMessagePacket(component, args);
                break;
            }
            // receive a new chat message
            case MessengerServerSystem.NetworkCommand.NewMessage:
            {
                HandleNewMessagePacket(component, args);
                break;
            }
            // delete all messages in one chat
            case MessengerServerSystem.NetworkCommand.DeleteAllMessageInOneChat:
            {
                HandleDeleteMsgInOneChat(component, args);
                break;
            }
            // delete all messages in all chats
            case MessengerServerSystem.NetworkCommand.DeleteAllMessageInAllChat:
            {
                HandleDeleteMsgInAllChat(component, args);
                break;
            }
        }
    }

    private void HandleServerInfoPacket(MessengerClientCartridgeComponent component, DeviceNetworkPacketEvent args)
    {
        var serverInfo = new ServerInfo();

        if (args.Data.TryGetValue(
                nameof(MessengerServerSystem.NetworkKey.ServerName),
                out string? serverName))
        {
            serverInfo.Name = serverName;
        }

        serverInfo.Address = args.SenderAddress;

        component.ActiveServer ??= args.Sender;

        if (!component.Servers.TryAdd(args.Sender, serverInfo))
        {
            component.Servers.Remove(args.Sender);
            component.Servers.Add(args.Sender, serverInfo);
        }

        component.SendState = true;
    }

    private void HandleClientContactPacket(MessengerClientCartridgeComponent component, DeviceNetworkPacketEvent args)
    {
        if (!args.Data.TryGetValue(nameof(MessengerServerSystem.NetworkKey.Contact),
                out MessengerContact? contact))
            return;

        EnqueueUiState(component.Loader, new MessengerClientContactUiState(contact));
    }

    private void HandleContactPacket(EntityUid loader, NetworkPayload data)
    {
        var contactsUpdate = new List<MessengerContact>();

        if (data.TryGetValue(nameof(MessengerServerSystem.NetworkKey.Contact), out MessengerContact? contact))
            contactsUpdate.Add(contact);

        if (data.TryGetValue(nameof(MessengerServerSystem.NetworkKey.ContactList), out List<MessengerContact>? contacts))
            contactsUpdate.AddRange(contacts);

        if (contactsUpdate.Count > 0)
            EnqueueUiState(loader, new MessengerContactUiState(contactsUpdate));
    }

    private void HandleChatPacket(MessengerClientCartridgeComponent component, DeviceNetworkPacketEvent args)
    {
        var updateChats = new List<MessengerChat>();

        if (args.Data.TryGetValue(nameof(MessengerServerSystem.NetworkKey.Chat), out MessengerChat? chat))
        {
            updateChats.Add(chat);
        }

        if (args.Data.TryGetValue(nameof(MessengerServerSystem.NetworkKey.ChatList),
                out List<MessengerChat>? chats))
        {
            updateChats.AddRange(chats);
        }

        if (updateChats.Count > 0)
            EnqueueUiState(component.Loader, new MessengerChatUpdateUiState(updateChats));
    }

    private void HandleMessagePacket(MessengerClientCartridgeComponent component, DeviceNetworkPacketEvent args)
    {
        var updateMessages = new List<MessengerMessage>();

        if (args.Data.TryGetValue(nameof(MessengerServerSystem.NetworkKey.Message),
                out MessengerMessage? message))
        {
            updateMessages.Add(message);
        }

        if (args.Data.TryGetValue(nameof(MessengerServerSystem.NetworkKey.MessageList),
                out List<MessengerMessage>? messages))
        {
            updateMessages.AddRange(messages);
        }

        if (updateMessages.Count > 0)
            EnqueueUiState(component.Loader, new MessengerMessagesUiState(updateMessages));

    }

    private void HandleNewMessagePacket(MessengerClientCartridgeComponent component, DeviceNetworkPacketEvent args)
    {
        if (!args.Data.TryGetValue(nameof(MessengerServerSystem.NetworkKey.Message),
                out MessengerMessage? message))
            return;

        if (!args.Data.TryGetValue(nameof(MessengerServerSystem.NetworkKey.ChatId), out uint? chatId))
            return;

        RaiseLocalEvent(component.Loader, new RingerPlayRingtoneMessage());
        EnqueueUiState(component.Loader, new MessengerNewChatMessageUiState(chatId.Value, message));
    }

    private void HandleDeleteMsgInOneChat(MessengerClientCartridgeComponent component, DeviceNetworkPacketEvent args)
    {
        if (!args.Data.TryGetValue(nameof(MessengerServerSystem.NetworkKey.ChatId), out uint? chatId))
            return;

        EnqueueUiState(component.Loader, new MessengerDeleteMsgInChatUiState(chatId.Value));
    }

    private void HandleDeleteMsgInAllChat(MessengerClientCartridgeComponent component, DeviceNetworkPacketEvent args)
    {
        EnqueueUiState(component.Loader, new MessengerDeleteMsgInChatUiState(null, true));
    }

    private void OnInstall(EntityUid uid, MessengerClientCartridgeComponent component, CartridgeAddedEvent args)
    {
        component.Loader = args.Loader;

        if (component.IsInstalled)
            return;

        component.IsInstalled = true;

        BroadcastCommand(uid, args.Loader, NetworkCommand.CheckServer);
    }

    private void BroadcastCommand(EntityUid senderUid, EntityUid loaderUid, NetworkCommand command)
    {
        var deviceMapId = Transform(loaderUid).MapID;
        var activeServersFrequency = _messengerServerSystem.ActiveServersFrequency(deviceMapId);

        foreach (var frequency in activeServersFrequency)
        {
            var payload = new NetworkPayload
            {
                [nameof(NetworkKey.Command)] = command,
                [nameof(NetworkKey.DeviceUid)] = loaderUid,
            };

            _deviceNetworkSystem.QueuePacket(senderUid, null, payload, frequency: frequency);
        }
    }

    private void OnUiReady(EntityUid uid, MessengerClientCartridgeComponent component, CartridgeUiReadyEvent args)
    {
        if (component.ActiveServer != null)
            return;

        BroadcastCommand(uid, args.Loader, NetworkCommand.CheckServer);

        EnqueueUiState(
            args.Loader,
            new MessengerErrorUiState(Loc.GetString("messenger-error-server-not-found")));
    }

    private void OnUiMessage(EntityUid uid, MessengerClientCartridgeComponent? component, CartridgeMessageEvent args)
    {
        if (!Resolve(uid, ref component))
            return;

        var serverAddress = GetServerAddress(ref component);

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
                    [nameof(NetworkKey.Command)] = NetworkCommand.StateUpdate,
                    [nameof(NetworkKey.DeviceUid)] = args.LoaderUid,
                };

                if (e.CurrentContacts is { Count: > 0 })
                    payload[nameof(NetworkKey.ContactsIds)] = e.CurrentContacts;

                if (e.CurrentMessages is { Count: > 0 })
                    payload[nameof(NetworkKey.MessagesIds)] = e.CurrentMessages;

                if (e.CurrentChats is { Count: > 0 })
                    payload[nameof(NetworkKey.CurrentChatIds)] = e.CurrentChats;

                _deviceNetworkSystem.QueuePacket(uid, serverAddress, payload);
                break;
            }
            case MessengerSendMessageUiEvent e:
            {
                if (string.IsNullOrEmpty(e.MessageText))
                    break;

                var message = _chat.ReplaceWords(e.MessageText);

                var payload = new NetworkPayload
                {
                    [nameof(NetworkKey.Command)] = NetworkCommand.MessageSend,
                    [nameof(NetworkKey.DeviceUid)] = args.LoaderUid,
                    [nameof(NetworkKey.ChatId)] = e.ChatId,
                    [nameof(NetworkKey.MessageText)] = message,
                };

                _adminLogger.Add(
                    LogType.MessengerClientCartridge,
                    LogImpact.Low,
                    $"Send: sender entity: {uid}, device entity: {args.LoaderUid}, chatID: {e.ChatId}, msg: {e.MessageText}, filtered: {message}");

                _deviceNetworkSystem.QueuePacket(uid, serverAddress, payload);
                break;
            }
            case MessengerClearChatUiMessageEvent e:
            {
                var command = e.DeleteAll
                    ? NetworkCommand.DeleteMessageInAllChat
                    : NetworkCommand.DeleteMessageInOneChat;

                var payload = new NetworkPayload
                {
                    [nameof(NetworkKey.Command)] = command,
                    [nameof(NetworkKey.DeviceUid)] = args.LoaderUid,
                    [nameof(NetworkKey.ChatId)] = e.ChatId,
                };

                _deviceNetworkSystem.QueuePacket(uid, serverAddress, payload);

                break;
            }
        }
    }

    private void EnqueueUiState(EntityUid loaderUid, BoundUserInterfaceState messengerUiState)
    {
        _statesQueue.Enqueue(new QueueBoundUserInterfaceState(loaderUid, messengerUiState));
    }

    private static string? GetServerAddress(ref MessengerClientCartridgeComponent component)
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
