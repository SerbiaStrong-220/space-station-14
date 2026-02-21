// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Administration;
using Content.Server.SS220.Zone.Systems;
using Content.Shared.Administration;
using Content.Shared.SS220.Zone.Systems;
using Content.Shared.SS220.Zone.Components;
using Robust.Shared.Console;

namespace Content.Server.SS220.Zone.Commands;

[AdminCommand(AdminFlags.Mapping)]
public sealed class DeleteZoneCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override string Command => $"{SharedZoneSystem.ZoneCommandsPrefix}delete";

    public override string Description => Loc.GetString("cmd-delete-zone-desc");

    public override string Help => Loc.GetString("cmd-delete-zone-help");

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString("cmd-delete-zone-invalid-args-count", ("help", Help)));
            return;
        }

        if (!NetEntity.TryParse(args[0], out var netId))
        {
            shell.WriteError(Loc.GetString("cmd-delete-zone-invalid-argument-0", ("arg", args[0])));
            return;
        }

        if (!_entityManager.TryGetEntity(netId, out var zoneUid))
        {
            shell.WriteError(Loc.GetString("cmd-delete-zone-invalid-net-entity", ("netId", netId)));
            return;
        }

        if (!_entityManager.TryGetComponent<ZoneComponent>(zoneUid, out var zoneComp))
        {
            shell.WriteError(Loc.GetString("cmd-delete-zone-invalid-entity", ("ent", _entityManager.ToPrettyString(zoneUid))));
            return;
        }

        var zoneSys = _entityManager.System<ZoneSystem>();
        zoneSys.DeleteZone((zoneUid.Value, zoneComp));
        shell.WriteLine(Loc.GetString("cmd-delete-zone-success", ("zone", _entityManager.ToPrettyString(zoneUid))));
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        var result = CompletionResult.Empty;
        if (args.Length == 1)
            result = CompletionResult.FromHintOptions(
                _entityManager.System<ZoneSystem>().GetZonesListCompletionOption(),
                Loc.GetString("cmd-delete-zone-hint-1"));

        return result;
    }
}
