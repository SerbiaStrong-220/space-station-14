using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.SS220.TraitorDynamics;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.TraitorDynamics;

[AdminCommand(AdminFlags.Round)]
public sealed partial class GetDynamicCommand : IConsoleCommand
{
    [Dependency] private readonly EntityManager _entityManager = default!;
    public string Command => "getcurrentdynamic";
    public string Description => "get traitor dynamic for current traitor rule";
    public string Help => "getcurrentdynamic";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var traitorDynamic = _entityManager.System<TraitorDynamicsSystem>();
        var proto = IoCManager.Resolve<IPrototypeManager>();

        var currentDynamic = traitorDynamic.GetCurrentDynamic();
        if (!proto.TryIndex(currentDynamic, out var dynamicProto))
        {
            shell.WriteLine("Can't find any dynamic!");
            return;
        }

        shell.WriteLine($"Current traitor dynamic: {dynamicProto.ID}");
    }
}
