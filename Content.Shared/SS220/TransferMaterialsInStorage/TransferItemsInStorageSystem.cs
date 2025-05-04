using System.Linq;
using Content.Shared.Interaction;
using Content.Shared.Materials;
using Content.Shared.Storage;

namespace Content.Shared.SS220.TransferMaterialsInStorage;

public sealed class TransferMaterialsInStorageSystem : EntitySystem
{
    [Dependency] private readonly SharedMaterialStorageSystem _material = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<TransferMaterialsInStorageComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnAfterInteract(Entity<TransferMaterialsInStorageComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target == null)
            return;

        if (!HasComp<MaterialStorageComponent>(args.Target.Value))
            return;

        if (!TryComp<StorageComponent>(ent.Owner, out var storageComponent))
            return;

        var items = storageComponent.Container.ContainedEntities.ToList();

        foreach (var item in items)
        {
            if (_material.TryInsertMaterialEntity(args.User, item, args.Target.Value))
                continue;

            RaiseLocalEvent(args.Target.Value, new AfterInteractUsingEvent(args.User, item, args.Target.Value, Transform(args.Target.Value).Coordinates, true));
        }
    }
}
