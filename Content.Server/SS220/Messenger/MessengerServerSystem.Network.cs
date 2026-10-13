// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using Content.Shared.Access.Components;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.SS220.Messenger;

using Robust.Shared.Utility;

namespace Content.Server.SS220.Messenger;

/// <summary>
/// Device network packets between the server and the messenger cartridges of the pdas.
/// </summary>
public sealed partial class MessengerServerSystem
{
    private void InitializeNetwork()
    {
        SubscribeLocalEvent<MessengerServerComponent, DeviceNetworkPacketEvent>(OnNetworkPacket);
    }

    private void OnNetworkPacket(Entity<MessengerServerComponent> ent, ref DeviceNetworkPacketEvent args)
    {
        if (!args.Data.TryGetValue(MessengerNetworkKey.Command.ToString(), out MessengerNetworkCommand? command))
            return;

        switch (command)
        {
            case MessengerNetworkCommand.CheckServer:
                OnCheckServer(ent, args);
                break;
            case MessengerNetworkCommand.StateUpdate:
                OnStateUpdate(ent, args);
                break;
            case MessengerNetworkCommand.MessageSend:
                OnMessageSend(ent, args);
                break;
            case MessengerNetworkCommand.MarkRead:
                OnMarkRead(ent, args);
                break;
            case MessengerNetworkCommand.SetNameLock:
                OnSetNameLock(ent, args);
                break;
            case MessengerNetworkCommand.SetChatMute:
                OnSetChatMute(ent, args);
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
        if (!TryAuthByIdCard(ent, args.Data, out var contactId))
            return;

        UpdateContactNames(ent);

        var chats = GetAccessedChats(ent.Comp, contactId);

        SendMissingContacts(ent, args, chats);
        SendMissingMessages(ent, args, chats);
        SendMissingChats(ent, args, chats);
    }

    private void OnMessageSend(Entity<MessengerServerComponent> ent, DeviceNetworkPacketEvent args)
    {
        if (!TryAuthByIdCard(ent, args.Data, out var senderId))
            return;

        UpdateContactNames(ent);

        // access may have changed since the last event, and a department chat must not outlive it
        if (TryGetIdCard(args.Data, out var senderCard, out _))
            SyncDepartmentAccess(ent, senderId, senderCard.Value);

        if (!args.Data.TryGetValue(MessengerNetworkKey.ChatId.ToString(), out ChatUid? chatId))
            return;

        if (!args.Data.TryGetValue(MessengerNetworkKey.MessageText.ToString(), out string? messageText))
            return;

        MessageUid? requestedReplyId = args.Data.TryGetValue(MessengerNetworkKey.ReplyToId.ToString(),
            out MessageUid? replyId)
            ? replyId
            : null;

        SendMessage(ent, senderId, chatId.Value, messageText, requestedReplyId, $"{args.Sender}");
    }

    private void OnMarkRead(Entity<MessengerServerComponent> ent, DeviceNetworkPacketEvent args)
    {
        if (!TryAuthByIdCard(ent, args.Data, out var contactId)
            || !args.Data.TryGetValue(MessengerNetworkKey.ChatId.ToString(), out ChatUid? chatId))
            return;

        MarkRead(ent.Comp, contactId, chatId.Value);
    }

    private void OnSetChatMute(Entity<MessengerServerComponent> ent, DeviceNetworkPacketEvent args)
    {
        if (!TryAuthByIdCard(ent, args.Data, out var contactId)
            || !args.Data.TryGetValue(MessengerNetworkKey.ChatId.ToString(), out ChatUid? chatId)
            || !args.Data.TryGetValue(MessengerNetworkKey.ChatMuted.ToString(), out bool? muted))
            return;

        SetChatMute(ent.Comp, contactId, chatId.Value, muted.Value);
    }

    private void OnSetNameLock(Entity<MessengerServerComponent> ent, DeviceNetworkPacketEvent args)
    {
        if (!TryAuthByIdCard(ent, args.Data, out var contactId)
            || !args.Data.TryGetValue(MessengerNetworkKey.NameLocked.ToString(), out bool? locked))
            return;

        SetNameLock(ent, contactId, locked.Value);
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
            CopyKnownIds<ContactUid>(args.Data, MessengerNetworkKey.ContactsIds));

        if (contacts.Count == 0)
            return;

        SendResponse(ent, args, new NetworkPayload
        {
            [MessengerNetworkKey.Command.ToString()] = MessengerNetworkCommand.Contact,
            [MessengerNetworkKey.ContactList.ToString()] = contacts,
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
            CopyKnownIds<MessageUid>(args.Data, MessengerNetworkKey.MessagesIds));

        if (messages.Count == 0)
            return;

        SendResponse(ent, args, new NetworkPayload
        {
            [MessengerNetworkKey.Command.ToString()] = MessengerNetworkCommand.Messages,
            [MessengerNetworkKey.MessageList.ToString()] = messages,
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
            CopyKnownIds<ChatUid>(args.Data, MessengerNetworkKey.CurrentChatIds));

        if (missingChats.Count == 0)
            return;

        SendResponse(ent, args, new NetworkPayload
        {
            [MessengerNetworkKey.Command.ToString()] = MessengerNetworkCommand.Chat,
            [MessengerNetworkKey.ChatList.ToString()] = missingChats,
        });
    }

    private static NetworkPayload CreateInfoPayload(MessengerServerComponent server)
    {
        return new NetworkPayload
        {
            [MessengerNetworkKey.Command.ToString()] = MessengerNetworkCommand.Info,
            [MessengerNetworkKey.ServerName.ToString()] = server.Name,
        };
    }

    private void SendResponse(Entity<MessengerServerComponent> ent, DeviceNetworkPacketEvent args, NetworkPayload payload)
    {
        _deviceNetwork.QueuePacket(ent, args.SenderAddress, payload);
    }

    /// <summary>
    /// Sends the packet to every client address of the contact (a contact can have many connected clients).
    /// </summary>
    private void SendToContact(Entity<MessengerServerComponent> ent, ContactUid contactId, NetworkPayload payload)
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
    /// Returns a modifiable copy of the id set sent by the client, or an empty set if there is none.
    /// </summary>
    private static HashSet<T> CopyKnownIds<T>(NetworkPayload payload, MessengerNetworkKey key)
    {
        return CopyIds(payload.TryGetValue(key.ToString(), out HashSet<T>? ids) ? ids : null);
    }

    /// <summary>
    /// Finds the id card in the device from the payload. The id card is the client authentication entity.
    /// </summary>
    /// <returns>True if the id card is already registered on the server.</returns>
    private bool TryAuthByIdCard(Entity<MessengerServerComponent> ent, NetworkPayload payload, out ContactUid contactId)
    {
        contactId = default;

        return TryGetIdCard(payload, out var idCardUid, out _)
               && TryGetContactId(ent, idCardUid.Value, out contactId);
    }

    /// <summary>
    /// Same as <see cref="TryAuthByIdCard"/>, but creates a new contact if the id card is not registered yet.
    /// </summary>
    /// <returns>True if the client is registered (now or before).</returns>
    private bool TryRegisterByIdCard(Entity<MessengerServerComponent> ent,
        string netAddress,
        NetworkPayload payload,
        out ContactUid contactId)
    {
        contactId = default;

        if (!TryGetIdCard(payload, out var idCardUid, out var idCard))
            return false;

        if (TryGetContactId(ent, idCardUid.Value, out contactId))
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

        if (payload.TryGetValue(MessengerNetworkKey.DeviceUid.ToString(), out NetEntity? netLoader))
            return TryGetIdCard(GetEntity(netLoader), out idCardUid, out idCard);

        if (payload.TryGetValue(MessengerNetworkKey.DeviceUid.ToString(), out EntityUid? loader))
            return TryGetIdCard(loader, out idCardUid, out idCard);

        return false;
    }

    private bool HasServerAccess(Entity<MessengerServerComponent> ent, NetworkPayload payload)
    {
        return TryGetIdCard(payload, out var idCardUid, out _) && HasServerAccess(ent, idCardUid.Value);
    }
}
