using Content.Shared.Body.Events;
using Content.Shared.Database;
using Content.Shared.DoAfter;
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
        SubscribeLocalEvent<MouthContainerComponent, MouthContainerDoAfterEvent>(InsertDoAfter);
        base.Initialize();
    }

    private void OnStartup(EntityUid uid, MouthContainerComponent component, ComponentStartup args)
    {
        component.MouthSlot = _container.EnsureContainer<ContainerSlot>(uid, component.MouthSlotId);
    }

    /// <summary>
    ///     Choose options for interacting with the MouthSlot to the context menu.
    /// </summary>
    private void OnGetVerb(Entity<MouthContainerComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        var subject = args.User;
        var toInsert = _hands.GetActiveItem(subject);
        if (CanInsert(ent!, toInsert))
        {
            AddMouthContainerVerb(ent, ref args, toInsert!.Value);
        }

        if (ent.Comp.MouthSlot.ContainedEntity != null)
        {
            AddMouthContainerVerb(ent, ref args);
        }
    }

    /// <summary>
    ///     Adds options for interacting with the MouthSlot to the context menu.
    /// </summary>
    private void AddMouthContainerVerb(Entity<MouthContainerComponent> ent,
        ref GetVerbsEvent<AlternativeVerb> args,
        EntityUid? toInsert = null)
    {
        var user = args.User;
        AlternativeVerb verb;

        if (toInsert != null)
        {
            verb = new AlternativeVerb
            {
                Priority = 1,
                Text = Loc.GetString(ent.Comp.InsertVerbOut),
                Impact = LogImpact.Medium,
                DoContactInteraction = true,
                Act = () => TryInsert(ent!, user, toInsert.Value),
            };
        }
        else
        {
            var str = Loc.GetString(user == args.Target ? ent.Comp.EjectVerbIn : ent.Comp.EjectVerbOut);
            verb = new AlternativeVerb
            {
                Priority = 1,
                Text = str,
                Impact = LogImpact.Medium,
                DoContactInteraction = true,
                Act = () => TryEject(ent, user),
            };
        }

        args.Verbs.Add(verb);
    }

    /// <summary>
    ///     Try to eject from MouthSlot when entity is gibbed.
    /// </summary>
    private void OnEntityGibbedEvent(Entity<MouthContainerComponent> ent, ref BeingGibbedEvent args)
    {
        TryEject(ent, ent);
    }

    /// <summary>
    ///     Try to insert to MouthSlot. Launches the progress bar from inside and outside.
    /// </summary>
    public void TryInsert(Entity<MouthContainerComponent?> ent, EntityUid subject, EntityUid toInsert)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return;

        if (!CanInsert(ent.Owner, toInsert))
            return;

        if (!Exists(toInsert))
            return;

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
            subject,
            ent.Comp.InsertDuration,
            new MouthContainerDoAfterEvent(toInsert),
            ent.Owner,
            ent.Owner,
            ent.Owner) { BreakOnMove = true, BreakOnDamage = true, MovementThreshold = 1.0f, });
    }

    /// <summary>
    ///     Try to eject from MouthSlot. Launches the progress bar from outside. Eject instantly from inside.
    /// </summary>
    private void TryEject(Entity<MouthContainerComponent> ent, EntityUid subject)
    {
        var uid = ent.Owner;
        var component = ent.Comp;
        var toremove = component.MouthSlot.ContainedEntity!.Value;

        if (!Resolve(uid, ref component))
            return;

        if (!Exists(toremove))
            return;

        if (uid == subject)
        {
            _container.RemoveEntity(uid, component.MouthSlot.ContainedEntity!.Value);
            _popup.PopupPredicted(Loc.GetString(component.EjectMessage), uid, uid);
            UpdateAppearance(uid, component);
            return;
        }

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
            subject,
            component.EjectDuration,
            new MouthContainerDoAfterEvent(uid),
            uid,
            uid,
            uid) { BreakOnMove = true, BreakOnDamage = true, MovementThreshold = 1.0f, });
    }

    /// <summary>
    ///     Insert item after progressbar.
    /// </summary>
    private void InsertDoAfter(Entity<MouthContainerComponent> ent, ref MouthContainerDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { Valid: true } target)
            return;

        if (!TryComp(target, out MouthContainerComponent? _))
            return;

        if (!Exists(ent) || !Exists(args.ToInsert))
            return;

        if (ent.Comp.MouthSlot.ContainedEntity == null)
        {
            if (Exists(args.ToInsert) && _container.CanInsert(args.ToInsert, ent.Comp.MouthSlot))
            {
                _container.Insert(args.ToInsert, ent.Comp.MouthSlot);
                _popup.PopupPredicted(Loc.GetString(ent.Comp.InsertMessage), ent.Owner, ent.Owner);
            }
        }
        else
        {
            var containedEntity = ent.Comp.MouthSlot.ContainedEntity.Value;
            if (Exists(containedEntity))
            {
                _container.RemoveEntity(ent.Owner, containedEntity);
                _popup.PopupPredicted(Loc.GetString(ent.Comp.EjectMessage), ent.Owner, ent.Owner);
            }
        }

        UpdateAppearance(ent.Owner, ent.Comp);
        args.Handled = true;
    }

    /// <summary>
    ///     Toggle MouthContainerVisuals.
    /// </summary>
    private void UpdateAppearance(EntityUid uid, MouthContainerComponent component)
    {
        _appearance.SetData(uid, MouthContainerVisuals.Visible, component.MouthSlot.ContainedEntity != null && (!TryComp<MobStateComponent>(uid, out var mobState) || _mobStateSystem.IsAlive(uid, mobState)));
    }

    /// <summary>
    ///     Update appearance on changed mob state.
    /// </summary>
    private void OnMobStateChanged(Entity<MouthContainerComponent> ent, ref MobStateChangedEvent args)
    {
        UpdateAppearance(ent.Owner, ent.Comp);
    }

    /// <summary>
    ///     Check can item be inserted in MouthSlot.
    /// </summary>
    public bool CanInsert(Entity<MouthContainerComponent?> ent, EntityUid? toInsert)
    {
        if (!Resolve(ent.Owner, ref ent.Comp) || toInsert == null || toInsert == ent.Owner)
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
