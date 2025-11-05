using Content.Shared.Popups;
using Content.Shared.Animals.Components;
using Content.Shared.Body.Events;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Item;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;

namespace Content.Shared.Animals.Systems;

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
        SubscribeLocalEvent<MouthContainerComponent, GetVerbsEvent<Verb>>(OnGetVerb);
        SubscribeLocalEvent<MouthContainerComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<MouthContainerComponent, MouthContainerDoAfterEvent>(InsertDoAfter);
        SubscribeLocalEvent<ItemComponent, GetVerbsEvent<Verb>>(OnGetVerbItem);

        base.Initialize();
    }

    private void OnStartup(EntityUid uid, MouthContainerComponent component, ComponentStartup args)
    {
        component.MouthSlot = _container.EnsureContainer<ContainerSlot>(uid, component.MouthSlotId);
    }

    private void OnGetVerb(Entity<MouthContainerComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        var subject = args.User;
        var toInsert = _hands.GetActiveItem(subject);
        if (CanInsert(ent, toInsert, ent))
        {
            var v = new Verb
            {
                Priority = 1,
                Text = Loc.GetString(ent.Comp.InsertVerbOut),
                Disabled = false,
                Impact = LogImpact.Medium,
                DoContactInteraction = true,
                Act = () =>
                {
                    TryInsert(ent, subject, toInsert, ent);
                },
            };
            args.Verbs.Add(v);
        }
        else
        {
            if (ent.Comp.MouthSlot.ContainedEntity == null)
                return;
            string str;
            str = Loc.GetString(args.User == args.Target ? ent.Comp.EjectVerbIn : ent.Comp.EjectVerbOut);

            var v = new Verb
            {
                Priority = 1,
                Text = str,
                Disabled = false,
                Impact = LogImpact.Medium,
                DoContactInteraction = true,
                Act = () =>
                {
                    {
                        TryEject(ent, ent);
                    }
                },
            };
            args.Verbs.Add(v);
        }
    }

    private void OnGetVerbItem(Entity<ItemComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        var subject = args.User;
        if (!HasComp<MouthContainerComponent>(subject))
            return;
        var mouthComp = Comp<MouthContainerComponent>(subject);
        var toInsert = ent.Owner;

        if (CanInsert(subject, toInsert, mouthComp))
        {
            var v = new Verb
            {
                Priority = 1,
                Text = Loc.GetString(mouthComp.InsertVerbIn),
                Disabled = false,
                Impact = LogImpact.Medium,
                DoContactInteraction = true,
                Act = () =>
                {
                    TryInsert(subject, subject, toInsert, mouthComp);
                },
            };
            args.Verbs.Add(v);
        }
    }

    private void OnEntityGibbedEvent(Entity<MouthContainerComponent> ent, ref BeingGibbedEvent args)
    {
        TryEject(ent, ent);
    }

    private void TryInsert(EntityUid uid, EntityUid subject, EntityUid? toInsert, MouthContainerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;
        if (!CanInsert(uid, toInsert!.Value, component))
            return;

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, subject, component.InsertDuration, new MouthContainerDoAfterEvent(toInsert.Value), uid, uid, uid)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            MovementThreshold = 1.0f,
        });
    }

    private void TryEject(EntityUid uid, MouthContainerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (component.MouthSlot.ContainedEntity == null)
            return;

        var toremove = component.MouthSlot.ContainedEntity.Value;

        _container.RemoveEntity(uid, toremove);
        _popup.PopupPredicted(Loc.GetString(component.EjectMessage), uid, uid);
        UpdateAppearance(uid, component);
    }

    private void InsertDoAfter(Entity<MouthContainerComponent> ent, ref MouthContainerDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Target == null)
            return;

        if (args.Target is not { Valid: true } target)
            return;

        if (!TryComp(target, out MouthContainerComponent? mouthContainer))
            return;

        _container.Insert(args.ToInsert, ent.Comp.MouthSlot);
        _popup.PopupPredicted(Loc.GetString(ent.Comp.InsertMessage), ent.Owner, ent.Owner);
        UpdateAppearance(ent.Owner, ent.Comp);
    }

    private void UpdateAppearance(EntityUid uid, MouthContainerComponent component)
    {
        UpdateSprite(uid, component);
        _appearance.SetData(uid, MouthContainerVisuals.Visible, component.IsVisibleCheeks);
    }

    private void OnMobStateChanged(Entity<MouthContainerComponent> ent, ref MobStateChangedEvent args)
    {
        UpdateAppearance(ent.Owner, ent.Comp);
    }

    private void UpdateSprite(EntityUid uid, MouthContainerComponent component)
    {
        component.IsVisibleCheeks = GetVisible();
        return;

        bool GetVisible()
        {
            if (component.MouthSlot.ContainedEntity == null)
                return false;

            if (TryComp<MobStateComponent>(uid, out var mobState) && !_mobStateSystem.IsAlive(uid, mobState))
                return false;

            return true;
        }
    }

    private bool CanInsert(EntityUid uid, EntityUid? toInsert, MouthContainerComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;
        if (toInsert == null)
            return false;
        if (toInsert == uid)
            return false;
        if (_whitelistSystem.IsWhitelistPass(component.Priority, toInsert.Value))
            return IsEmpty(component);
        if (_whitelistSystem.IsWhitelistPass(component.Blacklist, toInsert.Value))
            return false;
        if (_whitelistSystem.IsWhitelistFail(component.Whitelist, toInsert.Value))
            return false;

        return IsEmpty(component);
    }

    private static bool IsEmpty(MouthContainerComponent component)
    {
        return component.MouthSlot.ContainedEntity == null;
    }
}
