using Content.Shared.Body.Events;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;

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

    public override void Initialize()
    {
        SubscribeLocalEvent<MouthContainerComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<MouthContainerComponent, BeingGibbedEvent>(OnEntityGibbedEvent);
        SubscribeLocalEvent<MouthContainerComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerb);
        SubscribeLocalEvent<MouthContainerComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<MouthContainerComponent, MouthContainerDoAfterInsertEvent>(InsertDoAfter);
        SubscribeLocalEvent<MouthContainerComponent, MouthContainerDoAfterEjectEvent>(EjectDoAfter);
        base.Initialize();
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

        var isAlive = !TryComp<MobStateComponent>(user, out var mobState) || _mobStateSystem.IsAlive(user, mobState);
        if (user != args.Target && !HasComp<HandsComponent>(user) || !isAlive)
            return;

        var toInsert = _hands.GetActiveItem(user);

        AlternativeVerb verb;

        if (toInsert != null && CanInsert(ent, toInsert))
        {
            verb = new AlternativeVerb
            {
                Priority = 1,
                Text = Loc.GetString(ent.Comp.InsertVerbOut),
                Impact = LogImpact.Medium,
                DoContactInteraction = true,
                Act = () => TryStartInsert(ent, user, toInsert.Value),
            };
            args.Verbs.Add(verb);
        }

        if (ent.Comp.MouthSlot.ContainedEntity == null)
            return;

        var str = Loc.GetString(user == args.Target ? ent.Comp.EjectVerbIn : ent.Comp.EjectVerbOut);
        verb = new AlternativeVerb
        {
            Priority = 1,
            Text = str,
            Impact = LogImpact.Medium,
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
    ///     Try to insert.
    /// </summary>
    private void TryInsert(Entity<MouthContainerComponent> ent, EntityUid toInsert)
    {
        if (ent.Comp.MouthSlot.ContainedEntity != null || !_container.CanInsert(toInsert, ent.Comp.MouthSlot))
            return;

        _container.Insert(toInsert, ent.Comp.MouthSlot);
        _popup.PopupPredicted(Loc.GetString(ent.Comp.InsertMessage), ent.Owner, ent.Owner);
        UpdateAppearance(ent);
    }

    /// <summary>
    ///     Try to eject.
    /// </summary>
    private void TryEject(Entity<MouthContainerComponent> ent)
    {
        if (ent.Comp.MouthSlot.ContainedEntity == null)
            return;

        _container.RemoveEntity(ent.Owner, ent.Comp.MouthSlot.ContainedEntity.Value);
    }

    /// <summary>
    ///     Try to insert to MouthSlot. Launches the progress bar from inside and outside.
    /// </summary>
    public void TryStartInsert(Entity<MouthContainerComponent> ent, EntityUid user, EntityUid toInsert)
    {
        var uid = ent.Owner;
        var component = ent.Comp;
        if (!CanInsert(ent, toInsert))
            return;

        if (!Exists(toInsert))
            return;

        var duration = component.InsertUserDuration;

        if (uid == user)
            duration = component.InsertDuration;

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
            user,
            duration,
            new MouthContainerDoAfterInsertEvent(GetNetEntity(toInsert)),
            uid,
            uid,
            uid) { BreakOnMove = true, BreakOnDamage = true, MovementThreshold = 1.0f, });
    }

    /// <summary>
    ///     Try to eject from MouthSlot. Launches the progress bar from outside. Eject instantly from inside.
    /// </summary>
    private void TryStartEject(Entity<MouthContainerComponent> ent, EntityUid user)
    {
        var uid = ent.Owner;
        var component = ent.Comp;
        if (component.MouthSlot.ContainedEntity != null)
        {
            var toremove = component.MouthSlot.ContainedEntity.Value;

            if (!Resolve(uid, ref component))
                return;

            if (!Exists(toremove))
                return;
        }

        var duration = component.EjectUserDuration;

        if (uid == user)
            duration = component.EjectDuration;

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
            user,
            duration,
            new MouthContainerDoAfterEjectEvent(),
            uid,
            uid,
            uid) { BreakOnMove = true, BreakOnDamage = true, MovementThreshold = 1.0f, });
    }

    /// <summary>
    ///     Insert item after progressbar.
    /// </summary>
    private void InsertDoAfter(Entity<MouthContainerComponent> ent, ref MouthContainerDoAfterInsertEvent args)
    {
        var toInsert = GetEntity(args.ToInsert);
        if (args.Cancelled || args.Handled)
            return;

        if (!Exists(ent) || !Exists(toInsert))
            return;

        TryInsert(ent, toInsert);

        args.Handled = true;
    }

    /// <summary>
    ///     Eject item after progressbar.
    /// </summary>
    private void EjectDoAfter(Entity<MouthContainerComponent> ent, ref MouthContainerDoAfterEjectEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (!Exists(ent))
            return;

        if (ent.Comp.MouthSlot.ContainedEntity != null)
            _hands.TryPickupAnyHand(args.User, ent.Comp.MouthSlot.ContainedEntity.Value);

        TryEject(ent);

        _popup.PopupPredicted(Loc.GetString(ent.Comp.EjectMessage), ent.Owner, ent.Owner);
        UpdateAppearance(ent);

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
    public bool CanInsert(Entity<MouthContainerComponent> ent, EntityUid? toInsert)
    {
        if (toInsert == null || toInsert == ent.Owner)
            return false;

        if (_whitelistSystem.IsWhitelistPass(ent.Comp.Blacklist, toInsert.Value) ||
            _whitelistSystem.IsWhitelistFail(ent.Comp.Whitelist, toInsert.Value))
            return false;

        return IsEmpty(ent.Comp);
    }

    /// <summary>
    ///     Check is MouthSlot empty.
    /// </summary>
    private static bool IsEmpty(MouthContainerComponent component)
    {
        return component.MouthSlot.ContainedEntity == null;
    }
}
