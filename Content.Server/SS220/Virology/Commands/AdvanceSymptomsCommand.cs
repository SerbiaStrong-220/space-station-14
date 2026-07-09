// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.SS220.Pathology;
using Robust.Shared.Console;

namespace Content.Server.SS220.Virology.Commands;

[AdminCommand(AdminFlags.Debug)]
public sealed partial class AdvanceSymptomsCommand : IConsoleCommand
{
    [Dependency] private EntityManager _entityManager = default!;

    public string Command => "advancesymptoms";
    public string Description => Loc.GetString("advancesymptoms-command-description");
    public string Help => Loc.GetString("advancesymptoms-command-help");

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteLine(Loc.GetString("shell-wrong-arguments-number-need-specific",
                ("properAmount", 1), ("currentAmount", args.Length)));
            return;
        }

        if (!NetEntity.TryParse(args[0], out var netEntity) || !_entityManager.TryGetEntity(netEntity, out var target))
        {
            shell.WriteLine(Loc.GetString("advancesymptoms-command-cant-resolve", ("entity", args[0])));
            return;
        }

        var advanced = 0;

        if (_entityManager.HasComponent<PathologyHolderComponent>(target.Value))
            advanced += _entityManager.System<SharedPathologySystem>().ForceAdvanceAllPathologies(target.Value);

        advanced += _entityManager.System<VirologySystem>().ForceAdvanceAllSymptoms(target.Value);

        shell.WriteLine(Loc.GetString("advancesymptoms-command-advanced", ("count", advanced), ("target", netEntity)));
    }
}
