// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using Content.Server.Administration.Logs;
using Content.Server.CartridgeLoader;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Power.Components;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Bed.Cryostorage;
using Content.Shared.CartridgeLoader;
using Content.Shared.Database;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.GameTicking;
using Content.Shared.PDA;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Power;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Content.Server.SS220.CartridgeLoader.Cartridges;
using Content.Shared.SS220.Messenger;

namespace Content.Server.SS220.Messenger;

public sealed partial class MessengerServerSystem : EntitySystem
{
    [Dependency] private CartridgeLoaderSystem _cartridgeLoader = default!;
    [Dependency] private DeviceNetworkSystem _deviceNetwork = default!;
    [Dependency] private SharedIdCardSystem _idCard = default!;
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private AccessReaderSystem _accessSystem = default!;
    [Dependency] private SharedContainerSystem _containerSystem = default!;
    [Dependency] private SharedGameTicker _gameTicker = default!;
    [Dependency] private IAdminLogManager _adminLogger = default!;

    // TODO: move to shared for cross systems usage
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
        Silent,
    }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MessengerServerComponent, DeviceNetworkPacketEvent>(OnNetworkPacket);
        SubscribeLocalEvent<MessengerServerComponent, MapInitEvent>(OnServerMapInit);
        SubscribeLocalEvent<MessengerServerComponent, PowerChangedEvent>(OnServerPowerChanged);

        // NOTE: Contacts are born and move only when an id card or a cartridge appears in a pda, so we listen to that.
        // The pda and cartridge side is subscribed by other systems already, so we listen from the id card side
        // NOTE2: receive the cartridge installation from MessengerClientCartridgeSystem.
        SubscribeLocalEvent<IdCardComponent, EntGotInsertedIntoContainerMessage>(OnIdCardInserted);
        SubscribeLocalEvent<IdCardComponent, EntGotRemovedFromContainerMessage>(OnIdCardRemoved);
        SubscribeLocalEvent<MessengerClientCartridgeComponent, CartridgeRemovedEvent>(OnCartridgeRemoved);

        // events that change the status of a contact
        SubscribeLocalEvent<IdCardComponent, EntityTerminatingEvent>(OnIdCardTerminating);

        SubscribeLocalEvent<CryostorageContainedComponent, ComponentStartup>(OnCryostorageStartup);
        SubscribeLocalEvent<CryostorageContainedComponent, ComponentRemove>(OnCryostorageRemove);

        SubscribeLocalEvent<MessengerServerComponent, EntParentChangedMessage>(OnParentChanged);

        SubscribeLocalEvent<StationAiHeldComponent, ComponentStartup>(OnAiHeldStartup);
        SubscribeLocalEvent<StationAiHeldComponent, ComponentRemove>(OnAiHeldRemove);

        InitializeAi();
        InitializeAiContact();
    }

    private void OnServerMapInit(Entity<MessengerServerComponent> ent, ref MapInitEvent args)
    {
        EnsureDepartmentChats(ent);
        RegisterAllCartridges(ent);
    }

    private void OnServerPowerChanged(Entity<MessengerServerComponent> ent, ref PowerChangedEvent args)
    {
        // the server did not see the cartridges while it was off
        if (args.Powered)
            RegisterAllCartridges(ent);
        else
            UpdateContactStatuses(ent);
    }

    private void OnIdCardInserted(Entity<IdCardComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        if (args.Container.ID == PdaComponent.PdaIdSlotId)
            RefreshLoader(args.Container.Owner);
    }

    private void OnIdCardRemoved(Entity<IdCardComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        if (args.Container.ID == PdaComponent.PdaIdSlotId)
            RefreshLoader(args.Container.Owner);
    }

    /// <summary>
    /// Called by the cartridge system when the cartridge is installed to a loader.
    /// </summary>
    public void OnCartridgeInstalled(EntityUid cartridge, EntityUid loader)
    {
        var query = EntityQueryEnumerator<MessengerServerComponent>();

        while (query.MoveNext(out var serverUid, out var server))
        {
            if (Transform(serverUid).MapID != Transform(cartridge).MapID)
                continue;

            var serverEnt = (serverUid, server);

            RefreshCartridge(serverEnt, cartridge, loader);
            UpdateContactStatuses(serverEnt);

            break;
        }
    }

    private void OnCartridgeRemoved(Entity<MessengerClientCartridgeComponent> ent, ref CartridgeRemovedEvent args)
    {
        if (!TryComp<DeviceNetworkComponent>(ent, out var deviceNet) || string.IsNullOrEmpty(deviceNet.Address))
            return;

        var query = EntityQueryEnumerator<MessengerServerComponent>();

        while (query.MoveNext(out _, out var server))
        {
            ReleaseAddress(server, deviceNet.Address);
        }
    }

    private void OnIdCardTerminating(Entity<IdCardComponent> ent, ref EntityTerminatingEvent args)
    {
        UpdateAllContactStatuses();
    }

    private void OnCryostorageStartup(Entity<CryostorageContainedComponent> ent, ref ComponentStartup args)
    {
        UpdateAllContactStatuses();
    }

    private void OnCryostorageRemove(Entity<CryostorageContainedComponent> ent, ref ComponentRemove args)
    {
        UpdateAllContactStatuses(ent);
    }

    /// <summary>
    /// A grid that jumped to another map takes everyone on it out of the server range.
    /// </summary>
    private void OnParentChanged(Entity<MessengerServerComponent> ent, ref EntParentChangedMessage args)
    {
        UpdateAllContactStatuses();
    }

    private void UpdateAllContactStatuses(EntityUid? markIgnored = null)
    {
        var query = EntityQueryEnumerator<MessengerServerComponent>();

        while (query.MoveNext(out var serverUid, out var server))
        {
            UpdateContactStatuses((serverUid, server), markIgnored);
        }
    }

    /// <summary>
    /// Rebinds the messenger cartridges of the loader, e.g. after its id card was changed.
    /// </summary>
    private void RefreshLoader(EntityUid loader)
    {
        var query = EntityQueryEnumerator<MessengerServerComponent>();

        while (query.MoveNext(out var serverUid, out var server))
        {
            var serverEnt = (serverUid, server);

            foreach (var program in _cartridgeLoader.GetInstalled(loader))
            {
                if (HasComp<MessengerClientCartridgeComponent>(program))
                    RefreshCartridge(serverEnt, program, loader);
            }

            if (TryComp<CartridgeLoaderComponent>(loader, out var loaderComp)
                && loaderComp.CartridgeSlot.Item is { } slotItem
                && HasComp<MessengerClientCartridgeComponent>(slotItem))
                RefreshCartridge(serverEnt, slotItem, loader);

            UpdateContactStatuses(serverEnt);
        }
    }

    /// <summary>
    /// Lets the clone of a body write from the same messenger account as the original:
    /// the id card of the clone starts to use the contact of the original id card.
    /// Call it when the clone already has its id card.
    /// </summary>
    /// <returns>True if the original is registered on some server and the clone card was linked to it.</returns>
    public bool TryLinkClone(EntityUid original, EntityUid clone)
    {
        if (!_idCard.TryFindIdCard(original, out var originalCard)
            || !_idCard.TryFindIdCard(clone, out var cloneCard))
            return false;

        var linked = false;
        var query = EntityQueryEnumerator<MessengerServerComponent>();

        while (query.MoveNext(out var serverUid, out var server))
        {
            if (!server.ClientToContact.TryGetValue(originalCard.Owner, out var contactId)
                || server.ClientToContact.ContainsKey(cloneCard.Owner))
                continue;

            server.ClientToContact[cloneCard.Owner] = contactId;
            server.LinkedCards.Add(cloneCard.Owner);
            linked = true;

            DebugTools.Assert(IsConsistent(server), "Messenger server data is inconsistent after linking a clone");

            if (_containerSystem.TryGetContainingContainer(cloneCard.Owner, out var container)
                && container.ID == PdaComponent.PdaIdSlotId)
                RefreshLoader(container.Owner);
        }

        return linked;
    }

    /// <summary>
    /// Creates a group chat for every department of the server that does not have one yet.
    /// </summary>
    private void EnsureDepartmentChats(Entity<MessengerServerComponent> ent)
    {
        foreach (var (access, name) in ent.Comp.DepartmentChats)
        {
            if (ent.Comp.DepartmentChatAccess.ContainsValue(access))
                continue;

            var chat = new MessengerChat(Loc.GetString(name), MessengerChatKind.Chat, new HashSet<uint>())
            {
                Id = ent.Comp.NextId++,
            };
            ent.Comp.Chats[chat.Id] = chat;
            ent.Comp.DepartmentChatAccess[chat.Id] = access;
        }
    }

    /// <summary>
    /// Adds the contact to the department chats its id card has the access to and removes it from the others.
    /// A linked card (e.g. of a clone) can only add, because it does not have to repeat the access of the original.
    /// </summary>
    private void SyncDepartmentAccess(Entity<MessengerServerComponent> ent, uint contactId, EntityUid authUid)
    {
        var server = ent.Comp;

        if (server.DepartmentChatAccess.Count == 0 || !HasComp<IdCardComponent>(authUid))
            return;

        var accessTags = _accessSystem.FindAccessTags(authUid);
        var canRemove = !server.LinkedCards.Contains(authUid);

        foreach (var (chatId, access) in server.DepartmentChatAccess)
        {
            var chat = server.Chats[chatId];
            var isMember = chat.MembersId.Contains(contactId);

            if (accessTags.Contains(access))
            {
                if (isMember)
                    continue;

                chat.MembersId.Add(contactId);
                AddPrivateChat(server, contactId, chatId);
                SendToContact(ent, contactId, new NetworkPayload
                {
                    [NetworkKey.Command.ToString()] = NetworkCommand.Chat,
                    [NetworkKey.Chat.ToString()] = chat,
                });
            }
            else if (isMember && canRemove)
            {
                chat.MembersId.Remove(contactId);

                if (server.PrivateChats.TryGetValue(contactId, out var privateChats))
                    privateChats.Remove(chatId);
            }
        }

        DebugTools.Assert(IsConsistent(server), "Messenger server data is inconsistent after syncing department access");
    }

    private void OnAiHeldStartup(Entity<StationAiHeldComponent> ent, ref ComponentStartup args)
    {
        RegisterAiOnServers(ent);
    }

    private void OnAiHeldRemove(Entity<StationAiHeldComponent> ent, ref ComponentRemove args)
    {
        UpdateAllContactStatuses(ent);
    }

    /// <summary>
    /// Registers owners of all messenger cartridges that have access to the server,
    /// even if nobody has opened the cartridge. Contacts are never removed to keep the chat history.
    /// </summary>
    private void RegisterAllCartridges(Entity<MessengerServerComponent> ent)
    {
        var query = EntityQueryEnumerator<MessengerClientCartridgeComponent>();

        while (query.MoveNext(out var cartridgeUid, out var cartridge))
        {
            if (cartridge.Loader.IsValid())
                RefreshCartridge(ent, cartridgeUid, cartridge.Loader);
        }

        RegisterAllAi(ent);
        UpdateContactStatuses(ent);
    }

    /// <summary>
    /// A contact is reachable only on the cartridge that currently holds its id card. Registers the owner of the
    /// cartridge if it has access, binds the cartridge address to it and drops the address from the previous owner,
    /// otherwise the previous owner would keep receiving the messages on this pda.
    /// </summary>
    private void RefreshCartridge(Entity<MessengerServerComponent> ent, EntityUid cartridge, EntityUid loader)
    {
        if (!IsPowered(ent)
            || !TryComp<DeviceNetworkComponent>(cartridge, out var deviceNet)
            || string.IsNullOrEmpty(deviceNet.Address))
            return;

        var address = deviceNet.Address;

        if (!TryGetIdCard(loader, out var idCardUid, out var idCard))
        {
            ReleaseAddress(ent.Comp, address);
            return;
        }

        if (ent.Comp.ClientToContact.TryGetValue(idCardUid.Value, out var contactId))
        {
            BindAddress(ent, contactId, address);
            SyncDepartmentAccess(ent, contactId, idCardUid.Value);
            return;
        }

        ReleaseAddress(ent.Comp, address);

        if (!HasServerAccess(ent, idCardUid.Value))
            return;

        contactId = RegisterNewClient(ent, idCardUid.Value, idCard.FullName ?? "#unkn##n", address);
        SendToContact(ent, contactId, CreateInfoPayload(ent.Comp));
    }

    private void BindAddress(Entity<MessengerServerComponent> ent, uint contactId, string address)
    {
        if (!ent.Comp.Contacts.TryGetValue(contactId, out var contact))
            return;

        foreach (var other in ent.Comp.Contacts.Values)
        {
            if (other.Id != contactId)
                other.ActiveAddresses.Remove(address);
        }

        // a client on a new address does not know the server yet
        if (contact.ActiveAddresses.Add(address))
            _deviceNetwork.QueuePacket(ent, address, CreateInfoPayload(ent.Comp));

        DebugTools.Assert(IsAddressUnique(ent.Comp, address), $"Address {address} belongs to several messenger contacts");
    }

    private static void ReleaseAddress(MessengerServerComponent server, string address)
    {
        foreach (var contact in server.Contacts.Values)
        {
            contact.ActiveAddresses.Remove(address);
        }
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
            case MessengerClientCartridgeSystem.NetworkCommand.MarkRead:
                OnMarkRead(ent, args);
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

        UpdateContactNames(ent);

        var chats = GetAccessedChats(ent.Comp, contactId);

        SendMissingContacts(ent, args, chats);
        SendMissingMessages(ent, args, chats);
        SendMissingChats(ent, args, chats);
    }

    private void OnMessageSend(Entity<MessengerServerComponent> ent, DeviceNetworkPacketEvent args)
    {
        var server = ent.Comp;

        if (!TryAuthByIdCard(server, args.Data, out var senderId))
            return;

        UpdateContactNames(ent);

        // access may have changed since the last event, and a department chat must not outlive it
        if (TryGetIdCard(args.Data, out var senderCard, out _))
            SyncDepartmentAccess(ent, senderId, senderCard.Value);

        if (!args.Data.TryGetValue(MessengerClientCartridgeSystem.NetworkKey.ChatId.ToString(), out uint? chatId))
            return;

        if (!args.Data.TryGetValue(MessengerClientCartridgeSystem.NetworkKey.MessageText.ToString(),
                out string? messageText))
            return;

        uint? requestedReplyId = args.Data.TryGetValue(MessengerClientCartridgeSystem.NetworkKey.ReplyToId.ToString(),
            out uint? replyId)
            ? replyId
            : null;

        SendMessage(ent, senderId, chatId.Value, messageText, requestedReplyId, $"{args.Sender}");
    }

    /// <summary>
    /// Adds the message to the chat and delivers it to the members if the sender may write there.
    /// </summary>
    /// <param name="requestedReplyId">Message to reply to, ignored if it is not in the same chat.</param>
    /// <param name="senderLog">Description of the sender for the admin log.</param>
    private void SendMessage(Entity<MessengerServerComponent> ent,
        uint senderId,
        uint chatId,
        string messageText,
        uint? requestedReplyId,
        string senderLog)
    {
        var server = ent.Comp;

        if (!server.Chats.TryGetValue(chatId, out var chat) || !CanAccessChat(server, senderId, chat.Id))
        {
            _adminLogger.Add(LogType.MessengerServer, LogImpact.Low,
                $"No access: sender entity: {senderLog}, chat: {chat?.Name}, chatID: {chatId}, msg: {messageText}");
            return;
        }

        // a reply must point to a message of the same chat
        uint? replyToId = null;

        if (requestedReplyId != null
            && server.Messages.TryGetValue(requestedReplyId.Value, out var replyTarget)
            && replyTarget.ChatId == chat.Id)
            replyToId = replyTarget.Id;

        var message = new MessengerMessage(chat.Id, senderId,
            _gameTiming.CurTime - _gameTicker.RoundStartTimeSpan, messageText)
        {
            Id = server.NextId++,
            ReplyToId = replyToId,
        };

        server.Messages[message.Id] = message;

        chat.LastMessageId = message.Id;
        chat.MessagesId.Add(message.Id);

        DebugTools.Assert(IsConsistent(server), "Messenger server data is inconsistent after sending a message");

        var payload = new NetworkPayload
        {
            [NetworkKey.Command.ToString()] = NetworkCommand.NewMessage,
            [NetworkKey.ChatId.ToString()] = chat.Id,
            [NetworkKey.Message.ToString()] = message,
        };

        // the sender gets the message back to sync its ui, but without the ringtone
        var senderPayload = new NetworkPayload
        {
            [NetworkKey.Command.ToString()] = NetworkCommand.NewMessage,
            [NetworkKey.ChatId.ToString()] = chat.Id,
            [NetworkKey.Message.ToString()] = message,
            [NetworkKey.Silent.ToString()] = true,
        };

        foreach (var memberId in chat.MembersId)
        {
            SendToContact(ent, memberId, memberId == senderId ? senderPayload : payload);
        }

        _adminLogger.Add(LogType.MessengerServer, LogImpact.Low,
            $"Send: sender entity: {senderLog}, chat: {chat.Name}, chatID: {chat.Id}, msg: {messageText}");
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

    /// <summary>
    /// Invariants of the server data that the whole system relies on. Used only in asserts.
    /// </summary>
    private static bool IsConsistent(MessengerServerComponent server)
    {
        foreach (var contactId in server.ClientToContact.Values)
        {
            if (!server.Contacts.ContainsKey(contactId))
                return false;
        }

        foreach (var cardUid in server.LinkedCards)
        {
            if (!server.ClientToContact.ContainsKey(cardUid))
                return false;
        }

        foreach (var chat in server.Chats.Values)
        {
            foreach (var memberId in chat.MembersId)
            {
                if (!server.Contacts.ContainsKey(memberId))
                    return false;
            }

            foreach (var messageId in chat.MessagesId)
            {
                if (!server.Messages.TryGetValue(messageId, out var message) || message.ChatId != chat.Id)
                    return false;
            }

            if (chat.LastMessageId != null && !chat.MessagesId.Contains(chat.LastMessageId.Value))
                return false;
        }

        foreach (var privateChats in server.PrivateChats.Values)
        {
            foreach (var chatId in privateChats)
            {
                if (!server.Chats.ContainsKey(chatId))
                    return false;
            }
        }

        foreach (var chatId in server.DepartmentChatAccess.Keys)
        {
            if (!server.Chats.ContainsKey(chatId))
                return false;
        }

        foreach (var (contactId, aiUid) in server.AiContacts)
        {
            if (!server.Contacts.ContainsKey(contactId) || !server.ClientToContact.ContainsKey(aiUid))
                return false;
        }

        return true;
    }

    private static bool IsAddressUnique(MessengerServerComponent server, string address)
    {
        var owners = 0;

        foreach (var contact in server.Contacts.Values)
        {
            if (contact.ActiveAddresses.Contains(address))
                owners++;
        }

        return owners <= 1;
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

        if (ent.Comp.AiContacts.TryGetValue(contactId, out var aiUid))
        {
            SendToAi(aiUid, payload);
            return;
        }

        foreach (var address in contact.ActiveAddresses)
        {
            _deviceNetwork.QueuePacket(ent, address, payload);
        }
    }

    /// <summary>
    /// Creates a contact for the authentication entity (id card or AI) and private chats between it and every contact of the server
    /// (including a "favorites" chat with itself), then notifies connected clients.
    /// </summary>
    /// <returns>Id of the new contact.</returns>
    private uint RegisterNewClient(Entity<MessengerServerComponent> ent,
        EntityUid idCardUid,
        string fullName,
        string? netAddress)
    {
        var server = ent.Comp;
        var newContact = new MessengerContact
        {
            Id = server.NextId++,
            Name = fullName,
        };

        if (netAddress != null)
            newContact.ActiveAddresses.Add(netAddress);

        server.Contacts[newContact.Id] = newContact;
        server.ClientToContact[idCardUid] = newContact.Id;

        // for now every contact can write to every other contact
        foreach (var contactId in server.Contacts.Keys)
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

        SyncDepartmentAccess(ent, newContact.Id, idCardUid);

        DebugTools.Assert(IsConsistent(server), "Messenger server data is inconsistent after registering a client");

        return newContact.Id;
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
