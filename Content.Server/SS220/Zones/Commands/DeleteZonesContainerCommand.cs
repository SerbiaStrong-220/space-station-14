// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
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

    public override string Description => Loc.GetString("zone-commnad-delete-zones-container-desc");

    public override string Help => $"{Command} {{zones container uid}}";

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

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        var result = CompletionResult.Empty;
        if (args.Length == 1)
            result = CompletionResult.FromHintOptions(GetZonesContainersList(), Loc.GetString("zone-command-delete-zones-container-uid-hint"));

        return result;

        List<CompletionOption> GetZonesContainersList()
        {
            var result = new List<CompletionOption>();
            var query = _entityManager.AllEntityQueryEnumerator<ZonesContainerComponent>();
            while (query.MoveNext(out var uid, out var zoneComp))
            {
                var option = new CompletionOption(_entityManager.GetNetEntity(uid).ToString());
                if (_entityManager.TryGetComponent<MetaDataComponent>(uid, out var metadataComp))
                    option.Hint = metadataComp.EntityName;

                result.Add(option);
            }

            return result;
        }
    }
}
