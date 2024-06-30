// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Chat.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Enums;

namespace Content.Server.SS220.Chat.Command.Telepathy;

[AnyCommand]
public sealed class TelepathyCommand : IConsoleCommand
{
    public string Command => "telepathy";
    public string Description => "Send message through the power of mind";
    public string Help => $"{Command} <text>";
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } player)
        {
            shell.WriteError("This command cannot be run from the server.");
            return;
        }

        if (player.Status != SessionStatus.InGame)
            return;

        if (player.AttachedEntity is not {} playerEntity)
        {
            shell.WriteError("You don't have an entity!");
            return;
        }

        if (args.Length < 1)
            return;

        var message = string.Join(" ", args).Trim();
        if (string.IsNullOrEmpty(message))
            return;

        IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<ChatSystem>()
            .TrySendInGameICMessage(
                playerEntity,
                message,
                InGameICChatType.Telepathy,
                ChatTransmitRange.HideChat,
                false,
                shell,
                player
            );
    }
}
