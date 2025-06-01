using Content.Server.Administration;
using Content.Server.SS220.Zones.Systems;
using Content.Shared.Administration;
using Content.Shared.SS220.Zones.Components;
using Content.Shared.SS220.Zones.Systems;
using Robust.Shared.Console;

namespace Content.Server.SS220.Zones.Commands;

[AdminCommand(AdminFlags.Mapping)]
public sealed class ChangeZoneCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override string Command => $"{SharedZonesSystem.ZoneCommandsPrefix}change";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 2)
            return;

        if (!NetEntity.TryParse(args[0], out var netEnt))
            return;

        var zone = _entityManager.GetEntity(netEnt);
        if (!_entityManager.TryGetComponent<ZoneComponent>(zone, out var zoneComp))
            return;

        var @params = new ZoneParamsState();
        @params.ParseTags(argStr);

        var zonesSystem = _entityManager.System<ZonesSystem>();
        zonesSystem.ChangeZone((zone, zoneComp), @params);
    }
}
