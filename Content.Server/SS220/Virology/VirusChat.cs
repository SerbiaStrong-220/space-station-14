// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Chat.Managers;
using Content.Shared.Chat;
using Robust.Shared.Player;

namespace Content.Server.SS220.Virology;

public static class VirusChat
{
    /// <summary>Sends a line to players own chat.</summary>
    public static void SendSelfMessage(IChatManager chat, IEntityManager entityManager, EntityUid entity, string message, Color? color = null)
    {
        if (!entityManager.TryGetComponent<ActorComponent>(entity, out var actor))
            return;

        var wrapped = Loc.GetString("chat-manager-server-wrap-message", ("message", message));
        chat.ChatMessageToOne(ChatChannel.Emotes, message, wrapped, EntityUid.Invalid, false, actor.PlayerSession.Channel, color);
    }
}
