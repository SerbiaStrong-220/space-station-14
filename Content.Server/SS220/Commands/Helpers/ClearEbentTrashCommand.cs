// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Tag;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Console;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Commands.Helpers;

[AdminCommand(AdminFlags.Admin)]
public sealed partial class ClearEbentTrashCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entMan = default!;

    private static readonly ProtoId<TagPrototype> TrashTag = "Trash";
    private const string MagazineTag = "Magazine";

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
        var containerSystem = _entMan.System<SharedContainerSystem>();
        var tag = _entMan.System<TagSystem>();
        var processed = 0;
        foreach (var ent in _entMan.GetEntities())
        {
            if (_entMan.TryGetComponent<TagComponent>(ent, out var tagComp))
            {
                if ((tag.HasTag(ent, TrashTag) || tagComp.Tags.Any(tagName => tagName.Id.Contains(MagazineTag))) &&
                    !containerSystem.IsEntityOrParentInContainer(ent))
                {
                    _entMan.DeleteEntity(ent);
                    processed++;
                    continue;
                }
            }

            if (!_entMan.TryGetComponent(ent, out MetaDataComponent? comp))
                continue;

            if (containerSystem.IsEntityOrParentInContainer(ent) ||
                (comp.EntityPrototype?.ID != "MaterialWoodPlank1" && comp.EntityPrototype?.ID != "SheetSteel1"))
                continue;

            _entMan.DeleteEntity(ent);
            processed++;
        }

        shell.WriteLine($"Удалено {processed} мусора и магазинов.");
    }

    private void ClearAmmo(IConsoleShell shell)
    {
        var processed = 0;
        var containerSystem = _entMan.System<SharedContainerSystem>();
        var query = _entMan.AllEntityQueryEnumerator<CartridgeAmmoComponent>();
        while (query.MoveNext(out var entity, out _))
        {
            if (containerSystem.IsEntityOrParentInContainer(entity))
                continue;

            _entMan.QueueDeleteEntity(entity);
            processed++;
        }

        shell.WriteLine($"Удалено {processed} патронов.");
    }
}
