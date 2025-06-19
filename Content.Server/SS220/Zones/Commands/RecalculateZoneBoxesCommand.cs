// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Administration;
using Content.Server.SS220.Zones.Systems;
using Content.Shared.Administration;
using Content.Shared.SS220.Zones.Components;
using Content.Shared.SS220.Zones.Systems;
using Robust.Shared.Console;

namespace Content.Server.SS220.Zones.Commands;

[AdminCommand(AdminFlags.Mapping)]
public sealed partial class RecalculateZoneBoxesCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override string Command => $"{SharedZonesSystem.ZoneCommandsPrefix}recalc_regions";

    public override string Description => Loc.GetString("zone-commnad-recalc-regions-container-desc");

    public override string Help => $"{Command} {{zone uid}}";

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
        zoneSys.RecalculateZoneRegions((zoneUid, zoneComp));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        var result = CompletionResult.Empty;
        if (args.Length == 1)
            result = CompletionResult.FromHintOptions(GetZonesList(), Loc.GetString("zone-command-recalc-regions-uid-hint"));

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
