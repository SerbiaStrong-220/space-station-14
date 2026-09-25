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
using Robust.Shared.Timing;

namespace Content.Server.SS220.CartridgeLoader.Cartridges;

public sealed class MessengerClientCartridgeSystem : EntitySystem
{
    [Dependency] private readonly CartridgeLoaderSystem _cartridgeLoaderSystem = default!;
    [Dependency] private readonly DeviceNetworkSystem _deviceNetworkSystem = default!;
    [Dependency] private readonly MessengerServerSystem _messengerServerSystem = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

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

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<MessengerClientCartridgeComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (TerminatingOrDeleted(comp.ActiveServer))
            {
                comp.ActiveServer = null;
            }

            if (comp.ActiveServer == null &&
                comp.Loader != default &&
                now >= comp.NextServerScanTime)
            {
                comp.NextServerScanTime = now + comp.ServerScanInterval;
                BroadcastCheckServer(uid, comp.Loader);
            }

            if (!comp.SendState)
                continue;

            if (comp.ActiveServer == null)
                continue;

            if (!TryComp<MessengerServerComponent>(comp.ActiveServer.Value, out var server))
                continue;

            if (!_messengerServerSystem.RestoreContactUIStateIdCard(
                    comp.Loader,
                    ref server,
                    out var messengerUiState))
            {
                continue;
            }

            EnqueueUiState(comp.Loader, messengerUiState);
            comp.SendState = false;
        }
    }

    private void OnNetworkPacket(Entity<MessengerClientCartridgeComponent> ent, ref DeviceNetworkPacketEvent args)
    {
        if (!args.Data.TryGetValue(MessengerNetKeys.Command, out IMessengerCommand? cmd))
            return;

        switch (cmd)
        {
            case ServerInfoCommand info:
                HandleServerInfoPacket(ent, info, args);
                break;

            case UpdateContactsCommand contacts:
                HandleContactPacket(ent, contacts);
                break;

            case UpdateChatsCommand chats:
                HandleChatPacket(ent, chats);
                break;

            case UpdateMessagesCommand messages:
                HandleMessagePacket(ent, messages);
                break;

            case NewMessageCommand newMsg:
                HandleNewMessagePacket(ent, newMsg);
                break;

            case ChatMessageClearedCommand cleared:
                HandleDeleteMsgInOneChat(ent, cleared);
                break;

            case ErrorCommand error:
                EnqueueUiState(ent.Comp.Loader, new MessengerErrorUiState(error.Text));
                break;
        }
    }

    private void HandleServerInfoPacket(
        Entity<MessengerClientCartridgeComponent> ent,
        ServerInfoCommand cmd,
        DeviceNetworkPacketEvent args)
    {
        var serverInfo = new ServerInfo
        {
            Name = cmd.ServerName,
            Address = args.SenderAddress,
        };

        ent.Comp.ActiveServer ??= args.Sender;

        if (!ent.Comp.Servers.TryAdd(args.Sender, serverInfo))
        {
            ent.Comp.Servers.Remove(args.Sender);
            ent.Comp.Servers.Add(args.Sender, serverInfo);
        }

        ent.Comp.SendState = true;
    }

    private void HandleContactPacket(Entity<MessengerClientCartridgeComponent> ent, UpdateContactsCommand cmd)
    {
        if (cmd.Contacts.Count == 0)
            return;

        EnqueueUiState(ent.Comp.Loader, new MessengerContactUiState(cmd.Contacts));
    }

    private void HandleChatPacket(Entity<MessengerClientCartridgeComponent> ent, UpdateChatsCommand cmd)
    {
        if (cmd.Chats.Count == 0)
            return;

        EnqueueUiState(ent.Comp.Loader, new MessengerChatUpdateUiState(cmd.Chats));
    }

    private void HandleMessagePacket(Entity<MessengerClientCartridgeComponent> ent, UpdateMessagesCommand cmd)
    {
        if (cmd.Messages.Count == 0)
            return;

        EnqueueUiState(ent.Comp.Loader, new MessengerMessagesUiState(cmd.Messages));
    }

    private void HandleNewMessagePacket(Entity<MessengerClientCartridgeComponent> ent, NewMessageCommand cmd)
    {
        RaiseLocalEvent(ent.Comp.Loader, new RingerPlayRingtoneMessage());
        EnqueueUiState(ent.Comp.Loader, new MessengerNewChatMessageUiState(cmd.ChatId, cmd.Message));
    }

    private void HandleDeleteMsgInOneChat(Entity<MessengerClientCartridgeComponent> ent, ChatMessageClearedCommand cmd)
    {
        EnqueueUiState(ent.Comp.Loader, new MessengerDeleteMsgInChatUiState(cmd.ChatId));
    }

    private void OnInstall(Entity<MessengerClientCartridgeComponent> ent, ref CartridgeAddedEvent args)
    {
        ent.Comp.Loader = args.Loader;

        if (ent.Comp.IsInstalled)
            return;

        ent.Comp.IsInstalled = true;

        BroadcastCheckServer(ent, args.Loader);
    }

    private void BroadcastCheckServer(EntityUid senderUid, EntityUid loaderUid)
    {
        var deviceMapId = Transform(loaderUid).MapID;
        var activeServersFrequency = _messengerServerSystem.ActiveServersFrequency(deviceMapId);

        foreach (var frequency in activeServersFrequency)
        {
            var payload = new NetworkPayload
            {
                [MessengerNetKeys.Command] = new CheckServerCommand{ DeviceUid = loaderUid },
            };

            _deviceNetworkSystem.QueuePacket(senderUid, null, payload, frequency: frequency);
        }
    }

    private void OnUiReady(Entity<MessengerClientCartridgeComponent> ent, ref CartridgeUiReadyEvent args)
    {
        if (ent.Comp.ActiveServer != null)
            return;

        BroadcastCheckServer(ent, args.Loader);

        EnqueueUiState(
            args.Loader,
            new MessengerErrorUiState(Loc.GetString("messenger-error-server-not-found")));
    }

    private void OnUiMessage(Entity<MessengerClientCartridgeComponent> ent, ref CartridgeMessageEvent args)
    {
        var serverAddress = GetServerAddress(ref ent.Comp);
        if (serverAddress == null)
            return;

        switch (args)
        {
            // to sync state on UI and on server, client send current state,
            // and if server decide it will send missing state
            case MessengerUpdateStateUiEvent e:
            {
                if (e.IsFullState)
                {
                    ent.Comp.SendState = true;
                    break;
                }

                var cmd = new RequestStateCommand
                {
                    FullState = false,
                    KnownContacts = e.CurrentContacts is { Count: > 0 } ? e.CurrentContacts : null,
                    KnownMessages = e.CurrentMessages is { Count: > 0 } ? e.CurrentMessages : null,
                    KnownChats = e.CurrentChats is { Count: > 0 } ? e.CurrentChats : null,
                    DeviceUid = GetEntity(args.LoaderUid),
                };

                var payload = new NetworkPayload
                {
                    [MessengerNetKeys.Command] = cmd,
                };

                _deviceNetworkSystem.QueuePacket(ent, serverAddress, payload);
                break;
            }

            case MessengerSendMessageUiEvent e:
            {
                if (string.IsNullOrEmpty(e.MessageText))
                    break;

                var filtered = _chat.ReplaceWords(e.MessageText);

                var cmd = new SendMessageCommand
                {
                    ChatId = e.ChatId,
                    MessageText = filtered,
                    DeviceUid = GetEntity(args.LoaderUid),
                };

                var payload = new NetworkPayload
                {
                    [MessengerNetKeys.Command] = cmd,
                };

                _adminLogger.Add(
                    LogType.MessengerClientCartridge,
                    LogImpact.Low,
                    $"Send: sender entity: {ent}, device entity: {args.LoaderUid}, chatID: {e.ChatId}, msg: {e.MessageText}, filtered: {filtered}");

                _deviceNetworkSystem.QueuePacket(ent, serverAddress, payload);
                break;
            }

            case MessengerClearChatUiMessageEvent e:
            {
                var cmd = new DeleteChatMessagesCommand
                {
                    ChatId = e.ChatId,
                    DeviceUid = GetEntity(args.LoaderUid),
                };

                var payload = new NetworkPayload
                {
                    [MessengerNetKeys.Command] = cmd,
                };

                _deviceNetworkSystem.QueuePacket(ent, serverAddress, payload);
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
