// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Buckle.Components;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Gibbing.Events;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
using Content.Shared.SS220.LyingDownOnBuckledEntity.Components;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Shared.Physics.Events;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.LyingDownOnBuckledEntity.Systems;

public sealed partial class LyingDownOnBuckledEntitySystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private string StandUpAction = "ActionStandUp";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LyingDownOnBuckledEntityComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<LyingDownOnBuckledEntityComponent, StandUpActionEvent>(OnStandUpAction);
        SubscribeLocalEvent<LyingDownOnBuckledEntityComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<LyingDownOnBuckledEntityComponent, EndCollideEvent>(OnEndCollide);
        SubscribeLocalEvent<LyingDownOnBuckledEntityComponent, DamageChangedEvent>(OnDamageChanged);

        SubscribeLocalEvent<LyingDownOnBuckledEntityComponent, UpdateCanMoveEvent>(OnCanMoveUpdate);
        SubscribeLocalEvent<LyingDownOnBuckledEntityComponent, RaisePetDoAfterEvent>(OnRaiseDoAfter);
        SubscribeLocalEvent<LyingDownOnBuckledEntityComponent, LieDownDoAfterEvent>(OnLieDownDoAfter);

        SubscribeLocalEvent<UnderLyingPetComponent, UnbuckleAttemptEvent>(OnUnbuckleAttempt);
        SubscribeLocalEvent<UnderLyingPetComponent, UnstrapAttemptEvent>(OnUnstrapAttempt);
        SubscribeLocalEvent<UnderLyingPetComponent, EntityGibbedEvent>(OnEntityGibbed);

        SubscribeLocalEvent<GetVerbsEvent<AlternativeVerb>>(OnAlternativeVerb);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<UnderLyingPetComponent>();
        while (query.MoveNext(out var uid, out var lyingPet))
        {
            if (_timing.CurTime < lyingPet.NextSecond)
                continue;

            lyingPet.NextSecond = _timing.CurTime + TimeSpan.FromSeconds(1);

            if (lyingPet.Damage != null && !_mobState.IsDead(uid))
                _damageable.TryChangeDamage(uid, lyingPet.Damage, interruptsDoAfters: false);
        }
    }

    private void OnShutdown(Entity<LyingDownOnBuckledEntityComponent> entity, ref ComponentShutdown args)
    {
        if (entity.Comp.IsLying)
            StandUp(entity);

        Dirty(entity);
    }

    private void OnStandUpAction(Entity<LyingDownOnBuckledEntityComponent> entity, ref StandUpActionEvent args)
    {
        if (args.Handled)
            return;

        StandUp(entity);
        args.Handled = true;
    }

    private void OnMobStateChanged(Entity<LyingDownOnBuckledEntityComponent> entity, ref MobStateChangedEvent args)
    {
        if (entity.Comp.IsLying &&
            args.NewMobState is not MobState.Alive)
            StandUp(entity);
    }

    private void OnDamageChanged(Entity<LyingDownOnBuckledEntityComponent> entity, ref DamageChangedEvent args)
    {
        if (args.DamageDelta is { } damageDelta &&
            damageDelta.GetTotal() >= entity.Comp.DamagetThreshold)
            StandUp(entity);
    }

    private void OnEndCollide(Entity<LyingDownOnBuckledEntityComponent> entity, ref EndCollideEvent args)
    {
        if (entity.Comp.LyingOn == args.OtherEntity)
            StandUp(entity);
    }

    private void OnCanMoveUpdate(Entity<LyingDownOnBuckledEntityComponent> entity, ref UpdateCanMoveEvent args)
    {
        if (entity.Comp.IsLying)
            args.Cancel();
    }

    private void OnAlternativeVerb(GetVerbsEvent<AlternativeVerb> args)
    {
        var user = args.User;
        var target = args.Target;

        if (!args.CanAccess ||
            user == target)
            return;

        if (TryComp<LyingDownOnBuckledEntityComponent>(target, out var targetLyingDownComp) &&
            targetLyingDownComp.LyingOn is not null)
        {
            var raiseVerb = new AlternativeVerb
            {
                Text = Loc.GetString("raise-pet-verb", ("target", targetLyingDownComp.LyingOn.Value)),
                Act = () =>
                {
                    var doAfterEventArgs = new DoAfterArgs(EntityManager,
                        user,
                        targetLyingDownComp.RaiseDoAfter,
                        new RaisePetDoAfterEvent(),
                        target,
                        target)
                    {
                        Broadcast = false,
                        BreakOnDamage = false,
                        BreakOnMove = true,
                        NeedHand = false,
                        BlockDuplicate = true,
                        CancelDuplicate = true,
                        DuplicateCondition = DuplicateConditions.SameEvent
                    };
                    _doAfter.TryStartDoAfter(doAfterEventArgs);
                }
            };

            args.Verbs.Add(raiseVerb);
        }

        if (TryComp<LyingDownOnBuckledEntityComponent>(user, out var userLyingDownComp) &&
            HasComp<HumanoidAppearanceComponent>(target) &&
            !userLyingDownComp.IsLying &&
            CheckBuckledEntity(user, target) &&
            CheckOtherLyingPets(user, target))
        {
            var lieDownVerb = new AlternativeVerb
            {
                Text = Loc.GetString("pet-lie-down-werb"),
                Act = () =>
                {
                    var doAfterEventArgs = new DoAfterArgs(EntityManager,
                        user,
                        userLyingDownComp.LyingDoAfter,
                        new LieDownDoAfterEvent(),
                        user,
                        target)
                    {
                        Broadcast = false,
                        BreakOnDamage = false,
                        BreakOnMove = true,
                        NeedHand = false,
                        BlockDuplicate = true,
                        CancelDuplicate = true,
                        DuplicateCondition = DuplicateConditions.SameEvent
                    };
                    _doAfter.TryStartDoAfter(doAfterEventArgs);
                }
            };

            args.Verbs.Add(lieDownVerb);
        }
    }

    private void OnLieDownDoAfter(Entity<LyingDownOnBuckledEntityComponent> entity, ref LieDownDoAfterEvent args)
    {
        if (args.Cancelled ||
            args.Target is not { } target ||
            !CheckBuckledEntity(entity, target) ||
            !CheckOtherLyingPets(entity, target))
            return;

        LieDownOnEntity(entity, target);
    }

    private void OnRaiseDoAfter(Entity<LyingDownOnBuckledEntityComponent> entity, ref RaisePetDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        StandUp(entity);
    }

    private void OnUnbuckleAttempt(Entity<UnderLyingPetComponent> entity, ref UnbuckleAttemptEvent args)
    {
        if (!entity.Comp.BlockUnbuckle)
            return;

        args.Cancelled = true;
    }

    private void OnUnstrapAttempt(Entity<UnderLyingPetComponent> entity, ref UnstrapAttemptEvent args)
    {
        if (!entity.Comp.BlockUnbuckle)
            return;

        args.Cancelled = true;
    }

    private void OnEntityGibbed(Entity<UnderLyingPetComponent> entity, ref EntityGibbedEvent args)
    {
        if (entity.Comp.PetUid is { } pet)
            StandUp(pet);
    }

    private void LieDownOnEntity(EntityUid uid, EntityUid target, LyingDownOnBuckledEntityComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var targetCords = _transform.GetMoverCoordinates(target);
        _transform.SetCoordinates(uid, targetCords.Offset(component.Offset));

        component.LyingOn = target;
        component.IsLying = true;
        component.ActionUid = _actions.AddAction(uid, StandUpAction);
        Dirty(uid, component);

        if (TryComp<AppearanceComponent>(uid, out var appearance))
            _appearance.SetData(uid, LyingVisuals.State, true, appearance);

        var lyingPetComp = EnsureComp<UnderLyingPetComponent>(target);
        lyingPetComp.PetUid = uid;
        lyingPetComp.BlockUnbuckle = component.BlockUnbuckle;
        if (component.DamageOnLying is { } damageOnLying &&
            _entityWhitelist.IsWhitelistPass(damageOnLying.Whitelist, target))
        {
            lyingPetComp.Damage = damageOnLying.Damage;
        }

        _actionBlocker.UpdateCanMove(uid);
        Dirty(target, lyingPetComp);
    }

    private void StandUp(EntityUid uid, LyingDownOnBuckledEntityComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        _actions.RemoveAction(component.ActionUid);
        component.ActionUid = null;
        component.IsLying = false;

        if (TryComp<AppearanceComponent>(uid, out var appearance))
            _appearance.SetData(uid, LyingVisuals.State, false, appearance);

        if (component.LyingOn is { } lyingOnEnt)
            RemComp<UnderLyingPetComponent>(lyingOnEnt);

        component.LyingOn = null;
        _actionBlocker.UpdateCanMove(uid);
        Dirty(uid, component);
    }

    /// <summary>
    /// Checks if the target is buckled to a whitelisted entity
    /// </summary>
    private bool CheckBuckledEntity(EntityUid uid, EntityUid target, LyingDownOnBuckledEntityComponent? component = null)
    {
        return Resolve(uid, ref component) &&
            TryComp<BuckleComponent>(target, out var buckle) &&
            buckle.BuckledTo is { } strapEnt &&
            _entityWhitelist.IsWhitelistPass(component.StrapWhitelist, strapEnt);
    }

    /// <summary>
    /// Checks if there aren't other pets lying down on the entity
    /// </summary>
    private bool CheckOtherLyingPets(EntityUid pet, EntityUid target)
    {
        if (!TryComp<UnderLyingPetComponent>(target, out var comp))
            return true;

        if (comp.PetUid != null)
            _popup.PopupEntity(Loc.GetString("other-pet-is-lying-on", ("pet", comp.PetUid.Value), ("target", target)), pet, pet);

        return false;
    }
}

[Serializable, NetSerializable]
public enum LyingVisuals : byte
{
    State,
    LyingDown
}

[Serializable, NetSerializable]
public enum LyingState : byte
{
    LyingDown,
    StandUp
}

public sealed partial class StandUpActionEvent : InstantActionEvent
{
}

[Serializable, NetSerializable]
public sealed partial class RaisePetDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class LieDownDoAfterEvent : SimpleDoAfterEvent
{
}
