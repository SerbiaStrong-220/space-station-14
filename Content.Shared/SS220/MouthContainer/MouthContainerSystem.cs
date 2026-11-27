// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Diagnostics.CodeAnalysis;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Gibbing;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Network;

namespace Content.Shared.SS220.MouthContainer;

public sealed class MouthContainerSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MouthContainerComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<MouthContainerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MouthContainerComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<MouthContainerComponent, MouthContainerSpitActionEvent>(OnSpitAction);
        SubscribeLocalEvent<MouthContainerComponent, BeingGibbedEvent>(OnEntityGibbedEvent);
        SubscribeLocalEvent<MouthContainerComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerb);
        SubscribeLocalEvent<MouthContainerComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<MouthContainerComponent, MouthContainerDoAfterInsertEvent>(InsertDoAfter);
        SubscribeLocalEvent<MouthContainerComponent, MouthContainerDoAfterEjectEvent>(EjectDoAfter);
        SubscribeLocalEvent<MouthContainerComponent, EntInsertedIntoContainerMessage>(OnContainerInserted);
        SubscribeLocalEvent<MouthContainerComponent, EntRemovedFromContainerMessage>(OnContainerRemoved);
        base.Initialize();
    }

    private void OnMapInit(Entity<MouthContainerComponent> ent, ref MapInitEvent args)
    {
        UpdateAppearance(ent);
        UpdateAction(ent);
    }

    private void OnShutdown(Entity<MouthContainerComponent> ent, ref ComponentShutdown args)
    {
        if (_net.IsServer)
            _actions.RemoveAction(ent.Comp.EjectActionEntity);
    }

    private void OnSpitAction(Entity<MouthContainerComponent> ent, ref MouthContainerSpitActionEvent args)
    {
        if (args.Handled || args.Performer != ent.Owner)
            return;

        args.Handled = TryStartEject(ent, args.Performer);
    }

    /// <summary>
    ///     Shows the spit action on the action bar only while the mouth contains an item.
    /// </summary>
    private void UpdateAction(Entity<MouthContainerComponent> ent)
    {
        if (!_net.IsServer)
            return;

        if (ent.Comp.MouthSlot.ContainedEntity == null)
        {
            _actions.RemoveAction(ent.Comp.EjectActionEntity);
            return;
        }

        if (_actions.AddAction(ent.Owner, ref ent.Comp.EjectActionEntity, ent.Comp.EjectAction))
            Dirty(ent);
    }

    private void OnContainerInserted(Entity<MouthContainerComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container == ent.Comp.MouthSlot)
        {
            UpdateAppearance(ent);
            UpdateAction(ent);
        }
    }

    private void OnContainerRemoved(Entity<MouthContainerComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container == ent.Comp.MouthSlot)
        {
            UpdateAppearance(ent);
            UpdateAction(ent);
        }
    }

    private void OnStartup(Entity<MouthContainerComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.MouthSlot = _container.EnsureContainer<ContainerSlot>(ent.Owner, ent.Comp.MouthSlotId);
    }

    /// <summary>
    ///     Choose options for interacting with the MouthSlot to the context menu.
    /// </summary>
    private void OnGetVerb(Entity<MouthContainerComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        var user = args.User;
        if (user == ent.Owner || !args.CanAccess || !args.CanInteract || !CanInteract(ent, user))
            return;

        var toInsert = _hands.GetActiveItem(user);

        if (CanInsert(ent, toInsert))
        {
            var insertVerb = new AlternativeVerb
            {
                Priority = 1,
                Text = Loc.GetString(ent.Comp.InsertVerbOut),
                DoContactInteraction = true,
                Act = () => TryStartInsert(ent, user, toInsert.Value),
            };
            args.Verbs.Add(insertVerb);
        }

        if (ent.Comp.MouthSlot.ContainedEntity == null)
            return;

        var verb = new AlternativeVerb
        {
            Priority = 1,
            Text = Loc.GetString(ent.Comp.EjectVerbOut),
            DoContactInteraction = true,
            Act = () => TryStartEject(ent, user),
        };
        args.Verbs.Add(verb);
    }

    /// <summary>
    ///     Try to eject from MouthSlot when entity is gibbed.
    /// </summary>
    private void OnEntityGibbedEvent(Entity<MouthContainerComponent> ent, ref BeingGibbedEvent args)
    {
        TryEject(ent);
    }

    /// <summary>
    ///     Immediately inserts an item, checking the whitelist and container capacity.
    /// </summary>
    public bool TryInsert(Entity<MouthContainerComponent> ent, EntityUid toInsert)
    {
        if (!CanInsert(ent, toInsert) || !_container.Insert(toInsert, ent.Comp.MouthSlot))
            return false;

        _popup.PopupPredicted(Loc.GetString(ent.Comp.InsertMessage, ("entity", ent.Owner), ("item", toInsert)),
            ent.Owner, ent.Owner);
        return true;
    }

    /// <summary>
    ///     Immediately ejects the stored item without requiring a conscious user, e.g. when gibbing.
    /// </summary>
    public bool TryEject(Entity<MouthContainerComponent> ent)
    {
        if (ent.Comp.MouthSlot.ContainedEntity is not { } item)
            return false;

        return _container.Remove(item, ent.Comp.MouthSlot);
    }

    /// <summary>
    ///     Starts inserting an item into the mouth after checking the user's ability to interact.
    /// </summary>
    public bool TryStartInsert(Entity<MouthContainerComponent> ent, EntityUid user, EntityUid toInsert)
    {
        var uid = ent.Owner;
        var component = ent.Comp;
        if (!CanInteract(ent, user) || !CanInsert(ent, toInsert))
            return false;

        if (user != uid && _hands.GetActiveItem(user) != toInsert)
            return false;

        var duration = uid == user ? component.InsertDuration : component.InsertUserDuration;

        return _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
            user,
            duration,
            new MouthContainerDoAfterInsertEvent(GetNetEntity(toInsert)),
            uid,
            target: uid,
            used: toInsert)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            MovementThreshold = 1.0f,
            NeedHand = user != uid,
        });
    }

    /// <summary>
    ///     Starts extracting an item. Spitting out one's own item is instant by default.
    /// </summary>
    public bool TryStartEject(Entity<MouthContainerComponent> ent, EntityUid user)
    {
        var uid = ent.Owner;
        var component = ent.Comp;
        if (!CanInteract(ent, user) || component.MouthSlot.ContainedEntity is not { } item || !Exists(item))
            return false;

        var duration = uid == user ? component.EjectDuration : component.EjectUserDuration;

        return _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
            user,
            duration,
            new MouthContainerDoAfterEjectEvent(),
            uid,
            target: uid)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            MovementThreshold = 1.0f,
        });
    }

    /// <summary>
    ///     Insert item after progressbar.
    /// </summary>
    private void InsertDoAfter(Entity<MouthContainerComponent> ent, ref MouthContainerDoAfterInsertEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (!CanInteract(ent, args.User))
            return;

        var toInsert = GetEntity(args.ToInsert);
        args.Handled = TryInsert(ent, toInsert);
    }

    /// <summary>
    ///     Eject item after progressbar.
    /// </summary>
    private void EjectDoAfter(Entity<MouthContainerComponent> ent, ref MouthContainerDoAfterEjectEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (!CanInteract(ent, args.User) || ent.Comp.MouthSlot.ContainedEntity is not { } item)
            return;

        if (!TryEject(ent))
            return;

        _hands.TryPickupAnyHand(args.User, item);

        _popup.PopupPredicted(Loc.GetString(ent.Comp.EjectMessage, ("entity", ent.Owner), ("item", item)),
            ent.Owner, args.User);
        args.Handled = true;
    }

    /// <summary>
    ///     Toggle MouthContainerVisuals.
    /// </summary>
    private void UpdateAppearance(Entity<MouthContainerComponent> ent)
    {
        var component = ent.Comp;
        var uid = ent.Owner;
        var visible = component.MouthSlot.ContainedEntity != null &&
                      (!TryComp<MobStateComponent>(uid, out var mobState) || _mobStateSystem.IsAlive(uid, mobState));
        _appearance.SetData(uid, MouthContainerVisuals.Visible, visible);
    }

    /// <summary>
    ///     Update appearance on changed mob state.
    /// </summary>
    private void OnMobStateChanged(Entity<MouthContainerComponent> ent, ref MobStateChangedEvent args)
    {
        UpdateAppearance(ent);
    }

    /// <summary>
    ///     Check can item be inserted in MouthSlot.
    /// </summary>
    public bool CanInsert(Entity<MouthContainerComponent> ent, [NotNullWhen(true)] EntityUid? toInsert)
    {
        if (toInsert == null || toInsert == ent.Owner || !Exists(toInsert.Value))
            return false;

        if (_whitelistSystem.IsWhitelistPass(ent.Comp.Blacklist, toInsert.Value) ||
            _whitelistSystem.IsWhitelistFail(ent.Comp.Whitelist, toInsert.Value))
            return false;

        return ent.Comp.MouthSlot.ContainedEntity == null && _container.CanInsert(toInsert.Value, ent.Comp.MouthSlot);
    }

    /// <summary>
    ///     Only the container owner or an entity with hands may interact with the mouth.
    /// </summary>
    private bool CanInteract(Entity<MouthContainerComponent> ent, EntityUid user)
    {
        return Exists(ent.Owner) && Exists(user) &&
               _actionBlocker.CanInteract(user, ent.Owner) &&
               (user == ent.Owner || _hands.GetHandCount(user) > 0);
    }
}
