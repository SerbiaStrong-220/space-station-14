// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Administration;
using Content.Server.SS220.Zones.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Map;
using System.Linq;

namespace Content.Server.SS220.Zones.Commands;

[AdminCommand(AdminFlags.Mapping)]
public sealed partial class CreateZoneCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override string Command => "zone:create";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 3)
            return;

        if (!EntityUid.TryParse(args[0], out var parent))
            return;

        var strPoint1 = args[1].Split(';').Select(s => s.Trim()).ToArray();
        var strPoint2 = args[2].Split(';').Select(s => s.Trim()).ToArray(); ;
        if (strPoint1.Length != 2 ||
            strPoint2.Length != 2)
            return;

        if (!float.TryParse(strPoint1[0], out var x1) ||
            !float.TryParse(strPoint1[1], out var y1) ||
            !float.TryParse(strPoint2[0], out var x2) ||
            !float.TryParse(strPoint2[1], out var y2))
            return;

        var point1 = new EntityCoordinates(parent, x1, y1);
        var point2 = new EntityCoordinates(parent, x2, y2);

        var zonesSystem = _entityManager.System<ZonesSystem>();
        zonesSystem.CreateZone(parent, [(point1, point2)], true);
    }
}
