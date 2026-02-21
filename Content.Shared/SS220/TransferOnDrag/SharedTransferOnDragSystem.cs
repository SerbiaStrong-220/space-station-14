// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.DragDrop;
using Content.Shared.Mind;

namespace Content.Shared.SS220.TransferOnDrag;

public abstract class SharedTransferOnDragSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<TransferOnDragComponent, CanDragEvent>(OnCanDrag);
        SubscribeLocalEvent<TransferOnDragComponent, CanDropDraggedEvent>(OnCanDropDragged);
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
}
