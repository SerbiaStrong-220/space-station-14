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

        var virology = _entityManager.System<VirologySystem>();

        if (virology.AddVirus(target.Value, args[1]))
            shell.WriteLine($"Added virus {args[1]} to {netEntity}");
        else
            shell.WriteLine("Failed to add virus (not susceptible, immune, or already infected)");
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
