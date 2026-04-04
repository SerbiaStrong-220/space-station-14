using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.SS220.TraitorDynamics;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.TraitorDynamics;

[AdminCommand(AdminFlags.Round)]
public sealed partial class ForceDynamicCommand : IConsoleCommand
{
    [Dependency] private readonly EntityManager _entityManager = default!;

    public string Command => "setdynamic";
    public string Description => "force dynamic for current round";
    public string Help => "dynamic <dynamicProtoId>";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteLine("Expected 1 argument");
            return;
        }

        var proto = IoCManager.Resolve<IPrototypeManager>();
        if (!proto.HasIndex<DynamicPrototype>(args[0]))
        {
            shell.WriteLine($"{nameof(DynamicPrototype)} with id {args[0]} doesn't exist");
            return;
        }

        var dynamic = _entityManager.System<TraitorDynamicsSystem>();
        dynamic.SetDynamic(args[0]);
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length != 1)
            return CompletionResult.Empty;

        var options = IoCManager.Resolve<IPrototypeManager>()
            .EnumeratePrototypes<DynamicPrototype>()
            .OrderBy(p => p.ID)
            .Select(p => p.ID);

        return CompletionResult.FromHintOptions(options, "<dynamic>");

    }
}
