// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Administration;
using Robust.Shared.Console;
using Content.Shared.SS220.Experience;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Fun)]
public sealed class ExperienceEditorCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityNetworkManager _entityNetwork = default!;

    public override string Command => "expeditor";

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } player)
        {
            shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
            return;
        }

        switch (args.Length)
        {
            case 0:
                _entityNetwork.SendSystemNetworkMessage(new OpenExperienceEditorRequest(), player.Channel);
                break;

            case 1:

                if (!int.TryParse(args[0], out var entInt))
                {
                    shell.WriteLine(Loc.GetString("shell-entity-uid-must-be-number"));
                    return;
                }

                var nent = new NetEntity(entInt);
                _entityNetwork.SendSystemNetworkMessage(new OpenExperienceEditorRequest(nent), player.Channel);
                break;

            default:
                shell.WriteLine(Loc.GetString("cmd-expeditor-invalid-arguments"));
                shell.WriteLine(Help);
                return;
        }
    }
}
