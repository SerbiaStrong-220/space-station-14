// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Server.SS220.Messenger;

/// <summary>
/// Checks of the invariants that the whole system relies on. Used only in asserts, so they are not run in release.
/// </summary>
public sealed partial class MessengerServerSystem
{
    private bool IsConsistent(Entity<MessengerServerComponent> ent)
    {
        var server = ent.Comp;
        var query = EntityQueryEnumerator<MessengerClientComponent>();

        while (query.MoveNext(out _, out var client))
        {
            if (client.Contacts.TryGetValue(ent, out var contactId) && !server.Contacts.ContainsKey(contactId))
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
            if (!server.Contacts.ContainsKey(contactId)
                || !TryGetContactId(ent, aiUid, out var aiContactId)
                || aiContactId != contactId)
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
}
