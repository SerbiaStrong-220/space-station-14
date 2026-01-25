// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Administration;
using Content.Server.SS220.Zone.Systems;
using Content.Shared.Administration;
using Content.Shared.SS220.Zone.Systems;
using Content.Shared.SS220.Zone.Components;
using Robust.Shared.Console;

namespace Content.Server.SS220.Zone.Commands;

[AdminCommand(AdminFlags.Mapping)]
public sealed class RecalculateZoneAreaCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override string Command => $"{SharedZoneSystem.ZoneCommandsPrefix}recalculate_area";

    public override string Description => Loc.GetString("cmd-recalculate-zone-area-desc");

    public override string Help => Loc.GetString("cmd-recalculate-zone-area-help");

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("cmd-recalculate-zone-area-invalid-args-count", ("help", Help)));
            return;
        }

        if (!NetEntity.TryParse(args[0], out var netId))
        {
            shell.WriteError(Loc.GetString("cmd-recalculate-zone-area-invalid-argument-0", ("arg", args[0])));
            return;
        }

        if (!_entityManager.TryGetEntity(netId, out var zoneUid))
        {
            shell.WriteError(Loc.GetString("cmd-recalculate-zone-area-invalid-net-entity", ("netId", netId)));
            return;
        }

        if (!_entityManager.TryGetComponent<ZoneComponent>(zoneUid, out var zoneComp))
        {
            shell.WriteError(Loc.GetString("cmd-recalculate-zone-area-invalid-entity", ("ent", _entityManager.ToPrettyString(zoneUid))));
            return;
        }

        var zoneSys = _entityManager.System<ZoneSystem>();
        zoneSys.OptimizeZoneArea((zoneUid.Value, zoneComp));
        shell.WriteLine(Loc.GetString("cmd-recalculate-zone-area-success", ("zone", _entityManager.ToPrettyString(zoneUid))));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        var result = CompletionResult.Empty;
        if (args.Length == 1)
            result = CompletionResult.FromHintOptions(
                _entityManager.System<ZoneSystem>().GetZonesListCompletionOption(),
                Loc.GetString("cmd-recalculate-zone-area-hint-1"));

        return result;
    }
}
