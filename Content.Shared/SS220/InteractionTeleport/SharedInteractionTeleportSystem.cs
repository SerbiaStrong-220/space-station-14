// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;

namespace Content.Shared.SS220.InteractionTeleport;

public sealed class SharedInteractionTeleportSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InteractionTeleportComponent, GetVerbsEvent<Verb>>(OnGetVerb);
        SubscribeLocalEvent<InteractionTeleportComponent, CanDropTargetEvent>(OnCanDropTarget);
        SubscribeLocalEvent<InteractionTeleportComponent, DragDropTargetEvent>(OnDragDropTarget);
        SubscribeLocalEvent<InteractionTeleportComponent, InteractionTeleportDoAfterEvent>(OnTeleportDoAfter);
    }

    private void OnGetVerb(Entity<InteractionTeleportComponent> ent, ref GetVerbsEvent<Verb> args)//Not sure maybe it should be "InteractionVerb"
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if (_whitelist.IsWhitelistFail(ent.Comp.UserWhitelist, args.User))
            return;

        var user = args.User;

        var teleportVerb = new Verb
        {
            Text = Loc.GetString("teleport-use-verb"),
            Act = () =>
            {
                TryStartTeleport(ent, user, user);
            }
        };
        args.Verbs.Add(teleportVerb);
    }

    private void OnCanDropTarget(Entity<InteractionTeleportComponent> ent, ref CanDropTargetEvent args) //why not working
    {
        if (_whitelist.IsWhitelistFail(ent.Comp.UserWhitelist, args.Dragged))
        {
            _popup.PopupPredicted(ent.Comp.WhitelistRejectedLoc, ent, args.User, PopupType.MediumCaution);//if message is null == will be return anyway
            return;
        }
        args.CanDrop = true;
        args.Handled = true;
    }

    private void OnDragDropTarget(Entity<InteractionTeleportComponent> ent, ref DragDropTargetEvent args)
    {
        TryStartTeleport(ent, args.Dragged, args.User);
    }

    private bool TryStartTeleport(Entity<InteractionTeleportComponent> ent, EntityUid target, EntityUid user)
    {
        if (!ent.Comp.ShouldHaveDelay)
        {
            SendTeleporting(ent, target, user);
            return true;
        }

        var teleportDoAfter = new DoAfterArgs(EntityManager, user, ent.Comp.TeleportDoAfterTime, new InteractionTeleportDoAfterEvent(), ent, target)
        {
            BreakOnDamage = false,
            BreakOnMove = true,
            BlockDuplicate = true,
            CancelDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameEvent
        };

        var started = _doAfter.TryStartDoAfter(teleportDoAfter);

        if (started)
        {
            _popup.PopupPredicted(Loc.GetString("teleport-user-started"), ent, user, PopupType.MediumCaution);
            return true;
        }
        else
            return false;
    }

    private void OnTeleportDoAfter(Entity<InteractionTeleportComponent> ent, ref InteractionTeleportDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        if (args.Target == null)
            return;

        SendTeleporting(ent, args.Target.Value, args.User);
    }

    private void SendTeleporting(Entity<InteractionTeleportComponent> ent, EntityUid target, EntityUid user)
    {
        var ev = new TeleportTargetEvent(target, user);
        RaiseLocalEvent(ent, ref ev);
    }
}
