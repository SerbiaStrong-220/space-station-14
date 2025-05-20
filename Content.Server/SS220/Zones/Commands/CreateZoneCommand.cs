// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Administration;
using Content.Server.SS220.Zones.Systems;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Map;

namespace Content.Server.SS220.Zones.Commands;

[AdminCommand(AdminFlags.Mapping)]
public sealed partial class CreateZoneCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override string Command => $"{ZonesSystem.ZoneCommandsPrefix}create";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
            return;

        if (!EntityUid.TryParse(args[0], out var parent))
            return;

        var coords = new List<(EntityCoordinates, EntityCoordinates)>();
        var pairs = args[1].Split(';');
        for (var i = 0; i < pairs.Length; i++)
        {
            var cur = pairs[i];
            var num = cur.Split(' ');
            if (num.Length > 4)
            {
                shell.WriteLine($"Неверное количество аргументов в координатах бокса {i}");
                return;
            }

            if (!float.TryParse(num[0], out var x1) ||
                !float.TryParse(num[1], out var y1) ||
                !float.TryParse(num[2], out var x2) ||
                !float.TryParse(num[3], out var y2))
            {
                shell.WriteLine($"Не удалось получить координаты бокса {i}");
                return;
            }

            var point1 = new EntityCoordinates(parent, x1, y1);
            var point2 = new EntityCoordinates(parent, x2, y2);
            coords.Add((point1, point2));
        }

        var zonesSystem = _entityManager.System<ZonesSystem>();
        zonesSystem.CreateZone(parent, coords, true);
    }
}
