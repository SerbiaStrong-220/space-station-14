using Content.Server.Administration;
using Content.Server.SS220.Zones.Systems;
using Content.Shared.Administration;
using Content.Shared.SS220.Zones.Components;
using Content.Shared.SS220.Zones.Systems;
using Robust.Shared.Console;

namespace Content.Server.SS220.Zones.Commands;

[AdminCommand(AdminFlags.Mapping)]
public sealed partial class DeleteZoneCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override string Command => SharedZonesSystem.ZoneCommandsPrefix + "delete";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
            return;

        if (!NetEntity.TryParse(args[0], out var netUid))
            return;

        var zoneUid = _entityManager.GetEntity(netUid);
        if (!_entityManager.TryGetComponent<ZoneComponent>(zoneUid, out var zoneComp))
            return;

        var zoneSys = _entityManager.System<ZonesSystem>();
        zoneSys.DeleteZone((zoneUid, zoneComp));
    }
}
