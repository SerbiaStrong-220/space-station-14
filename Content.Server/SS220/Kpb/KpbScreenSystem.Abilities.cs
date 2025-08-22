using Content.Shared.SS220.Kpb;
using Robust.Shared.Player;

namespace Content.Server.SS220.Kpb;

public sealed partial class KpbScreenSystem
{
    private void InitializeKpbScreenAbilities()
    {
        SubscribeLocalEvent<KpbScreenComponent, KpbScreenActionEvent>(KpbScreenAction);
    }

    private void KpbScreenAction(EntityUid uid, KpbScreenComponent comp, KpbScreenActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<ActorComponent>(uid, out var actor))
            return;

        _uiSystem.TryOpenUi(uid, KpbScreenUiKey.Key, actor.Owner);

        UpdateInterface(uid, comp);

        args.Handled = true;
    }
}
