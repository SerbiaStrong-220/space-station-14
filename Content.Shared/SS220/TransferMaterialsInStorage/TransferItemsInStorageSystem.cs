using System.Linq;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Content.Shared.Interaction;
using Content.Shared.Materials;
using Content.Shared.Storage;
using Content.Shared.Tag;

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

        var items = storageComponent.Container.ContainedEntities.ToList();

        if (HasComp<MaterialStorageComponent>(args.Target.Value))
        {
            foreach (var item in items)
            {
                if (_material.TryInsertMaterialEntity(args.User, item, args.Target.Value))
                    continue;

                RaiseLocalEvent(args.Target.Value, new AfterInteractUsingEvent(args.User, item, args.Target.Value, Transform(args.Target.Value).Coordinates, true));
            }
        }
        else if (TryComp<TagComponent>(args.Target.Value, out var tag) &&
                 _tag.HasTag(args.Target.Value, ReagentGrinderTag) &&
                 _container.TryGetContainer(args.Target.Value, "inputContainer", out var container))
        {
            foreach (var item in items)
            {
                if (_container.Insert(item, container))
                    continue;

                RaiseLocalEvent(args.Target.Value, new AfterInteractUsingEvent(args.User, item, args.Target.Value, Transform(args.Target.Value).Coordinates, true));
            }
        }
    }
}
