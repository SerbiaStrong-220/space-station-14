using Content.Server.Administration.Managers;
using Content.Shared.Chat;
using Content.Shared.Database;
using Robust.Shared.Network;

namespace Content.Server.Chat.Managers;

/// <summary>
///     Dispatches chat messages to clients.
/// </summary>
internal sealed partial class ChatManager : IChatManager
{
    [Dependency] private readonly IBanManager _banManager = default!;

    private bool ChatBannedForAuthor(ChatChannel chatChannel, NetUserId? author)
    {
        if (!author.HasValue || !_player.TryGetSessionById(author, out var authorSession))
            return false;

        var bannableChat = chatChannel switch
        {
            ChatChannel.LOOC => BannableChats.LOOC,
            ChatChannel.OOC => BannableChats.OOC,
            _ => (BannableChats?)null,
        };

        if (bannableChat.HasValue && _banManager.IsChatBanned(authorSession, bannableChat.Value))
            return true;

        return false;
    }
}
