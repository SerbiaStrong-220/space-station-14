using System.Linq;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Content.Shared.Interaction;
using Content.Shared.Kitchen;
using Content.Shared.Materials;
using Content.Shared.Storage;
using Content.Shared.Tag;
using Content.Shared.Kitchen.Components;

namespace Content.Shared.SS220.TransferMaterialsInStorage;

public sealed class TransferMaterialsInStorageSystem : EntitySystem
{
    [Dependency] private readonly SharedMaterialStorageSystem _material = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    private static readonly ProtoId<TagPrototype> ReagentGrinderTag = "ReagentGrinder";

    public override void Initialize()
    {
        SubscribeLocalEvent<TransferMaterialsInStorageComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnAfterInteract(Entity<TransferMaterialsInStorageComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target == null)
            return;

        if (!TryComp<StorageComponent>(ent.Owner, out var storageComponent))
            return;

        var target = args.Target.Value;

        var isMaterialStorage = HasComp<MaterialStorageComponent>(target);
        var isGrinder = _tag.HasTag(target, ReagentGrinderTag) && _container.TryGetContainer(target, ReagentGrinderComponent.InputContainerId, out _);

        if (!isMaterialStorage && !isGrinder)
            return;

        args.Handled = true;

        var items = storageComponent.Container.ContainedEntities.ToList();
        var coords = Transform(target).Coordinates;

        foreach (var item in items)
        {
            bool inserted;

            if (isMaterialStorage)
                inserted = _material.TryInsertMaterialEntity(args.User, item, target);
            else
            {
                var ev = new InteractUsingEvent(args.User, item, target, coords);
                RaiseLocalEvent(target, ev);
                continue;
            }

            if (inserted)
                continue;

            RaiseLocalEvent(target, new AfterInteractUsingEvent(args.User, item, target, coords, true));
        }
    }
}
