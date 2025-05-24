
using Content.Server.Administration;
using Robust.Shared.Console;
using Content.Shared.Administration;
using Content.Shared.SS220.Zones.Systems;
using Content.Shared.SS220.Zones.Components;
using Content.Server.SS220.Zones.Systems;

namespace Content.Server.SS220.Zones.Commands;

[AdminCommand(AdminFlags.Mapping)]
public sealed partial class DeleteZonesContainerCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override string Command => SharedZonesSystem.ZoneCommandsPrefix + "delete_container";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
            return;

        if (!NetEntity.TryParse(args[0], out var netUid))
            return;

        var container = _entityManager.GetEntity(netUid);
        if (!_entityManager.TryGetComponent<ZonesContainerComponent>(container, out var containerComp))
            return;

        var zoneSys = _entityManager.System<ZonesSystem>();
        zoneSys.DeleteZonesContaner((container, containerComp));
    }
}
