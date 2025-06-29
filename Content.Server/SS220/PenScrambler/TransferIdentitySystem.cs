using Content.Shared.Popups;
using Content.Shared.SS220.PenScrambler;

namespace Content.Server.SS220.PenScrambler;

public sealed class TransferIdentitySystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<TransferIdentityComponent, CopyDnaFromPenToImplantEvent>(OnCopyIdentityToImplant);
    }

    private void OnCopyIdentityToImplant(Entity<TransferIdentityComponent> ent, ref CopyDnaFromPenToImplantEvent args)
    {
        if (args.Cancelled || args.Target == null)
            return;

        if (!TryComp<PenScramblerComponent>(args.Target, out var penComponent))
            return;

        if (penComponent.NullspaceClone == null)
            return;

        ent.Comp.NullspaceClone = penComponent.NullspaceClone;

        _popup.PopupEntity(Loc.GetString("pen-scrambler-success-transfer-to-implant",
            ("identity", MetaData(penComponent.NullspaceClone.Value).EntityName)), args.User, args.User);

        QueueDel(args.Target);
    }
}
