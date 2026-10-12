// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using Content.Shared.Access.Components;
using Content.Shared.Bed.Cryostorage;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.PDA;
using Content.Shared.Silicons.StationAi;
using Robust.Shared.Map;
using Content.Server.SS220.CartridgeLoader.Cartridges;
using Content.Shared.SS220.Messenger;
using Content.Shared.SS220.CartridgeLoader.Cartridges;

namespace Content.Server.SS220.Messenger;

public sealed partial class MessengerServerSystem : EntitySystem
{
    /// <summary>
    /// Names follow the id cards, but a card does not tell when it is renamed, so they are checked
    /// when a client asks for the state or sends a message. Changed contacts are sent to every client.
    /// </summary>
    private void UpdateContactNames(Entity<MessengerServerComponent> ent)
    {
        List<MessengerContact>? changed = null;

        foreach (var (idCardUid, contactId) in ent.Comp.ClientToContact)
        {
            if (ent.Comp.LinkedCards.Contains(idCardUid) || !TryGetOwnerName(idCardUid, out var name))
                continue;

            if (!ent.Comp.Contacts.TryGetValue(contactId, out var contact) || contact.Name == name)
                continue;

            contact.Name = name;
            changed ??= new List<MessengerContact>();
            changed.Add(contact);
        }

        if (changed != null)
            SendContactsToAll(ent, changed);
    }

    private bool TryGetOwnerName(EntityUid authUid, [NotNullWhen(true)] out string? name)
    {
        name = null;

        if (HasComp<StationAiHeldComponent>(authUid))
            name = MetaData(authUid).EntityName;
        else if (TryComp<IdCardComponent>(authUid, out var card))
            name = card.FullName;

        return name != null;
    }

    /// <summary>
    /// Recalculates statuses of all contacts and sends the changed ones to every client.
    /// A contact that returned to the server range also gets the server info, so its client resyncs missed messages.
    /// </summary>
    /// <param name="markIgnored">Entity whose cryostorage or AI mark is being removed and must not be counted.</param>
    private void UpdateContactStatuses(Entity<MessengerServerComponent> ent, EntityUid? markIgnored = null)
    {
        var serverMap = Transform(ent.Owner).MapID;
        var powered = IsPowered(ent);
        var statuses = new Dictionary<uint, MessengerContactStatus>();
        List<MessengerContact>? changed = null;

        // a contact with several cards gets the best status of them (Online is the smallest value)
        foreach (var (idCardUid, contactId) in ent.Comp.ClientToContact)
        {
            var status = GetContactStatus(idCardUid, serverMap, powered, markIgnored);

            if (!statuses.TryGetValue(contactId, out var best) || status < best)
                statuses[contactId] = status;
        }

        foreach (var (contactId, status) in statuses)
        {
            if (!ent.Comp.Contacts.TryGetValue(contactId, out var contact) || contact.Status == status)
                continue;

            contact.Status = status;
            changed ??= new List<MessengerContact>();
            changed.Add(contact);

            if (status == MessengerContactStatus.Online)
                SendToContact(ent, contactId, CreateInfoPayload(ent.Comp));
        }

        if (changed != null)
            SendContactsToAll(ent, changed);
    }

    private void SendContactsToAll(Entity<MessengerServerComponent> ent, List<MessengerContact> contacts)
    {
        var payload = new NetworkPayload
        {
            [NetworkKey.Command.ToString()] = NetworkCommand.Contact,
            [NetworkKey.ContactList.ToString()] = contacts,
        };

        foreach (var contactId in ent.Comp.Contacts.Keys)
        {
            SendToContact(ent, contactId, payload);
        }
    }

    private MessengerContactStatus GetContactStatus(EntityUid idCardUid,
        MapId serverMap,
        bool serverPowered,
        EntityUid? markIgnored)
    {
        if (TerminatingOrDeleted(idCardUid))
            return MessengerContactStatus.OutOfRange;

        // the AI has no pda, it is reachable while it sits in a core on the server map
        if (idCardUid != markIgnored && HasComp<StationAiHeldComponent>(idCardUid))
        {
            return serverPowered && Transform(idCardUid).MapID == serverMap
                ? MessengerContactStatus.Online
                : MessengerContactStatus.OutOfRange;
        }

        if (IsInCryostorage(idCardUid, markIgnored))
            return MessengerContactStatus.Cryo;

        // assume contact is reachable only while its id card is inside a pda on the server map
        if (!serverPowered
            || !_containerSystem.TryGetContainingContainer(idCardUid, out var container)
            || container.ID != PdaComponent.PdaIdSlotId)
            return MessengerContactStatus.OutOfRange;

        return Transform(container.Owner).MapID == serverMap
            ? MessengerContactStatus.Online
            : MessengerContactStatus.OutOfRange;
    }

    /// <summary>
    /// A body that left for cryostorage keeps its items, so the outermost container owner is the body itself.
    /// </summary>
    private bool IsInCryostorage(EntityUid uid, EntityUid? ignored)
    {
        var current = uid;

        while (_containerSystem.TryGetContainingContainer(current, out var container))
        {
            current = container.Owner;
        }

        return current != ignored && HasComp<CryostorageContainedComponent>(current);
    }

    private void OnMarkRead(Entity<MessengerServerComponent> ent, DeviceNetworkPacketEvent args)
    {
        if (!TryAuthByIdCard(ent.Comp, args.Data, out var contactId)
            || !args.Data.TryGetValue(MessengerClientCartridgeSystem.NetworkKey.ChatId.ToString(), out uint? chatId))
            return;

        MarkRead(ent.Comp, contactId, chatId.Value);
    }

    private static void MarkRead(MessengerServerComponent server, uint contactId, uint chatId)
    {
        if (!server.Chats.TryGetValue(chatId, out var chat)
            || chat.LastMessageId == null
            || !CanAccessChat(server, contactId, chat.Id))
            return;

        if (!server.LastReadMessages.TryGetValue(contactId, out var readMessages))
        {
            readMessages = new Dictionary<uint, uint>();
            server.LastReadMessages[contactId] = readMessages;
        }

        readMessages[chat.Id] = chat.LastMessageId.Value;
    }

    /// <summary>
    /// Counts messages of other members that the contact has not read yet.
    /// </summary>
    private static int CountUnread(MessengerServerComponent server, uint contactId, MessengerChat chat)
    {
        uint? lastRead = null;

        if (server.LastReadMessages.TryGetValue(contactId, out var readMessages)
            && readMessages.TryGetValue(chat.Id, out var readId))
            lastRead = readId;

        var unread = 0;

        foreach (var messageId in chat.MessagesId)
        {
            if (lastRead != null && messageId <= lastRead)
                continue;

            if (server.Messages.TryGetValue(messageId, out var message) && message.FromContactId != contactId)
                unread++;
        }

        return unread;
    }


    /// <summary>
    /// Sends contacts of chat members that the client does not know yet.
    /// </summary>
    private void SendMissingContacts(Entity<MessengerServerComponent> ent,
        DeviceNetworkPacketEvent args,
        List<MessengerChat> chats)
    {
        var contacts = CollectMissingContacts(ent.Comp,
            chats,
            GetKnownIds(args.Data, MessengerClientCartridgeSystem.NetworkKey.ContactsIds));

        if (contacts.Count == 0)
            return;

        SendResponse(ent, args, new NetworkPayload
        {
            [NetworkKey.Command.ToString()] = NetworkCommand.Contact,
            [NetworkKey.ContactList.ToString()] = contacts,
        });
    }

    /// <summary>
    /// Sends messages of accessed chats that the client does not know yet.
    /// </summary>
    private void SendMissingMessages(Entity<MessengerServerComponent> ent,
        DeviceNetworkPacketEvent args,
        List<MessengerChat> chats)
    {
        var messages = CollectMissingMessages(ent.Comp,
            chats,
            GetKnownIds(args.Data, MessengerClientCartridgeSystem.NetworkKey.MessagesIds));

        if (messages.Count == 0)
            return;

        SendResponse(ent, args, new NetworkPayload
        {
            [NetworkKey.Command.ToString()] = NetworkCommand.Messages,
            [NetworkKey.MessageList.ToString()] = messages,
        });
    }

    /// <summary>
    /// Sends accessed chats that the client does not know yet.
    /// </summary>
    private void SendMissingChats(Entity<MessengerServerComponent> ent,
        DeviceNetworkPacketEvent args,
        List<MessengerChat> chats)
    {
        var missingChats = CollectMissingChats(chats,
            GetKnownIds(args.Data, MessengerClientCartridgeSystem.NetworkKey.CurrentChatIds));

        if (missingChats.Count == 0)
            return;

        SendResponse(ent, args, new NetworkPayload
        {
            [NetworkKey.Command.ToString()] = NetworkCommand.Chat,
            [NetworkKey.ChatList.ToString()] = missingChats,
        });
    }

    /// <param name="knownIds">Ids the client has, filled with the ids of the result.</param>
    private static List<MessengerContact> CollectMissingContacts(MessengerServerComponent server,
        List<MessengerChat> chats,
        HashSet<uint> knownIds)
    {
        var contacts = new List<MessengerContact>();

        foreach (var chat in chats)
        {
            foreach (var memberId in chat.MembersId)
            {
                if (knownIds.Add(memberId) && server.Contacts.TryGetValue(memberId, out var contact))
                    contacts.Add(contact);
            }
        }

        return contacts;
    }

    /// <param name="knownIds">Ids the client has, filled with the ids of the result.</param>
    private static List<MessengerMessage> CollectMissingMessages(MessengerServerComponent server,
        List<MessengerChat> chats,
        HashSet<uint> knownIds)
    {
        var messages = new List<MessengerMessage>();

        foreach (var chat in chats)
        {
            foreach (var messageId in chat.MessagesId)
            {
                if (knownIds.Add(messageId) && server.Messages.TryGetValue(messageId, out var message))
                    messages.Add(message);
            }
        }

        return messages;
    }

    private static List<MessengerChat> CollectMissingChats(List<MessengerChat> chats, HashSet<uint> knownIds)
    {
        var missingChats = new List<MessengerChat>();

        foreach (var chat in chats)
        {
            if (!knownIds.Contains(chat.Id))
                missingChats.Add(chat);
        }

        return missingChats;
    }

    /// <summary>
    /// Restores the whole ui state of an already registered client to minimize network load.
    /// </summary>
    /// <param name="ent">Messenger server</param>
    /// <param name="loader">Device with loaded id card (for now only id card can be authenticator)</param>
    /// <param name="uiState">Restored state</param>
    /// <returns>True if the client is registered and the state was restored.</returns>
    public bool TryRestoreContactUiState(Entity<MessengerServerComponent> ent,
        EntityUid loader,
        [NotNullWhen(true)] out MessengerUiState? uiState)
    {
        uiState = null;

        if (!TryGetIdCard(loader, out var idCardUid, out _))
            return false;

        if (!ent.Comp.ClientToContact.TryGetValue(idCardUid.Value, out var contactId))
            return false;

        SyncDepartmentAccess(ent, contactId, idCardUid.Value);

        return TryBuildContactUiState(ent, contactId, out uiState);
    }

    /// <summary>
    /// Builds the whole ui state of a registered contact: the chats it can access, their last messages and members.
    /// </summary>
    private bool TryBuildContactUiState(Entity<MessengerServerComponent> ent,
        uint contactId,
        [NotNullWhen(true)] out MessengerUiState? uiState)
    {
        uiState = null;

        UpdateContactNames(ent);

        if (!ent.Comp.Contacts.TryGetValue(contactId, out var clientContact))
            return false;

        uiState = new MessengerUiState
        {
            ClientContact = clientContact,
        };

        var index = 0;

        foreach (var chat in GetAccessedChats(ent.Comp, contactId))
        {
            // the sets are copied, the state must not share the server data
            uiState.Chats[chat.Id] = new MessengerChatUiState(chat.Id,
                chat.Name,
                chat.Kind,
                new HashSet<uint>(chat.MembersId),
                new HashSet<uint>(chat.MessagesId),
                chat.LastMessageId,
                index)
            {
                Unread = CountUnread(ent.Comp, contactId, chat),
            };

            index++;

            if (chat.LastMessageId != null && ent.Comp.Messages.TryGetValue(chat.LastMessageId.Value, out var lastMessage))
                uiState.Messages[lastMessage.Id] = lastMessage;

            foreach (var memberId in chat.MembersId)
            {
                if (ent.Comp.Contacts.TryGetValue(memberId, out var member))
                    uiState.Contacts[member.Id] = member;
            }
        }

        return true;
    }
}
