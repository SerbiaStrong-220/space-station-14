// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Administration;
using Content.Server.SS220.Zones.Systems;
using Content.Shared.Administration;
using Content.Shared.SS220.Zones;
using Content.Shared.SS220.Zones.Systems;
using Robust.Shared.Console;

namespace Content.Server.SS220.Zones.Commands;

[AdminCommand(AdminFlags.Mapping)]
public sealed partial class CreateZoneCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override string Command => $"{SharedZonesSystem.ZoneCommandsPrefix}create";

    public override string Description => Loc.GetString("zone-commnad-create-zone-desc");

    public override string Help => $"{Command} {{zone params}}";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1)
            return;

        var @params = new ZoneParams();
        @params.ParseTags(argStr);

        var zonesSystem = _entityManager.System<ZonesSystem>();
        var (zone, failReason) = zonesSystem.CreateZone(@params);
        if (zone == null)
        {
            failReason ??= "Error creating a new zone";
            shell.WriteError(failReason);
        }
    }

    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        var result = CompletionResult.Empty;
        if (args.Length <= 0)
            return result;

        result = CompletionResult.FromHint(Loc.GetString("zone-command-params-types-array"));
        return result;
    }
}
