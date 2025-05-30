using Content.Shared.Database;
using Robust.Shared.Player;
using System.Text.RegularExpressions;

namespace Content.Server.Chat.Managers;

internal sealed partial class ChatManager
{
    [GeneratedRegex(@"[\u0fd5-\u0fd8\u2500-\u25ff\u2800-\u28ff]+")]
    private static partial Regex ProhibitedCharactersRegex();

    public string DeleteProhibitedCharacters(string message, EntityUid player)
    {
        _player.TryGetSessionByEntity(player, out var session);
        return DeleteProhibitedCharacters(message, session);
    }

    public string DeleteProhibitedCharacters(string message, ICommonSession? player = null)
    {
        string censoredMessage = ProhibitedCharactersRegex().Replace(message, string.Empty);
        if (message.Length != censoredMessage.Length && player != null)
        {
            _adminLogger.Add(LogType.Chat, LogImpact.Low, $"{player.Name} tried to send a message with forbidden characters:\n{message}");
            SendAdminAlert(Loc.GetString("chat-manager-founded-prohibited-characters", ("player", player.Name), ("message", message)));
        }

        return censoredMessage;
    }
}
