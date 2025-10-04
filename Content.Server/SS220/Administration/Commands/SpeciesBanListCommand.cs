// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Administration;
using Content.Server.Administration.BanList;
using Content.Server.Database;
using Content.Server.EUI;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server.SS220.Administration.Commands;

[AdminCommand(AdminFlags.Ban)]
public sealed class SpeciesBanListCommand : LocalizedCommands
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly EuiManager _eui = default!;
    [Dependency] private readonly IPlayerLocator _locator = default!;

    public override string Command => "speciesbanlist";
    public override string Description => Loc.GetString("cmd-speciesbanlist-desc");
    public override string Help => Loc.GetString("cmd-speciesbanlist-help");

    public override async void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        string target;
        var includeUnbanned = true;
        switch (args.Length)
        {
            case 1:
                target = args[0];
                break;
            case 2:
                target = args[0];

                if (!bool.TryParse(args[1], out includeUnbanned))
                {
                    shell.WriteLine(Loc.GetString("cmd-speciesbanlist-invalid-argument-2", ("help", Help)));
                    return;
                }
                break;
            default:
                shell.WriteLine(Loc.GetString("cmd-speciesbanlist-invalid-arguments-amount", ("help", Help)));
                return;
        }

        var data = await _locator.LookupIdByNameOrIdAsync(target);
        if (data is null)
        {
            shell.WriteLine(Loc.GetString("cmd-speciesbanlist-invalid-player", ("player", target)));
            return;
        }

        if (shell.Player is not { } player)
        {
            var bans = await _db.GetServerSpeciesBansAsync(data.LastAddress, data.UserId, data.LastLegacyHWId, data.LastModernHWIds, includeUnbanned);
            if (bans.Count == 0)
            {
                shell.WriteLine(Loc.GetString("cmd-speciesbanlist-player-has-not-bans", ("player", data.Username)));
                return;
            }

            foreach (var ban in bans)
            {
                shell.WriteLine(Loc.GetString("cmd-speciesbanlist-ban-info",
                    ("id", ban.Id?.ToString() ?? "null"),
                    ("species", ban.SpeciesId),
                    ("reason", ban.Reason),
                    ("unbanned", ban.Unban != null)));
            }

            return;
        }

        var ui = new BanListEui();
        _eui.OpenEui(ui, player);
        await ui.ChangeBanListPlayer(data.UserId);
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(CompletionHelper.SessionNames(),
                Loc.GetString("cmd-speciesbanlist-hint-1")),
            2 => CompletionResult.FromHintOptions(CompletionHelper.Booleans,
                Loc.GetString("cmd-speciesbanlist-hint-2")),
            _ => CompletionResult.Empty
        };
    }
}
