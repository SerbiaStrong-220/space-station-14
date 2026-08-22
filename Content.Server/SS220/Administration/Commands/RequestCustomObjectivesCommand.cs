using Content.Server.Administration;
using Content.Server.SS220.Administration.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.SS220.Administration.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed partial class RequestCustomObjectivesCommand : IConsoleCommand
{
    [Dependency] private IEntityManager _entityManager = default!;

    public string Command => "requestcustomobjectives";
    public string Description => "Sends the current custom objectives player list to the requesting admin.";
    public string Help => "Usage: requestcustomobjectives";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player == null)
        {
            shell.WriteLine("This does not work from the server console.");
            return;
        }

        var system = _entityManager.System<CustomObjectivesAdminSystem>();
        system.SendCustomObjectivesList(shell.Player);
    }
}
