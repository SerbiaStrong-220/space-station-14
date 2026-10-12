// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using Content.Server.Administration.Logs;
using Content.Server.CartridgeLoader;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.SS220.Messenger;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.Database;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.SS220.CartridgeLoader.Cartridges;
using Content.Shared.SS220.Messenger;
using Content.Shared.PDA.Ringer;

namespace Content.Server.SS220.CartridgeLoader.Cartridges;

public sealed partial class MessengerClientCartridgeSystem : EntitySystem
{
    [Dependency] private CartridgeLoaderSystem _cartridgeLoader = default!;
    [Dependency] private DeviceNetworkSystem _deviceNetwork = default!;
    [Dependency] private MessengerServerSystem _messengerServer = default!;
    [Dependency] private IAdminLogManager _adminLogger = default!;

    [Dependency] private EntityQuery<MessengerServerComponent> _serverQuery = default!;

    private readonly LocId _serverNotFound = "messenger-error-server-not-found";

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
        var query = EntityQueryEnumerator<MessengerClientCartridgeComponent>();

        while (query.MoveNext(out var uid, out var cartridge))
        {
            if (cartridge.SendState && TryQueueFullState((uid, cartridge)))
                cartridge.SendState = false;

            if (!cartridge.Loader.IsValid())
                continue;

            // states set to one loader within a tick overwrite each other, so send one per update
            if (cartridge.PendingStates.TryDequeue(out var state))
                _cartridgeLoader.UpdateCartridgeUiState(cartridge.Loader, state);
        }
    }

    private bool TryQueueFullState(Entity<MessengerClientCartridgeComponent> ent)
    {
        if (ent.Comp.ActiveServer is not { Valid: true } serverUid)
            return false;

        if (!_serverQuery.TryComp(serverUid, out var server))
        {
            Log.Error($"Got messenger cartridge {ToPrettyString(ent)} with entity {ToPrettyString(serverUid)} without {nameof(MessengerServerComponent)}");
            return false;
        }

        if (!_messengerServer.TryRestoreContactUiState((serverUid, server), ent.Comp.Loader, out var state))
            return false;

        ent.Comp.PendingStates.Enqueue(state);

        return true;
    }

    private void OnInstall(Entity<MessengerClientCartridgeComponent> ent, ref CartridgeAddedEvent args)
    {
        ent.Comp.Loader = args.Loader;
        _messengerServer.OnCartridgeInstalled(ent, args.Loader);
    }

    private void OnUiReady(Entity<MessengerClientCartridgeComponent> ent, ref CartridgeUiReadyEvent args)
    {
        if (ent.Comp.ActiveServer != null)
            return;

        BroadcastCommand(ent, args.Loader, MessengerNetworkCommand.CheckServer);
        ent.Comp.PendingStates.Enqueue(new MessengerErrorUiState(_serverNotFound));
    }

    private void OnUiMessage(Entity<MessengerClientCartridgeComponent> ent, ref CartridgeMessageEvent args)
    {
        switch (args)
        {
            case MessengerUpdateStateUiEvent e:
                OnUpdateStateRequest(ent, e);
                break;
            case MessengerSendMessageUiEvent e:
                OnSendMessage(ent, e);
                break;
            case MessengerMarkReadUiEvent e:
                OnMarkRead(ent, e);
                break;
        }
    }

    private void OnMarkRead(Entity<MessengerClientCartridgeComponent> ent, MessengerMarkReadUiEvent e)
    {
        if (!TryGetServerAddress(ent.Comp, out var serverAddress))
            return;

        _deviceNetwork.QueuePacket(ent.Owner, serverAddress, new NetworkPayload
        {
            [MessengerNetworkKey.Command.ToString()] = MessengerNetworkCommand.MarkRead,
            [MessengerNetworkKey.DeviceUid.ToString()] = e.LoaderUid,
            [MessengerNetworkKey.ChatId.ToString()] = e.ChatId,
        });
    }

    /// <summary>
    /// The ui sends the ids it already has, the server answers with the missing ones.
    /// </summary>
    private void OnUpdateStateRequest(Entity<MessengerClientCartridgeComponent> ent, MessengerUpdateStateUiEvent e)
    {
        if (e.IsFullState)
        {
            ent.Comp.SendState = true;

            return;
        }

        if (!TryGetServerAddress(ent.Comp, out var serverAddress))
            return;

        var payload = new NetworkPayload
        {
            [MessengerNetworkKey.Command.ToString()] = MessengerNetworkCommand.StateUpdate,
            [MessengerNetworkKey.DeviceUid.ToString()] = e.LoaderUid,
        };

        if (e.CurrentContacts is { Count: > 0 })
            payload[MessengerNetworkKey.ContactsIds.ToString()] = e.CurrentContacts;

        if (e.CurrentMessages is { Count: > 0 })
            payload[MessengerNetworkKey.MessagesIds.ToString()] = e.CurrentMessages;

        if (e.CurrentChats is { Count: > 0 })
            payload[MessengerNetworkKey.CurrentChatIds.ToString()] = e.CurrentChats;

        _deviceNetwork.QueuePacket(ent.Owner, serverAddress, payload);
    }

    private void OnSendMessage(Entity<MessengerClientCartridgeComponent> ent, MessengerSendMessageUiEvent e)
    {
        if (string.IsNullOrEmpty(e.MessageText) || !TryGetServerAddress(ent.Comp, out var serverAddress))
            return;

        var message = _messengerServer.SanitizeMessage(e.MessageText);

        var payload = new NetworkPayload
        {
            [MessengerNetworkKey.Command.ToString()] = MessengerNetworkCommand.MessageSend,
            [MessengerNetworkKey.DeviceUid.ToString()] = e.LoaderUid,
            [MessengerNetworkKey.ChatId.ToString()] = e.ChatId,
            [MessengerNetworkKey.MessageText.ToString()] = message,
        };

        if (e.ReplyToId != null)
            payload[MessengerNetworkKey.ReplyToId.ToString()] = e.ReplyToId.Value;

        _deviceNetwork.QueuePacket(ent.Owner, serverAddress, payload);

        _adminLogger.Add(LogType.MessengerClientCartridge,
            LogImpact.Low,
            $"Send: sender entity: {ToPrettyString(ent.Owner):entity}, device entity: {ToPrettyString(e.LoaderUid):entity}, chatID: {e.ChatId}, msg: {e.MessageText}, filtered: {message}");
    }

    private void OnNetworkPacket(Entity<MessengerClientCartridgeComponent> ent, ref DeviceNetworkPacketEvent args)
    {
        if (!args.Data.TryGetValue(MessengerNetworkKey.Command.ToString(),
                out MessengerNetworkCommand? command))
            return;

        switch (command)
        {
            case MessengerNetworkCommand.Info:
                OnServerInfo(ent, args.Sender, args.SenderAddress);
                break;
            case MessengerNetworkCommand.ClientContact:
                OnClientContact(ent, args.Data);
                break;
            case MessengerNetworkCommand.Contact:
                OnContacts(ent, args.Data);
                break;
            case MessengerNetworkCommand.Chat:
                OnChats(ent, args.Data);
                break;
            case MessengerNetworkCommand.Messages:
                OnMessages(ent, args.Data);
                break;
            case MessengerNetworkCommand.NewMessage:
                OnNewMessage(ent, args.Data);
                break;
        }
    }

    /// <summary>
    /// The server announced itself: remember its address and request the full state.
    /// </summary>
    private void OnServerInfo(Entity<MessengerClientCartridgeComponent> ent, EntityUid server, string address)
    {
        ent.Comp.ActiveServer ??= server;
        ent.Comp.ServerAddresses[server] = address;
        ent.Comp.SendState = true;
    }

    private void OnClientContact(Entity<MessengerClientCartridgeComponent> ent, NetworkPayload data)
    {
        if (data.TryGetValue(MessengerNetworkKey.Contact.ToString(), out MessengerContact? contact))
            ent.Comp.PendingStates.Enqueue(new MessengerClientContactUiState(contact));
    }

    private void OnContacts(Entity<MessengerClientCartridgeComponent> ent, NetworkPayload data)
    {
        if (TryGetItems(data,
                MessengerNetworkKey.Contact,
                MessengerNetworkKey.ContactList,
                out List<MessengerContact> contacts))
        {
            ent.Comp.PendingStates.Enqueue(new MessengerContactUiState(contacts));
        }
    }

    private void OnChats(Entity<MessengerClientCartridgeComponent> ent, NetworkPayload data)
    {
        if (TryGetItems(data,
                MessengerNetworkKey.Chat,
                MessengerNetworkKey.ChatList,
                out List<MessengerChat> chats))
        {
            ent.Comp.PendingStates.Enqueue(new MessengerChatUpdateUiState(chats));
        }
    }

    private void OnMessages(Entity<MessengerClientCartridgeComponent> ent, NetworkPayload data)
    {
        if (TryGetItems(data,
                MessengerNetworkKey.Message,
                MessengerNetworkKey.MessageList,
                out List<MessengerMessage> messages))
        {
            ent.Comp.PendingStates.Enqueue(new MessengerMessagesUiState(messages));
        }
    }

    private void OnNewMessage(Entity<MessengerClientCartridgeComponent> ent, NetworkPayload data)
    {
        if (!data.TryGetValue(MessengerNetworkKey.Message.ToString(), out MessengerMessage? message)
            || !data.TryGetValue(MessengerNetworkKey.ChatId.ToString(), out ChatUid? chatId))
            return;

        if (!data.ContainsKey(MessengerNetworkKey.Silent.ToString()))
            RaiseLocalEvent(ent.Comp.Loader, new RingerPlayRingtoneMessage());

        ent.Comp.PendingStates.Enqueue(new MessengerNewChatMessageUiState(chatId.Value, message));
    }

    private void BroadcastCommand(EntityUid senderUid, EntityUid loaderUid, MessengerNetworkCommand command)
    {
        var mapId = Transform(loaderUid).MapID;

        foreach (var frequency in _messengerServer.ActiveServersFrequency(mapId))
        {
            _deviceNetwork.QueuePacket(senderUid,
                null,
                new NetworkPayload
                {
                    [MessengerNetworkKey.Command.ToString()] = command,
                    [MessengerNetworkKey.DeviceUid.ToString()] = loaderUid,
                },
                frequency: frequency);
        }
    }

    /// <summary>
    /// Packets carry either a single item or a list of them. Collects both into a new list.
    /// </summary>
    /// <returns>True if there is at least one item.</returns>
    public static bool TryGetItems<T>(NetworkPayload data,
        MessengerNetworkKey itemKey,
        MessengerNetworkKey listKey,
        out List<T> items)
        where T : class
    {
        items = new List<T>();

        if (data.TryGetValue(itemKey.ToString(), out T? item))
            items.Add(item);

        if (data.TryGetValue(listKey.ToString(), out List<T>? list))
            items.AddRange(list);

        return items.Count > 0;
    }

    private static bool TryGetServerAddress(MessengerClientCartridgeComponent cartridge,
        [NotNullWhen(true)] out string? address)
    {
        address = null;

        return cartridge.ActiveServer is { } server && cartridge.ServerAddresses.TryGetValue(server, out address);
    }
}
