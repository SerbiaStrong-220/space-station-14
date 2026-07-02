// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.SS220.Pathology;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Pathology.Commands;

[AdminCommand(AdminFlags.Debug)]
public sealed class AddVirusCommand : IConsoleCommand
{
    [Dependency] private EntityManager _entityManager = default!;
    [Dependency] private IPrototypeManager _prototype = default!;

    public string Command => "addvirus";
    public string Description => "Adds a virus (composed of symptoms) to the target entity.";
    public string Help => "addvirus <targetNetEntity> <virusProtoId>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteLine("Expected 2 arguments");
            return;
        }

        if (!NetEntity.TryParse(args[0], out var netEntity) || !_entityManager.TryGetEntity(netEntity, out var target))
        {
            shell.WriteLine($"Can't resolve entity {args[0]}");
            return;
        }

        if (!_prototype.HasIndex<VirusPrototype>(args[1]))
        {
            shell.WriteLine($"{nameof(VirusPrototype)} with id {args[1]} doesn't exist");
            return;
        }

        var pathology = _entityManager.System<SharedPathologySystem>();
        _entityManager.EnsureComponent<PathologyHolderComponent>(target.Value);

        if (pathology.TryAddVirus(target.Value, args[1], out var id))
            shell.WriteLine($"Added virus {args[1]} to {netEntity} as instance {id}");
        else
            shell.WriteLine("Failed to add virus");
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
