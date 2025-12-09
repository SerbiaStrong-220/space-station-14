// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Administration;
using Robust.Shared.Console;
using Content.Shared.SS220.Experience;

namespace Content.Server.Administration.Commands;

[AdminCommand(AdminFlags.Fun)]
public sealed class ExperienceRedactorCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityNetworkManager _entityNetwork = default!;

    public override string Command => "experienceredactor";

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
                _entityNetwork.SendSystemNetworkMessage(new OpenExperienceRedactorRequest(), player.Channel);
                break;

            case 1:

                if (!int.TryParse(args[0], out var entInt))
                {
                    shell.WriteLine(Loc.GetString("shell-entity-uid-must-be-number"));
                    return;
                }

                var nent = new NetEntity(entInt);
                _entityNetwork.SendSystemNetworkMessage(new OpenExperienceRedactorRequest(nent), player.Channel);
                break;

            default:
                shell.WriteLine(Loc.GetString("cmd-experience-redactor-invalid-arguments"));
                // TODO: "cmd-{Command}-help"
                shell.WriteLine(Help);
                return;
        }
    }
}
