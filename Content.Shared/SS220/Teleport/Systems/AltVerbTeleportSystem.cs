// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Content.Shared.SS220.Teleport.Components;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;

namespace Content.Shared.SS220.Teleport.Systems;

public sealed class AltVerbTeleportSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AltVerbTeleportComponent, GetVerbsEvent<AlternativeVerb>>(OnAddSwitchModeVerb);
    }

    private void OnAddSwitchModeVerb(Entity<AltVerbTeleportComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null || !args.Using.HasValue)
            return;

        if (_whitelist.IsWhitelistFail(ent.Comp.UserWhitelist, args.User))
        {
            if (ent.Comp.WhitelistRejectedLoc != null)
                _popup.PopupPredicted(Loc.GetString(ent.Comp.WhitelistRejectedLoc), ent, args.User, PopupType.MediumCaution);

            return;
        }

        if (_whitelist.IsWhitelistPass(ent.Comp.UserBlacklist, args.User))
            return;

        TryStartTeleport(ent, args.User);
    }

    private bool TryStartTeleport(Entity<AltVerbTeleportComponent> ent, EntityUid user)
    {
        var ev = new TeleportUseAttemptEvent(user, user);
        RaiseLocalEvent(ent, ref ev);

        if (ev.Cancelled)
            return false;

        if (ent.Comp.TeleportDoAfterTime is null)
        {
            SendTeleporting(ent, user);
            return true;
        }

        var teleportDoAfter = new DoAfterArgs(EntityManager, user, ent.Comp.TeleportDoAfterTime.Value, new InteractionTeleportDoAfterEvent(), ent, user)
        {
            BreakOnDamage = ent.Comp.DamageThreshold != null,
            DamageThreshold = ent.Comp.DamageThreshold ?? 0,
            BreakOnMove = true,
            BlockDuplicate = true,
            CancelDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameEvent
        };

        if (_doAfter.TryStartDoAfter(teleportDoAfter))
        {
            _popup.PopupPredicted(Loc.GetString("teleport-user-started"), ent, user, PopupType.MediumCaution);
            return true;
        }
        else
        {
            return false;
        }
    }

    private void SendTeleporting(Entity<AltVerbTeleportComponent> ent, EntityUid user)
    {
        var ev = new TeleportTargetEvent(user, user);
        RaiseLocalEvent(ent, ref ev);
    }
}
