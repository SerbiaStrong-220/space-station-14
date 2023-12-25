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
        [Dependency] private readonly SharedContainerSystem _containerSystem = default!;

        public string Command => "garbage_clear_up";
        public string Description => "Removes all objects with a tag 'trash' from the map";
        public string Help => "Surgery tommorow";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            int cnt = 0;
            foreach (var ent in _entMan.GetEntities())
            {
                if (!_entMan.TryGetComponent<TagComponent>(ent, out var component))
                    continue;
                if (!component.Tags.Contains("Trash"))
                    continue;
                if (_containerSystem.IsEntityOrParentInContainer(ent))
                    continue;

                _entMan.DeleteEntity(ent);
                cnt++;
            }
            shell.WriteLine($"Карта очищена от {cnt} объектов мусора!");
        }
    }
}