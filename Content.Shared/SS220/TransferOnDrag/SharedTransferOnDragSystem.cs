using Content.Shared.DragDrop;
using Content.Shared.Mind;

namespace Content.Shared.SS220.TransferOnDrag;

public sealed class SharedTransferOnDragSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<TransferOnDragComponent, CanDragEvent>(OnCanDrag);
        SubscribeLocalEvent<TransferOnDragComponent, CanDropDraggedEvent>(OnCanDropDragged);
        SubscribeLocalEvent<TransferOnDragComponent, DragDropDraggedEvent>(OnDragDrop);
    }

    private void OnCanDrag(Entity<TransferOnDragComponent> ent, ref CanDragEvent args)
    {
        args.Handled = true;
    }

    private void OnCanDropDragged(Entity<TransferOnDragComponent> ent, ref CanDropDraggedEvent args)
    {
        args.CanDrop = true;
        args.Handled = true;
    }

    private void OnDragDrop(Entity<TransferOnDragComponent> ent, ref DragDropDraggedEvent args)
    {
        if (args.Handled)
            return;

        if (!Exists(args.Target) || TerminatingOrDeleted(args.Target))
            return;

        if (args.Target == args.User)
            return;

        _mind.ControlMob(args.User, args.Target);
    }
}
