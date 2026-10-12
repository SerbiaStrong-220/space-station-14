// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.CartridgeLoader.Cartridges;
using Content.Server.Chat.Systems;
using Content.Shared.CartridgeLoader;
using Content.Shared.CartridgeLoader.Cartridges;
using Content.Shared.DeviceNetwork;
using Content.Shared.Messenger;
using Content.Shared.Popups;
using Content.Shared.Silicons.StationAi;

namespace Content.Server.Messenger;

/// <summary>
/// The station AI as a contact of the server. It has no id card and no pda, so it talks with the server directly:
/// the ui of the AI sends the same events as the cartridge, and the server answers with the same states.
/// </summary>
public sealed partial class MessengerServerSystem
{
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    private const int MaxMessageLength = 500;

    private void InitializeAiContact()
    {
        SubscribeLocalEvent<StationAiHeldComponent, MessengerAiOpenEvent>(OnAiOpenMessenger);
        SubscribeLocalEvent<StationAiHeldComponent, CartridgeUiMessage>(OnAiClientMessage);
    }

    /// <summary>
    /// Cuts the text of a message to the allowed length and applies the word filter.
    /// </summary>
    public string SanitizeMessage(string text)
    {
        return _chat.ReplaceWords(text.Length > MaxMessageLength ? text[..MaxMessageLength] : text);
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
            || !server.Comp.ClientToContact.TryGetValue(ent.Owner, out var contactId))
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
        }
    }

    /// <summary>
    /// The same as a cartridge does: the full state, or only what the ui does not have yet.
    /// </summary>
    private void OnAiStateRequest(Entity<MessengerServerComponent> server,
        EntityUid ai,
        uint contactId,
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

    private static HashSet<uint> CopyIds(HashSet<uint>? ids)
    {
        return ids == null ? new HashSet<uint>() : new HashSet<uint>(ids);
    }

    /// <summary>
    /// Turns a packet meant for a pda into the ui state of the AI window, if the window is open.
    /// Updates missed while it is closed are restored by the full state on the next opening.
    /// </summary>
    private void SendToAi(EntityUid ai, NetworkPayload payload)
    {
        if (!_ui.IsUiOpen(ai, MessengerAiClientUiKey.Key, ai)
            || !payload.TryGetValue(NetworkKey.Command.ToString(), out NetworkCommand? command))
            return;

        switch (command)
        {
            case NetworkCommand.Contact:
                if (MessengerClientCartridgeSystem.TryGetItems(payload,
                        NetworkKey.Contact,
                        NetworkKey.ContactList,
                        out List<MessengerContact> contacts))
                    SendAiState(ai, new MessengerContactUiState(contacts));

                break;
            case NetworkCommand.Chat:
                if (MessengerClientCartridgeSystem.TryGetItems(payload,
                        NetworkKey.Chat,
                        NetworkKey.ChatList,
                        out List<MessengerChat> chats))
                    SendAiState(ai, new MessengerChatUpdateUiState(chats));

                break;
            case NetworkCommand.NewMessage:
                if (payload.TryGetValue(NetworkKey.Message.ToString(), out MessengerMessage? message)
                    && payload.TryGetValue(NetworkKey.ChatId.ToString(), out uint? chatId))
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
        if (server.Comp.ClientToContact.ContainsKey(ai))
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
