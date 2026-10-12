// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using Content.Server.Chat.Systems;
using Content.Shared.Access.Components;
using Content.Shared.DeviceNetwork;
using Content.Shared.Database;
using Content.Shared.SS220.CartridgeLoader.Cartridges;
using Content.Shared.SS220.Messenger;

using Robust.Shared.Utility;

namespace Content.Server.SS220.Messenger;

/// <summary>
/// Chats, messages, read marks and the access of the contacts to the chats.
/// </summary>
public sealed partial class MessengerServerSystem
{
    [Dependency] private ChatSystem _chat = default!;

    private const int MaxMessageLength = 500;

    /// <summary>
    /// Cuts the text of a message to the allowed length and applies the word filter.
    /// </summary>
    public string SanitizeMessage(string text)
    {
        return _chat.ReplaceWords(text.Length > MaxMessageLength ? text[..MaxMessageLength] : text);
    }

    /// <summary>
    /// Adds the message to the chat and delivers it to the members if the sender may write there.
    /// </summary>
    /// <param name="requestedReplyId">Message to reply to, ignored if it is not in the same chat.</param>
    /// <param name="senderLog">Description of the sender for the admin log.</param>
    private void SendMessage(Entity<MessengerServerComponent> ent,
        ContactUid senderId,
        ChatUid chatId,
        string messageText,
        MessageUid? requestedReplyId,
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
        MessageUid? replyToId = null;

        if (requestedReplyId != null
            && server.Messages.TryGetValue(requestedReplyId.Value, out var replyTarget)
            && replyTarget.ChatId == chat.Id)
            replyToId = replyTarget.Id;

        var message = new MessengerMessage(chat.Id, senderId,
            _gameTiming.CurTime - _gameTicker.RoundStartTimeSpan, messageText)
        {
            Id = new MessageUid(server.NextId++),
            ReplyToId = replyToId,
        };

        server.Messages[message.Id] = message;

        chat.LastMessageId = message.Id;
        chat.MessagesId.Add(message.Id);

        DebugTools.Assert(IsConsistent(ent), "Messenger server data is inconsistent after sending a message");

        var payload = new NetworkPayload
        {
            [MessengerNetworkKey.Command.ToString()] = MessengerNetworkCommand.NewMessage,
            [MessengerNetworkKey.ChatId.ToString()] = chat.Id,
            [MessengerNetworkKey.Message.ToString()] = message,
        };

        // the sender gets the message back to sync its ui, but without the ringtone
        var senderPayload = new NetworkPayload
        {
            [MessengerNetworkKey.Command.ToString()] = MessengerNetworkCommand.NewMessage,
            [MessengerNetworkKey.ChatId.ToString()] = chat.Id,
            [MessengerNetworkKey.Message.ToString()] = message,
            [MessengerNetworkKey.Silent.ToString()] = true,
        };

        foreach (var memberId in chat.MembersId)
        {
            SendToContact(ent, memberId, memberId == senderId ? senderPayload : payload);
        }

        _adminLogger.Add(LogType.MessengerServer, LogImpact.Low,
            $"Send: sender entity: {senderLog}, chat: {chat.Name}, chatID: {chat.Id}, msg: {messageText}");
    }

    private static bool CanAccessChat(MessengerServerComponent server, ContactUid contactId, ChatUid chatId)
    {
        if (server.PublicChats.Contains(chatId))
            return true;

        return server.PrivateChats.TryGetValue(contactId, out var privateChats) && privateChats.Contains(chatId);
    }

    /// <summary>
    /// Returns public chats and chats where the contact was invited.
    /// </summary>
    private static List<MessengerChat> GetAccessedChats(MessengerServerComponent server, ContactUid contactId)
    {
        var chatIds = new HashSet<ChatUid>(server.PublicChats);

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

    private static void AddPrivateChat(MessengerServerComponent server, ContactUid contactId, ChatUid chatId)
    {
        if (!server.PrivateChats.TryGetValue(contactId, out var privateChats))
        {
            privateChats = new HashSet<ChatUid>();
            server.PrivateChats[contactId] = privateChats;
        }

        privateChats.Add(chatId);
    }

    private static void MarkRead(MessengerServerComponent server, ContactUid contactId, ChatUid chatId)
    {
        if (!server.Chats.TryGetValue(chatId, out var chat)
            || chat.LastMessageId == null
            || !CanAccessChat(server, contactId, chat.Id))
            return;

        if (!server.LastReadMessages.TryGetValue(contactId, out var readMessages))
        {
            readMessages = new Dictionary<ChatUid, MessageUid>();
            server.LastReadMessages[contactId] = readMessages;
        }

        readMessages[chat.Id] = chat.LastMessageId.Value;
    }

    /// <summary>
    /// Counts messages of other members that the contact has not read yet.
    /// </summary>
    private static int CountUnread(MessengerServerComponent server, ContactUid contactId, MessengerChat chat)
    {
        MessageUid? lastRead = null;

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

    /// <param name="knownIds">Ids the client has, filled with the ids of the result.</param>
    private static List<MessengerContact> CollectMissingContacts(MessengerServerComponent server,
        List<MessengerChat> chats,
        HashSet<ContactUid> knownIds)
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
        HashSet<MessageUid> knownIds)
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

    private static List<MessengerChat> CollectMissingChats(List<MessengerChat> chats, HashSet<ChatUid> knownIds)
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

        if (!TryGetContactId(ent, idCardUid.Value, out var contactId))
            return false;

        SyncDepartmentAccess(ent, contactId, idCardUid.Value);

        return TryBuildContactUiState(ent, contactId, out uiState);
    }

    /// <summary>
    /// Builds the whole ui state of a registered contact: the chats it can access, their last messages and members.
    /// </summary>
    private bool TryBuildContactUiState(Entity<MessengerServerComponent> ent,
        ContactUid contactId,
        [NotNullWhen(true)] out MessengerUiState? uiState)
    {
        uiState = null;

        UpdateContactNames(ent);

        if (!ent.Comp.Contacts.TryGetValue(contactId, out var clientContact))
            return false;

        uiState = new MessengerUiState
        {
            ClientContact = clientContact,
            NameLocked = ent.Comp.NameLockedContacts.Contains(contactId),
        };

        var index = 0;

        foreach (var chat in GetAccessedChats(ent.Comp, contactId))
        {
            // the sets are copied, the state must not share the server data
            uiState.Chats[chat.Id] = new MessengerChatUiState(chat.Id,
                chat.Name,
                chat.Kind,
                new HashSet<ContactUid>(chat.MembersId),
                new HashSet<MessageUid>(chat.MessagesId),
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

    /// <summary>
    /// Creates a group chat for every department of the server that does not have one yet.
    /// </summary>
    private void EnsureDepartmentChats(Entity<MessengerServerComponent> ent)
    {
        foreach (var (access, name) in ent.Comp.DepartmentChats)
        {
            if (ent.Comp.DepartmentChatAccess.ContainsValue(access))
                continue;

            var chat = new MessengerChat(Loc.GetString(name), MessengerChatKind.Chat, new HashSet<ContactUid>())
            {
                Id = new ChatUid(ent.Comp.NextId++),
            };
            ent.Comp.Chats[chat.Id] = chat;
            ent.Comp.DepartmentChatAccess[chat.Id] = access;
        }
    }

    /// <summary>
    /// Adds the contact to the department chats its id card has the access to and removes it from the others.
    /// A linked client (e.g. a clone) can only add, because it does not have to repeat the access of the original.
    /// </summary>
    private void SyncDepartmentAccess(Entity<MessengerServerComponent> ent, ContactUid contactId, EntityUid clientUid)
    {
        var server = ent.Comp;

        if (server.DepartmentChatAccess.Count == 0 || !HasComp<IdCardComponent>(clientUid))
            return;

        var accessTags = _accessSystem.FindAccessTags(clientUid);
        var canRemove = !IsLinkedClient(clientUid);

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
                    [MessengerNetworkKey.Command.ToString()] = MessengerNetworkCommand.Chat,
                    [MessengerNetworkKey.Chat.ToString()] = chat,
                });
            }
            else if (isMember && canRemove)
            {
                chat.MembersId.Remove(contactId);

                if (server.PrivateChats.TryGetValue(contactId, out var privateChats))
                    privateChats.Remove(chatId);
            }
        }

        DebugTools.Assert(IsConsistent(ent), "Messenger server data is inconsistent after syncing department access");
    }
}
