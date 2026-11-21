// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Server.Player;
using Robust.Shared.Console;

namespace Content.Server.SS220.AntagRemoval;

[AdminCommand(AdminFlags.Admin)]
public sealed class RemoveAntagCommand : IConsoleCommand
{
    [Dependency] private IEntityManager _entities = default!;
    [Dependency] private IPlayerManager _players = default!;

    public string Command => "removeantag";
    public string Description => Loc.GetString("ss220-removeantag-command-description");
    public string Help => Loc.GetString("ss220-removeantag-command-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length is < 1 or > 2)
        {
            shell.WriteError(Help);
            return;
        }

        if (!_players.TryGetSessionByUsername(args[0], out var target))
        {
            shell.WriteError(Loc.GetString("ss220-removeantag-error-not-connected"));
            return;
        }

        var sys = _entities.System<RemoveAntagSystem>();
        if (args.Length == 1)
        {
            shell.WriteLine(sys.GetRemovableRoles(target));
            return;
        }

        if (sys.TryRemove(shell.Player, target, args[1], out var message))
            shell.WriteLine(message);
        else
            shell.WriteError(message);
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHintOptions(CompletionHelper.SessionNames(), Help),
            2 => CompletionResult.FromOptions(new[] { "traitor", "thief", "cult", "initialinfected", "headrev" }),
            _ => CompletionResult.Empty
        };
    }
}
