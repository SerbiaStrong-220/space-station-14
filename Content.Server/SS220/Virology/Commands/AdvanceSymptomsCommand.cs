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
    public string Description => "Force-advances every active pathology and virus symptom in the target by one stage (run again to push further).";
    public string Help => "advancesymptoms <targetNetEntity>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteLine("Expected 1 argument");
            return;
        }

        if (!NetEntity.TryParse(args[0], out var netEntity) || !_entityManager.TryGetEntity(netEntity, out var target))
        {
            shell.WriteLine($"Can't resolve entity {args[0]}");
            return;
        }

        var advanced = 0;

        if (_entityManager.HasComponent<PathologyHolderComponent>(target.Value))
            advanced += _entityManager.System<SharedPathologySystem>().ForceAdvanceAllPathologies(target.Value);

        advanced += _entityManager.System<VirologySystem>().ForceAdvanceAllSymptoms(target.Value);

        shell.WriteLine($"Advanced {advanced} symptom(s) on {netEntity}");
    }
}
