// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
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

    public override string Description => Loc.GetString("zone-command-change-zone-desc");

    public override string Help => $"{Command} {{zone uid}} {{new params}}";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 2)
            return;

        if (!NetEntity.TryParse(args[0], out var netEnt))
            return;

        var zone = _entityManager.GetEntity(netEnt);
        if (!_entityManager.TryGetComponent<ZoneComponent>(zone, out var zoneComp))
            return;

        var @params = new ZoneParams(zoneComp.ZoneParams);
        @params.ParseTags(argStr);

        var zonesSystem = _entityManager.System<ZonesSystem>();
        zonesSystem.ChangeZone((zone, zoneComp), @params);
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        var result = CompletionResult.Empty;
        if (args.Length <= 0)
            return result;

        if (args.Length == 1)
            result = CompletionResult.FromHintOptions(GetZonesList(), Loc.GetString("zone-command-change-zone-uid-hint"));
        else
            result = CompletionResult.FromHint(Loc.GetString("zone-command-params-types-array"));

        return result;

        List<CompletionOption> GetZonesList()
        {
            var result = new List<CompletionOption>();
            var query = _entityManager.AllEntityQueryEnumerator<ZoneComponent>();
            while (query.MoveNext(out var uid, out var zoneComp))
            {
                var option = new CompletionOption(_entityManager.GetNetEntity(uid).ToString());
                option.Hint = zoneComp.ZoneParams.Name;
                result.Add(option);
            }

            return result;
        }
    }
}
