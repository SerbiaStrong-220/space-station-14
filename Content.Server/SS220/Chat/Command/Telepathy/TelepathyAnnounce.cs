using Content.Server.Administration;
using Content.Server.Chat.Systems;
using Content.Shared.Administration;
using Content.Shared.SS220.Telepathy;
using Robust.Shared.Console;

namespace Content.Server.SS220.Chat.Command.Telepathy;

[AdminCommand(AdminFlags.Admin)]
public sealed class TelepathyAnnounce : IConsoleCommand
{
    public string Command => "telepathyAnnounce";
    public string Description => "Send announce to all users of specific telepathy channel";
    public string Help => $"{Command} <telepathyChannel> <message>";

public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 3)
        {
            shell.WriteError("Not enough arguments! Need 3.");
            return;
        }

        var message = string.Join(' ', new ArraySegment<string>(args, 1, args.Length-1));
        IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<ChatSystem>().DispatchTelepathyAnnouncement(
            message,
            args[0]
        );
    }
}
