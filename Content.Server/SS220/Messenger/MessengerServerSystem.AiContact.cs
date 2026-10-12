// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.SS220.CartridgeLoader.Cartridges;
using Content.Shared.CartridgeLoader;
using Content.Shared.DeviceNetwork;
using Content.Shared.Popups;
using Content.Shared.Silicons.StationAi;
using Content.Shared.SS220.CartridgeLoader.Cartridges;
using Content.Shared.SS220.Messenger;

namespace Content.Server.SS220.Messenger;

/// <summary>
/// The station AI as a contact of the server. It has no id card and no pda, so it talks with the server directly:
/// the ui of the AI sends the same events as the cartridge, and the server answers with the same states.
/// </summary>
public sealed partial class MessengerServerSystem
{
    [Dependency] private SharedPopupSystem _popup = default!;

    private void InitializeAiContact()
    {
        SubscribeLocalEvent<StationAiHeldComponent, MessengerAiOpenEvent>(OnAiOpenMessenger);
        SubscribeLocalEvent<StationAiHeldComponent, CartridgeUiMessage>(OnAiClientMessage);

        SubscribeLocalEvent<StationAiHeldComponent, ComponentStartup>(OnAiHeldStartup);
        SubscribeLocalEvent<StationAiHeldComponent, ComponentRemove>(OnAiHeldRemove);
    }

    private void OnAiHeldStartup(Entity<StationAiHeldComponent> ent, ref ComponentStartup args)
    {
        RegisterAiOnServers(ent);
    }

    private void OnAiHeldRemove(Entity<StationAiHeldComponent> ent, ref ComponentRemove args)
    {
        UpdateAllContactStatuses(ent);
    }

    private void OnAiOpenMessenger(Entity<StationAiHeldComponent> ent, ref MessengerAiOpenEvent args)
    {
        if (args.Handled)
            return;

        if (!TryFindAiServer(ent, out var server))
        {
            _popup.PopupEntity(Loc.GetString("messenger-error-server-not-found"), ent, ent);
            return;
        }

        args.Handled = true;
        RegisterAi(server, ent);
        _ui.OpenUi(ent.Owner, MessengerAiClientUiKey.Key, ent.Owner);
    }

    private void OnAiClientMessage(Entity<StationAiHeldComponent> ent, ref CartridgeUiMessage args)
    {
        if (args.Actor != ent.Owner
            || !TryFindAiServer(ent, out var server)
            || !TryGetContactId(server, ent.Owner, out var contactId))
            return;

        switch (args.MessageEvent)
        {
            case MessengerUpdateStateUiEvent e:
                OnAiStateRequest(server, ent, contactId, e);
                break;
            case MessengerSendMessageUiEvent e:
                if (!string.IsNullOrEmpty(e.MessageText))
                    SendMessage(server, contactId, e.ChatId, SanitizeMessage(e.MessageText), e.ReplyToId, $"{ToPrettyString(ent)}");

                break;
            case MessengerMarkReadUiEvent e:
                MarkRead(server.Comp, contactId, e.ChatId);
                break;
            case MessengerSetNameLockUiEvent e:
                SetNameLock(server, contactId, e.Locked);
                break;
        }
    }

    /// <summary>
    /// The same as a cartridge does: the full state, or only what the ui does not have yet.
    /// </summary>
    private void OnAiStateRequest(Entity<MessengerServerComponent> server,
        EntityUid ai,
        ContactUid contactId,
        MessengerUpdateStateUiEvent e)
    {
        if (e.IsFullState)
        {
            if (TryBuildContactUiState(server, contactId, out var state))
                SendAiState(ai, state);

            return;
        }

        UpdateContactNames(server);

        var chats = GetAccessedChats(server.Comp, contactId);
        var contacts = CollectMissingContacts(server.Comp, chats, CopyIds(e.CurrentContacts));
        var messages = CollectMissingMessages(server.Comp, chats, CopyIds(e.CurrentMessages));
        var missingChats = CollectMissingChats(chats, CopyIds(e.CurrentChats));

        if (contacts.Count > 0)
            SendAiState(ai, new MessengerContactUiState(contacts));

        if (messages.Count > 0)
            SendAiState(ai, new MessengerMessagesUiState(messages));

        if (missingChats.Count > 0)
            SendAiState(ai, new MessengerChatUpdateUiState(missingChats));
    }

    private static HashSet<T> CopyIds<T>(HashSet<T>? ids)
    {
        return ids == null ? new HashSet<T>() : new HashSet<T>(ids);
    }

    /// <summary>
    /// Turns a packet meant for a pda into the ui state of the AI window, if the window is open.
    /// Updates missed while it is closed are restored by the full state on the next opening.
    /// </summary>
    private void SendToAi(EntityUid ai, NetworkPayload payload)
    {
        if (!_ui.IsUiOpen(ai, MessengerAiClientUiKey.Key, ai)
            || !payload.TryGetValue(MessengerNetworkKey.Command.ToString(), out MessengerNetworkCommand? command))
            return;

        switch (command)
        {
            case MessengerNetworkCommand.Contact:
                if (MessengerClientCartridgeSystem.TryGetItems(payload,
                        MessengerNetworkKey.Contact,
                        MessengerNetworkKey.ContactList,
                        out List<MessengerContact> contacts))
                    SendAiState(ai, new MessengerContactUiState(contacts));

                break;
            case MessengerNetworkCommand.Chat:
                if (MessengerClientCartridgeSystem.TryGetItems(payload,
                        MessengerNetworkKey.Chat,
                        MessengerNetworkKey.ChatList,
                        out List<MessengerChat> chats))
                    SendAiState(ai, new MessengerChatUpdateUiState(chats));

                break;
            case MessengerNetworkCommand.NewMessage:
                if (payload.TryGetValue(MessengerNetworkKey.Message.ToString(), out MessengerMessage? message)
                    && payload.TryGetValue(MessengerNetworkKey.ChatId.ToString(), out ChatUid? chatId))
                    SendAiState(ai, new MessengerNewChatMessageUiState(chatId.Value, message));

                break;
        }
    }

    private void SendAiState(EntityUid ai, BoundUserInterfaceState state)
    {
        _ui.ServerSendUiMessage(ai, MessengerAiClientUiKey.Key, new MessengerAiStateMessage(state), ai);
    }

    /// <summary>
    /// The AI uses the first powered server on its map that allows the AI contact.
    /// </summary>
    private bool TryFindAiServer(EntityUid ai, out Entity<MessengerServerComponent> server)
    {
        var map = Transform(ai).MapID;
        var query = EntityQueryEnumerator<MessengerServerComponent, TransformComponent>();

        while (query.MoveNext(out var serverUid, out var serverComp, out var xform))
        {
            if (!serverComp.AiContactEnabled || xform.MapID != map || !IsPowered(serverUid))
                continue;

            server = new Entity<MessengerServerComponent>(serverUid, serverComp);
            return true;
        }

        server = default;
        return false;
    }

    /// <summary>
    /// Gives the AI a contact on the server, so the crew sees it in the chat list.
    /// </summary>
    private void RegisterAi(Entity<MessengerServerComponent> server, EntityUid ai)
    {
        if (TryGetContactId(server, ai, out _))
            return;

        var contactId = RegisterNewClient(server, ai, MetaData(ai).EntityName, null);
        server.Comp.AiContacts[contactId] = ai;

        UpdateContactStatuses(server);
    }

    private void RegisterAiOnServers(EntityUid ai)
    {
        if (TryFindAiServer(ai, out var server))
            RegisterAi(server, ai);
    }

    /// <summary>
    /// Registers the AIs that were already in their cores when the server started to work.
    /// </summary>
    private void RegisterAllAi(Entity<MessengerServerComponent> server)
    {
        if (!server.Comp.AiContactEnabled)
            return;

        var query = EntityQueryEnumerator<StationAiHeldComponent>();

        while (query.MoveNext(out var aiUid, out _))
        {
            if (Transform(aiUid).MapID == Transform(server).MapID)
                RegisterAi(server, aiUid);
        }
    }
}
