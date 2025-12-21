// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Tag;
using Robust.Shared.Console;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Administration.Commands;

[AdminCommand(AdminFlags.Admin)]
public sealed partial class GarbageClearUpCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entMan = default!;

    public string Command => "clearupgarbage";
    public string Description => Loc.GetString("command-garbage-clear-up-desc");
    public string Help => $"Usage: {Command}";

    private static readonly ProtoId<TagPrototype> TrashProto = "Trash";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        var container = _entMan.System<SharedContainerSystem>();

        var count = 0;
        var query = _entMan.EntityQueryEnumerator<TagComponent>();

        while (query.MoveNext(out var uid, out var tag))
        {
            if (!tag.Tags.Contains(TrashProto) || container.IsEntityOrParentInContainer(uid))
                continue;

            _entMan.QueueDeleteEntity(uid);
            count++;
        }

        shell.WriteLine($"Удалено {count} объектов.");
    }
}
