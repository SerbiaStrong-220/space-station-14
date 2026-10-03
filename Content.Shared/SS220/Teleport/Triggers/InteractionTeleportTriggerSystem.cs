// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Ghost;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Network;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.Teleport.Triggers;

public sealed partial class InteractionTeleportTriggerSystem : EntitySystem
{
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InteractionTeleportTriggerComponent, GetVerbsEvent<Verb>>(OnGetVerb);
        SubscribeLocalEvent<InteractionTeleportTriggerComponent, GetVerbsEvent<AlternativeVerb>>(OnGetGhostVerb);
        SubscribeLocalEvent<InteractionTeleportTriggerComponent, CanDropTargetEvent>(OnCanDropTarget);
        SubscribeLocalEvent<InteractionTeleportTriggerComponent, DragDropTargetEvent>(OnDragDropTarget);
        SubscribeLocalEvent<InteractionTeleportTriggerComponent, InteractionTeleportDoAfterEvent>(OnTeleportDoAfter);
    }

    private void OnGetVerb(Entity<InteractionTeleportTriggerComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess)
            return;

        if (!args.CanInteract)
            return;

        if (!IsTargetAllowed(ent, args.User))
            return;

        var user = args.User;

        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString("teleport-enter-verb"),
            Act = () => TryStartTeleport(ent, user, user)
        });
    }

    private void OnGetGhostVerb(Entity<InteractionTeleportTriggerComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess)
            return;

        if (!HasComp<GhostComponent>(args.User))
            return;

        var ghost = args.User;
        args.Verbs.Add(new AlternativeVerb
        {
            Priority = 11,
            Text = Loc.GetString("portal-component-ghost-traverse"),
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/open.svg.192dpi.png")),
            Act = () => TryRequestGhostTeleport(ent, ghost)
        });
    }

    private void OnCanDropTarget(Entity<InteractionTeleportTriggerComponent> ent, ref CanDropTargetEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        args.CanDrop = IsTargetAllowed(ent, args.Dragged);
    }

    private void OnDragDropTarget(Entity<InteractionTeleportTriggerComponent> ent, ref DragDropTargetEvent args)
    {
        if (args.Handled)
            return;

        if (!TryStartTeleport(ent, args.Dragged, args.User))
            return;

        args.Handled = true;
    }

    private bool IsTargetAllowed(Entity<InteractionTeleportTriggerComponent> ent, EntityUid target)
    {
        if (_whitelist.IsWhitelistFail(ent.Comp.TargetWhitelist, target))
            return false;

        if (_whitelist.IsWhitelistPass(ent.Comp.TargetBlacklist, target))
            return false;

        return true;
    }

    private bool TryStartTeleport(Entity<InteractionTeleportTriggerComponent> ent, EntityUid target, EntityUid user)
    {
        if (_whitelist.IsWhitelistFail(ent.Comp.TargetWhitelist, target))
        {
            ShowWhitelistRejectedPopup(ent, user);
            return false;
        }

        if (_whitelist.IsWhitelistPass(ent.Comp.TargetBlacklist, target))
            return false;

        var attemptEvent = new TeleportUseAttemptEvent(target, user);
        RaiseLocalEvent(ent, ref attemptEvent);

        if (attemptEvent.Cancelled)
            return false;

        if (ent.Comp.TeleportDoAfterTime is null)
            return TryRequestTeleport(ent, target, user);

        var teleportDoAfter = new DoAfterArgs(EntityManager, user, ent.Comp.TeleportDoAfterTime.Value, new InteractionTeleportDoAfterEvent(), ent, target)
        {
            BreakOnDamage = ent.Comp.DamageThreshold != null,
            DamageThreshold = ent.Comp.DamageThreshold ?? 0,
            BreakOnMove = true,
            BlockDuplicate = true,
            CancelDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameEvent
        };

        if (!_doAfter.TryStartDoAfter(teleportDoAfter))
            return false;

        _popup.PopupPredicted(
            Loc.GetString("teleport-interaction-started"),
            null,
            user,
            user,
            PopupType.MediumCaution);
        return true;
    }

    private void ShowWhitelistRejectedPopup(
        Entity<InteractionTeleportTriggerComponent> ent,
        EntityUid user)
    {
        if (ent.Comp.WhitelistRejectedLoc is not { } rejectedLoc)
            return;

        _popup.PopupPredicted(
            Loc.GetString(rejectedLoc),
            null,
            ent,
            user,
            PopupType.MediumCaution);
    }

    private void OnTeleportDoAfter(Entity<InteractionTeleportTriggerComponent> ent, ref InteractionTeleportDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        if (args.Target == null)
            return;

        if (TryRequestTeleport(ent, args.Target.Value, args.User))
            return;

        if (_net.IsClient)
            return;

        Log.Error($"InteractionTeleportTrigger on {ToPrettyString(ent)} couldn't teleport {ToPrettyString(args.Target.Value)} because no teleport implementation handled the request");
    }

    private bool TryRequestTeleport(Entity<InteractionTeleportTriggerComponent> ent, EntityUid target, EntityUid user)
    {
        var request = new TeleportRequestEvent(target, user);
        RaiseLocalEvent(ent, ref request);
        return request.Handled;
    }

    private bool TryRequestGhostTeleport(Entity<InteractionTeleportTriggerComponent> ent, EntityUid ghost)
    {
        if (!HasComp<GhostComponent>(ghost))
            return false;

        var attemptEvent = new TeleportUseAttemptEvent(ghost, ghost);
        RaiseLocalEvent(ent, ref attemptEvent);

        if (attemptEvent.Cancelled)
            return false;

        var request = new GhostTeleportRequestEvent(ghost);
        RaiseLocalEvent(ent, ref request);
        return request.Handled;
    }
}
