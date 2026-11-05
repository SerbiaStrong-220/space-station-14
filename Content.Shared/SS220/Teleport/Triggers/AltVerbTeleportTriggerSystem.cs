// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Network;

namespace Content.Shared.SS220.Teleport.Triggers;

public sealed partial class AltVerbTeleportTriggerSystem : EntitySystem
{
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AltVerbTeleportTriggerComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAlternativeVerb);
        SubscribeLocalEvent<AltVerbTeleportTriggerComponent, AltVerbTeleportDoAfterEvent>(OnAltVerbTeleportDoAfter);
    }

    private void OnGetAlternativeVerb(Entity<AltVerbTeleportTriggerComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess)
            return;

        if (!args.CanInteract)
            return;

        if (args.Hands == null)
            return;

        if (_hands.GetActiveItem((args.User, args.Hands)) != ent.Owner)
            return;

        if (!IsUserAllowed(ent, args.User))
            return;

        var user = args.User;
        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString(ent.Comp.VerbText),
            IconEntity = GetNetEntity(ent.Owner),
            Act = () => TryStartTeleport(ent, user)
        });
    }

    private bool TryStartTeleport(Entity<AltVerbTeleportTriggerComponent> ent, EntityUid user)
    {
        if (_hands.GetActiveItem(user) != ent.Owner)
            return false;

        if (_whitelist.IsWhitelistFail(ent.Comp.UserWhitelist, user))
        {
            ShowWhitelistRejectedPopup(ent, user);
            return false;
        }

        if (_whitelist.IsWhitelistPass(ent.Comp.UserBlacklist, user))
            return false;

        var attemptEvent = new TeleportUseAttemptEvent(user, user);
        RaiseLocalEvent(ent, ref attemptEvent);

        if (attemptEvent.Cancelled)
            return false;

        if (ent.Comp.TeleportDoAfterTime is null)
            return TryRequestTeleport(ent, user);

        var teleportDoAfter = new DoAfterArgs(EntityManager, user, ent.Comp.TeleportDoAfterTime.Value, new AltVerbTeleportDoAfterEvent(), eventTarget: ent, target: user)
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

    private bool IsUserAllowed(Entity<AltVerbTeleportTriggerComponent> ent, EntityUid user)
    {
        if (_whitelist.IsWhitelistFail(ent.Comp.UserWhitelist, user))
            return false;

        if (_whitelist.IsWhitelistPass(ent.Comp.UserBlacklist, user))
            return false;

        return true;
    }

    private void ShowWhitelistRejectedPopup(Entity<AltVerbTeleportTriggerComponent> ent, EntityUid user)
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

    private void OnAltVerbTeleportDoAfter(Entity<AltVerbTeleportTriggerComponent> ent, ref AltVerbTeleportDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        if (args.Target is not { } target)
            return;

        if (_hands.GetActiveItem(args.User) != ent.Owner)
            return;

        if (TryRequestTeleport(ent, target))
            return;

        if (_net.IsClient)
            return;

        Log.Error($"AltVerbTeleportTrigger on {ToPrettyString(ent)} couldn't teleport {ToPrettyString(target)} because no teleport implementation handled the request");
    }

    private bool TryRequestTeleport(Entity<AltVerbTeleportTriggerComponent> ent, EntityUid user)
    {
        var request = new TeleportRequestEvent(user, user);
        RaiseLocalEvent(ent, ref request);
        return request.Handled;
    }
}
