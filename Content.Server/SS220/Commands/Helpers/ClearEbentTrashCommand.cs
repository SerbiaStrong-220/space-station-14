// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Tag;
using Robust.Shared.Console;
using Robust.Shared.Containers;
using Content.Shared.Weapons.Ranged.Components;
using System.Linq;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Admin)]
    public sealed partial class ClearEbentTrashCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        //SS220-clearebenttrash
        public string Command => "clearebenttrash";
        public string Description => "Удаляет вcё с тегом trash, патроны, магазины, сталь и дерево (одиночные)";
        public string Help => $"Usage: {Command}";

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            ClearTrash(shell);
            ClearAmmo(shell);
        }
        private void ClearTrash(IConsoleShell shell)
        {
            var _containerSystem = _entMan.System<SharedContainerSystem>();
            var _tag = _entMan.System<TagSystem>();
            int processed = 0;
            foreach (var ent in _entMan.GetEntities())
            {

                if (_entMan.TryGetComponent<TagComponent>(ent, out var component))
                {
                    if (_tag.HasTag(ent, "Trash") && !_containerSystem.IsEntityOrParentInContainer(ent))
                    {
                        _entMan.DeleteEntity(ent);
                        processed++;
                        continue;
                    }
                    else if (!_containerSystem.IsEntityOrParentInContainer(ent) && _tag.HasTag(ent, "Magazine"))
                    {
                        _entMan.DeleteEntity(ent);
                        processed++;
                        continue;
                    }
                }

                if (_entMan.TryGetComponent(ent, out MetaDataComponent? comp))
                {
                    if (!_containerSystem.IsEntityOrParentInContainer(ent) && (comp.EntityPrototype?.ID == "MaterialWoodPlank1" || comp.EntityPrototype?.ID == "SheetSteel1"))
                    {
                        _entMan.DeleteEntity(ent);
                        processed++;
                    }
                }
            }
            shell.WriteLine($"Удалено {processed} мусора и магазинов.");
        }
        public void ClearAmmo(IConsoleShell shell)
        {
            var processed = 0;
            var _containerSystem = _entMan.System<SharedContainerSystem>();
            var query = _entMan.AllEntityQueryEnumerator<CartridgeAmmoComponent>();
            while (query.MoveNext(out var entity, out var comp))
            {
                if (!_containerSystem.IsEntityOrParentInContainer(entity))
                    _entMan.QueueDeleteEntity(entity);
                processed++;
            }

            shell.WriteLine($"Удалено {processed} патронов.");
        }
    }
}
