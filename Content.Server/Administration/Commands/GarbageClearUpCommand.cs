// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Administration;
using Content.Shared.Tag;
using Robust.Shared.Console;
using Robust.Shared.Containers;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed partial class GarbageClearUpCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        //SS220-clearupgarbage
        public string Command => "clearupgarbage";
        public string Description => "Удаляет весь мусор с карты (применимо к объектам с тегом 'trash')";
        public string Help => $"Usage: {Command} ... surgery tommorow";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            var _containerSystem = _entMan.System<SharedContainerSystem>();
            var _tag = _entMan.System<TagSystem>();

            int processed = 0;
            foreach (var ent in _entMan.GetEntities())
            {
                if (!_entMan.TryGetComponent<TagComponent>(ent, out var component))
                    continue;
                if (!_tag.HasTag(ent, "Trash") || _containerSystem.IsEntityOrParentInContainer(ent))
                    continue;

                _entMan.DeleteEntity(ent);
                processed++;
            }
            shell.WriteLine($"Удалено {processed} энтити.");
        }
    }
}
