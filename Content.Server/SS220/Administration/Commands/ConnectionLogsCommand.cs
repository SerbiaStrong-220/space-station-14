using Content.Server.Administration;
using Content.Server.EUI;
using Content.Server.SS220.Administration.Connections;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.SS220.Administration.Commands;

[AdminCommand(AdminFlags.Logs)]
public sealed class ConnectionLogsCommand : LocalizedCommands
{
    [Dependency] private readonly EuiManager _eui = default!;

    public override string Command => "connectionlogs";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (shell.Player is not { } player)
        {
            shell.WriteError(Loc.GetString("shell-cannot-run-command-from-server"));
            return;
        }

        _eui.OpenEui(new ConnectionLogsEui(), player);
    }
}
