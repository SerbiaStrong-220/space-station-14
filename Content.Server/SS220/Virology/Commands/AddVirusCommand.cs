// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.SS220.Virology;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Virology.Commands;

[AdminCommand(AdminFlags.Debug)]
public sealed partial class AddVirusCommand : IConsoleCommand
{
    [Dependency] private EntityManager _entityManager = default!;
    [Dependency] private IPrototypeManager _prototype = default!;

    public string Command => "addvirus";
    public string Description => Loc.GetString("addvirus-command-description");
    public string Help => Loc.GetString("addvirus-command-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteLine(Loc.GetString("shell-wrong-arguments-number-need-specific",
                ("properAmount", 2), ("currentAmount", args.Length)));
            return;
        }

        if (!NetEntity.TryParse(args[0], out var netEntity) || !_entityManager.TryGetEntity(netEntity, out var target))
        {
            shell.WriteLine(Loc.GetString("addvirus-command-cant-resolve", ("entity", args[0])));
            return;
        }

        if (!_prototype.HasIndex<VirusPrototype>(args[1]))
        {
            shell.WriteLine(Loc.GetString("addvirus-command-no-prototype", ("id", args[1])));
            return;
        }

        var virology = _entityManager.System<VirologySystem>();

        if (virology.AddVirus(target.Value, args[1]))
            shell.WriteLine(Loc.GetString("addvirus-command-added", ("virus", args[1]), ("target", netEntity)));
        else
            shell.WriteLine(Loc.GetString("addvirus-command-failed"));
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length != 2)
            return CompletionResult.Empty;

        var options = _prototype.EnumeratePrototypes<VirusPrototype>()
            .OrderBy(p => p.ID)
            .Select(p => p.ID);

        return CompletionResult.FromHintOptions(options, "<virus>");
    }
}
