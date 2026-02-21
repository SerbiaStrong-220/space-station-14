using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.Mind;
using Content.Shared.Database;
using Content.Shared.DragDrop;
using Content.Shared.SS220.TransferOnDrag;
using Robust.Shared.Player;

namespace Content.Server.SS220.TransferOnDrag;

public sealed class TransferOnDragSystem : SharedTransferOnDragSystem
{
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly QuickDialogSystem _quickDialog = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TransferOnDragComponent, DragDropDraggedEvent>(OnDragDrop);
    }

    private void OnDragDrop(Entity<TransferOnDragComponent> ent, ref DragDropDraggedEvent args)
    {
        if (args.Handled)
            return;

        if (!Exists(args.Target) || TerminatingOrDeleted(args.Target))
            return;

        if (args.Target == args.User)
            return;

        if (!TryComp<ActorComponent>(args.User, out var actor))
            return;

        var user = args.User;
        var target = args.Target;

        var desc = Loc.GetString("transfer-on-drag-desc", ("name", ToPrettyString(target)));

        _quickDialog.OpenDialog(actor.PlayerSession,
            Loc.GetString("transfer-on-drag-title"),
            desc,
            Loc.GetString("transfer-on-drag-ok"),
            confirmed =>
            {
                if (!confirmed)
                    return;

                _mind.ControlMob(user, target);
                _adminLog.Add(LogType.AdminCommand, LogImpact.Medium, $"{ToPrettyString(user)} transfer mind to {ToPrettyString(target)}");
            });
    }
}
