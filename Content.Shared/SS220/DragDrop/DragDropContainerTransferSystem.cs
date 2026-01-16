using Content.Shared.DragDrop;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Whitelist;

namespace Content.Shared.SS220.DragDrop;

public sealed partial class DragDropContainerTransferSystem : EntitySystem
{
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<DragDropContainerTransferComponent, CanDragEvent>(OnCanDrag);
        SubscribeLocalEvent<DragDropContainerTransferComponent, CanDropDraggedEvent>(OnCanDropDrag);
        SubscribeLocalEvent<DragDropContainerTransferComponent, DragDropDraggedEvent>(OnDragDropDragged);
    }

    private void OnCanDrag(Entity<DragDropContainerTransferComponent> ent, ref CanDragEvent args)
    {
        args.Handled = true;
    }

    private void OnCanDropDrag(Entity<DragDropContainerTransferComponent> ent, ref CanDropDraggedEvent args)
    {
        if (!TryComp<StorageComponent>(ent, out var storage) || storage.Container.Count == 0)
            return;

        if (!HasComp<StorageComponent>(args.Target))
            return;

        if (_whitelist.IsWhitelistFail(ent.Comp.Whitelist, args.Target) ||
            _whitelist.IsBlacklistPass(ent.Comp.Blacklist, args.Target))
        {
            return;
        }

        args.Handled = true;
        args.CanDrop = true;
    }

    private void OnDragDropDragged(Entity<DragDropContainerTransferComponent> ent, ref DragDropDraggedEvent args)
    {
        if (!TryComp<StorageComponent>(args.Target, out var targetStorage))
            return;

        if (!TryComp<StorageComponent>(ent, out var entityStorage))
            return;

        _storage.TransferEntities(ent, args.Target, args.User, sourceComp: entityStorage, targetComp: targetStorage);
    }
}
