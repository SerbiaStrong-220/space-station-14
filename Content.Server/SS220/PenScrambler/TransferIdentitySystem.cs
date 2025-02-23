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

        if (penComponent.Target == null)
            return;

        ent.Comp.Target = penComponent.Target.Value;
        ent.Comp.AppearanceComponent = penComponent.AppearanceComponent;

        _popup.PopupEntity(Loc.GetString("pen-scrambler-success-transfer-to-implant",
            ("identity", MetaData(penComponent.Target.Value).EntityName)), args.User, args.User);

        Dirty(ent);
        QueueDel(args.Target);
    }
}
