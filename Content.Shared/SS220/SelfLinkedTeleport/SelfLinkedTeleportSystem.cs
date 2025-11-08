// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Popups;
using Content.Shared.SS220.CultYogg.MiGo;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.SelfLinkedTeleport;

/// <summary>
/// </summary>
public sealed class SelfLinkedTeleportSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SelfLinkedTeleportComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SelfLinkedTeleportComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<SelfLinkedTeleportComponent, GetVerbsEvent<Verb>>(OnGetVerb);
        SubscribeLocalEvent<SelfLinkedTeleportComponent, CanDropDraggedEvent>(OnCanDropOn);
        SubscribeLocalEvent<SelfLinkedTeleportComponent, DragDropTargetEvent>(OnDragDropTarget);
        SubscribeLocalEvent<SelfLinkedTeleportComponent, SelfLinkedTeleportDoAfterEvent>(OnTeleportDoAfter);
    }

    private void OnMapInit(Entity<SelfLinkedTeleportComponent> ent, ref MapInitEvent args)//not sure about an event type
    {
        var locations = EntityQueryEnumerator<SelfLinkedTeleportComponent>();
        while (locations.MoveNext(out var uid, out var teleport))
        {
            if (uid == ent.Owner)//shouldn't be linked to itself
                continue;

            if (TerminatingOrDeleted(uid))
                continue;

            if (teleport.LinkedEntity != null)//if its already linked = find next one
                continue;

            if (_whitelist.IsWhitelistFail(ent.Comp.UserWhitelist, uid))
                continue;

            ent.Comp.LinkedEntity = uid;//ToDo_SS220 maybe it should be incapsulated in function
            teleport.LinkedEntity = ent;
        }
    }

    private void OnRemove(Entity<SelfLinkedTeleportComponent> ent, ref ComponentRemove args)
    {
        if (ent.Comp.LinkedEntity == null)
            return;

        if (TryComp<SelfLinkedTeleportComponent>(ent, out var linkedTeleporterComp))
        {
            linkedTeleporterComp.LinkedEntity = null;
        }

        ent.Comp.LinkedEntity = null;
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

    private void OnCanDropOn(Entity<SelfLinkedTeleportComponent> ent, ref CanDropDraggedEvent args)//wtf
    {
        args.CanDrop |= args.Target == args.User; //should be not only that

        if (args.CanDrop)
            args.Handled = true;
    }

    private void OnDragDropTarget(Entity<SelfLinkedTeleportComponent> ent, ref DragDropTargetEvent args)
    {
        //ToDo_SS220 add dragdrop

        WarpTo(ent, args.Dragged);
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

    private void WarpTo(Entity<SelfLinkedTeleportComponent> ent, EntityUid user)
    {
        if (ent.Comp.LinkedEntity == null)
            return;

        _adminLogger.Add(LogType.Teleport, $"{ToPrettyString(user):user} used linked telepoter {ToPrettyString(ent):teleport enter} and was teleported to {ToPrettyString(ent.Comp.LinkedEntity.Value):teleport exit}");

        var xform = Transform(user);
        _transformSystem.SetCoordinates(user, xform, Transform(ent.Comp.LinkedEntity.Value).Coordinates);
    }
}
