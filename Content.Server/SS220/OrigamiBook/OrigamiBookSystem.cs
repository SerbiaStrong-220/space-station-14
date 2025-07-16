using Content.Shared.DoAfter;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.SS220.OrigamiBook;

namespace Content.Server.SS220.OrigamiBook;

public sealed class OrigamiBookSystem : SharedOrigamiSystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OrigamiBookComponent, UseInHandEvent>(OnUseInHand);
    }

    private void OnUseInHand(Entity<OrigamiBookComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (HasComp<OrigamiUserComponent>(args.User))
        {
            _popup.PopupEntity(Loc.GetString("origami-book-already-known"), args.User, args.User);
            args.Handled = true;
            return;
        }

        var doAfterArgs = new DoAfterArgs(EntityManager,
            args.User,
            ent.Comp.DelayToLearn,
            new StartLearnOrigamiDoAfter(ent.Owner),
            args.User)
        {
            Broadcast = true,
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
            BlockDuplicate = true,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
        args.Handled = true;
    }
}
