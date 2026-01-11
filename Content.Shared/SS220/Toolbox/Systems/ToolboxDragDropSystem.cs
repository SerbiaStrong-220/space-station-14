using Content.Shared.DragDrop;
using Content.Shared.SS220.Toolbox.Components;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Whitelist;

namespace Content.Shared.SS220.Toolbox.Systems;

public sealed partial class ToolboxDragDropSystem : EntitySystem
{
    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ToolboxComponent, CanDragEvent>(OnCanDrag);
        SubscribeLocalEvent<ToolboxComponent, CanDropDraggedEvent>(OnCanDropDrag);
        SubscribeLocalEvent<ToolboxComponent, DragDropDraggedEvent>(OnDragDropDragged);
    }

    private void OnCanDrag(Entity<ToolboxComponent> ent, ref CanDragEvent args)
    {
        args.Handled = true;
    }

    private void OnCanDropDrag(Entity<ToolboxComponent> ent, ref CanDropDraggedEvent args)
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

    private void OnDragDropDragged(Entity<ToolboxComponent> ent, ref DragDropDraggedEvent args)
    {
        if (!TryComp<StorageComponent>(args.Target, out var targetStorage))
            return;

        if (!TryComp<StorageComponent>(ent, out var toolboxStorage))
            return;

        _storage.TransferEntities(ent, args.Target, args.User, sourceComp: toolboxStorage, targetComp: targetStorage);
    }
}
