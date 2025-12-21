// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.SS220.Administration.Commands;

[AdminCommand(AdminFlags.Ban)]
public sealed class SpeciesUnbanCommand : LocalizedCommands
{
    [Dependency] private readonly IBanManager _ban = default!;

    public override string Command => "speciesunban";
    public override string Description => Loc.GetString("cmd-speciesunban-desc");
    public override string Help => Loc.GetString("cmd-speciesunban-help");

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteLine(Help);
            return;
        }

        if (!int.TryParse(args[0], out var banId))
        {
            shell.WriteLine(Loc.GetString($"cmd-speciesunban-unable-to-parse-id", ("id", args[0]), ("help", Help)));
            return;
        }

        var response = await _ban.PardonSpeciesBan(banId, shell.Player?.UserId, DateTimeOffset.Now);
        shell.WriteLine(response);
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHint(Loc.GetString("cmd-speciesunban-hint-1")),
            _ => CompletionResult.Empty
        };
    }
}
