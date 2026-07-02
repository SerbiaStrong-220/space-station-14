// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.SS220.Pathology;
using Robust.Shared.Console;

namespace Content.Server.SS220.Pathology.Commands;

[AdminCommand(AdminFlags.Debug)]
public sealed class AdvanceSymptomsCommand : IConsoleCommand
{
    [Dependency] private EntityManager _entityManager = default!;

    public string Command => "advancesymptoms";
    public string Description => "Force-advances every active symptom in the target by one stage (run again to push further).";
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

        if (!_entityManager.HasComponent<PathologyHolderComponent>(target.Value))
        {
            shell.WriteLine($"{netEntity} has no active pathologies");
            return;
        }

        var pathology = _entityManager.System<SharedPathologySystem>();
        var advanced = pathology.ForceAdvanceAllPathologies(target.Value);
        shell.WriteLine($"Advanced {advanced} symptom(s) on {netEntity}");
    }
}
