// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Administration;
using Content.Server.SS220.Zones.Systems;
using Content.Shared.Administration;
using Content.Shared.SS220.Zones.Components;
using Robust.Shared.Console;

namespace Content.Server.SS220.Zones.Commands;

[AdminCommand(AdminFlags.Mapping)]
public sealed partial class RecalculateZoneBoxesCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override string Command => $"{ZonesSystem.ZoneCommandsPrefix}recalc_boxes";

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
        zoneSys.RecalculateZoneBoxes((zoneUid, zoneComp));
    }
}
