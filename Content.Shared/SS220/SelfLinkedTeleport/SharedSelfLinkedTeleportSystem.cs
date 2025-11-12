// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry.Components;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Foldable;
using Content.Shared.Popups;
using Content.Shared.SS220.CultYogg.Cultists;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.SelfLinkedTeleport;

/// <summary>
/// </summary>
public abstract class SharedSelfLinkedTeleportSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SelfLinkedTeleportComponent, GetVerbsEvent<Verb>>(OnGetVerb);
        SubscribeLocalEvent<SelfLinkedTeleportComponent, CanDropTargetEvent>(OnCanDropTarget);
        SubscribeLocalEvent<SelfLinkedTeleportComponent, DragDropTargetEvent>(OnDragDropTarget);
        SubscribeLocalEvent<SelfLinkedTeleportComponent, SelfLinkedTeleportDoAfterEvent>(OnTeleportDoAfter);
    }

    private void OnGetVerb(Entity<SelfLinkedTeleportComponent> ent, ref GetVerbsEvent<Verb> args)//Not sure maybe it should be "InteractionVerb"
    {
        if (!args.CanAccess || !args.CanInteract || ent.Comp.LinkedEntity == null)
            return;

        if (_whitelist.IsWhitelistFail(ent.Comp.UserWhitelist, args.User))
            return;

        var user = args.User;

        var teleportVerb = new Verb
        {
            Text = Loc.GetString("cult-yogg-enslave-verb"),//ToDo_SS220 add loc
            Icon = new SpriteSpecifier.Rsi(new ResPath("SS220/Interface/Actions/cult_yogg.rsi"), "enslavement"),//ToDo_SS220 add picture
            Act = () =>
            {
                StartTeleport(ent, user);
            }
        };
        args.Verbs.Add(teleportVerb);
    }

    private void OnCanDropTarget(Entity<SelfLinkedTeleportComponent> ent, ref CanDropTargetEvent args)
    {
        if (_whitelist.IsWhitelistFail(ent.Comp.UserWhitelist, args.User))
            return;

        args.CanDrop = true;
        args.Handled = true;
    }

    private void OnDragDropTarget(Entity<SelfLinkedTeleportComponent> ent, ref DragDropTargetEvent args)
    {
        StartTeleport(ent, args.User);
    }

    private void StartTeleport(Entity<SelfLinkedTeleportComponent> ent, EntityUid user)
    {
        if (!ent.Comp.ShouldHaveDelay)
        {
            WarpTo(ent, user);
            return;
        }

        var sacrificeDoAfter = new DoAfterArgs(EntityManager, user, ent.Comp.TeleportDoAfterTime, new SelfLinkedTeleportDoAfterEvent(), ent, target: user)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            BlockDuplicate = true,
            CancelDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameEvent
        };

        var started = _doAfter.TryStartDoAfter(sacrificeDoAfter);

        if (started)
        {
            //ToDo_SS220 add popup
            //_popup.PopupPredicted(Loc.GetString("cult-yogg-sacrifice-started", ("user", user), ("target", user)),
            //    ent, null, PopupType.MediumCaution);
        }
    }

    private void OnTeleportDoAfter(Entity<SelfLinkedTeleportComponent> ent, ref SelfLinkedTeleportDoAfterEvent args)
    {
        WarpTo(ent, args.User);
    }

    protected virtual void WarpTo(Entity<SelfLinkedTeleportComponent> ent, EntityUid user) { }
}
