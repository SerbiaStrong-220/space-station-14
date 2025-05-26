// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Administration;
using Content.Server.SS220.Zones.Systems;
using Content.Shared.Administration;
using Content.Shared.SS220.Zones.Systems;
using Robust.Shared.Console;
using Robust.Shared.Map;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using System.Numerics;

namespace Content.Server.SS220.Zones.Commands;

[AdminCommand(AdminFlags.Mapping)]
public sealed partial class CreateZoneCommand : LocalizedCommands
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override string Command => $"{ZonesSystem.ZoneCommandsPrefix}create";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 2)
            return;

        if (!EntityUid.TryParse(args[0], out var container))
            return;

        var @params = new ZoneParamsState()
        {
            Container = _entityManager.GetNetEntity(container)
        };
        var pairs = args[1].Split(';');
        pairs = pairs.Select(x => x.Replace("(", string.Empty).Replace(")", string.Empty).Trim()).ToArray();
        for (var i = 0; i < pairs.Length; i++)
        {
            var num = pairs[i].Split(' ');
            if (num.Length > 4)
            {
                shell.WriteLine($"Неверное количество аргументов в координатах бокса {i + 1}");
                return;
            }

            num = num.Select(x => x.Trim()).ToArray();
            if (!float.TryParse(num[0], out var x1) ||
                !float.TryParse(num[1], out var y1) ||
                !float.TryParse(num[2], out var x2) ||
                !float.TryParse(num[3], out var y2))
            {
                shell.WriteLine($"Не удалось получить координаты бокса {i}");
                return;
            }

            var box = Box2.FromTwoPoints(new Vector2(x1, y1), new Vector2(x2, y2));
            @params.Boxes.Add(box);
        }
        @params.ParseOptionalTags(argStr);

        var zonesSystem = _entityManager.System<ZonesSystem>();
        zonesSystem.CreateZone(@params);
    }
}
