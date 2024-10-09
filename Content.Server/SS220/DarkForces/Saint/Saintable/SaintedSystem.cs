using System;
using Content.Server.SS220.DarkForces.Saint.Items.Events;
using Content.Server.SS220.DarkForces.Saint.Saintable.Events;
using Content.Server.Popups;
using Content.Shared.Damage;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Prying.Components;
using Content.Shared.SS220.DarkForces.Saint.Saintable;
using Content.Shared.Throwing;
using Content.Shared.Timing;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Physics.Events;

namespace Content.Server.SS220.DarkForces.Saint.Saintable;

public sealed class SaintedSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly TransformSystem _transformSystem = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SaintedComponent, StartCollideEvent>(OnCollideStart);
        SubscribeLocalEvent<SaintedComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<SaintedComponent, GettingPickedUpAttemptEvent>(OnSaintPickedUpAttemptEvent);
        SubscribeLocalEvent<SaintedComponent, InteractHandEvent>(OnSaintInteractHand);
        SubscribeLocalEvent<SaintedComponent, BeforePryEvent>(OnBeforePryEvent);

        SubscribeLocalEvent<SaintSilverComponent, StartCollideEvent>(OnCollideSilver);
        SubscribeLocalEvent<SaintSilverComponent, AfterInteractEvent>(OnAfterInteractSilver);
        SubscribeLocalEvent<SaintSilverComponent, GettingPickedUpAttemptEvent>(OnSaintSilverPickedUpAttemptEvent);
        SubscribeLocalEvent<SaintSilverComponent, InteractHandEvent>(OnSilverInteractHand);
        SubscribeLocalEvent<SaintSilverComponent, BeforePryEvent>(OnBeforePrySilverEvent);
    }

    private void OnBeforePrySilverEvent(EntityUid uid, SaintSilverComponent component, ref BeforePryEvent args)
    {
        if (args.Cancelled)
            return;

        var ev = new OnTryPryingSilverEvent(component.DamageOnCollide, component.PushOnCollide);
        RaiseLocalEvent(args.User, ev);

        if (!ev.Handled)
            return;

        args.Cancelled = true;
        CauseHolyDamage(uid, args.User, ev.DamageOnCollide, ev.PushOnCollide);
    }

    private void OnBeforePryEvent(EntityUid uid, SaintedComponent component, ref BeforePryEvent args)
    {
        if (args.Cancelled)
            return;

        var ev = new OnTryPryingSaintedEvent(component.DamageOnCollide, component.PushOnCollide);
        RaiseLocalEvent(args.User, ev);

        if (!ev.Handled)
            return;

        args.Cancelled = true;
        CauseHolyDamage(uid, args.User, ev.DamageOnCollide, ev.PushOnCollide);
    }

    private void OnSilverInteractHand(EntityUid uid, SaintSilverComponent component, InteractHandEvent args)
    {
        if (args.Handled)
            return;

        var ev = new OnSilverEntityHandInteract(component.DamageOnCollide, component.PushOnCollide);
        RaiseLocalEvent(args.User, ev);

        if (!ev.Handled)
            return;

        args.Handled = true;
        CauseHolyDamage(uid, args.User, ev.DamageOnCollide, ev.PushOnCollide);
    }

    private void OnSaintInteractHand(EntityUid uid, SaintedComponent component, InteractHandEvent args)
    {
        if (args.Handled)
            return;

        var ev = new OnSaintEntityHandInteract(component.DamageOnCollide, component.PushOnCollide);
        RaiseLocalEvent(args.User, ev);

        if (!ev.Handled)
            return;

        args.Handled = true;
        CauseHolyDamage(uid, args.User, ev.DamageOnCollide, ev.PushOnCollide);
    }

    private void OnSaintSilverPickedUpAttemptEvent(EntityUid uid, SaintSilverComponent component, GettingPickedUpAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        var ev = new OnSilverEntityTryPickedUp(component.DamageOnCollide, component.PushOnCollide);
        RaiseLocalEvent(args.User, ev);

        if (!ev.Handled)
            return;

        args.Cancel();
        CauseHolyDamage(uid, args.User, ev.DamageOnCollide, ev.PushOnCollide);
    }

    private void OnSaintPickedUpAttemptEvent(EntityUid uid, SaintedComponent component, GettingPickedUpAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        var ev = new OnSaintEntityTryPickedUp(component.DamageOnCollide, component.PushOnCollide);
        RaiseLocalEvent(args.User, ev);

        if (!ev.Handled)
            return;

        args.Cancel();
        CauseHolyDamage(uid, args.User, ev.DamageOnCollide, ev.PushOnCollide);
    }

    private void OnAfterInteractSilver(EntityUid uid, SaintSilverComponent component, AfterInteractEvent args)
    {
        OnAfterInteractEvent(uid, args, () => new OnSilverEntityAfterInteract(component.DamageOnCollide, component.PushOnCollide));
    }

    private void OnAfterInteract(EntityUid uid, SaintedComponent component, AfterInteractEvent args)
    {
        OnAfterInteractEvent(uid, args, () => new OnSaintEntityAfterInteract(component.DamageOnCollide, component.PushOnCollide));
    }

    private void OnAfterInteractEvent(EntityUid uid, InteractEvent args, Func<object> eventProvider)
    {
        var target = args.Target;
        if (args.Handled || target == null)
            return;

        if (!TryComp<UseDelayComponent>(uid, out var delay) || _useDelay.IsDelayed((uid, delay)))
            return;

        var ev = eventProvider();
        RaiseLocalEvent(target.Value, ev);

        if (ev is not ISaintEntityEvent {IsHandled: true} saintEv)
            return;

        _useDelay.TryResetDelay((uid, delay));
        CauseHolyDamage(uid, target.Value, saintEv.DamageOnCollide, saintEv.PushOnCollide);
        args.Handled = true;
    }

    private void OnCollideSilver(EntityUid uid, SaintSilverComponent component, ref StartCollideEvent args)
    {
        OnCollideEvent(uid, ref args, () => new OnSilverEntityCollide(component.DamageOnCollide, component.PushOnCollide));
    }

    private void OnCollideStart(EntityUid uid, SaintedComponent component, ref StartCollideEvent args)
    {
        OnCollideEvent(uid, ref args, () => new OnSaintEntityCollide(component.DamageOnCollide, component.PushOnCollide));
    }

    private void OnCollideEvent(EntityUid uid, ref StartCollideEvent args, Func<object> eventProvider)
    {
        var target = args.OtherEntity;
        var ev = eventProvider();
        RaiseLocalEvent(target, ev);

        if (ev is not ISaintEntityEvent {IsHandled: true} saintEv)
            return;

        CauseHolyDamage(uid, target, saintEv.DamageOnCollide, saintEv.PushOnCollide);
    }

    private void CauseHolyDamage(EntityUid uid, EntityUid target, DamageSpecifier damageSpecifier, bool pushOnCollide)
    {
        _damageable.TryChangeDamage(target, damageSpecifier);

        if (!pushOnCollide)
            return;

        var fieldDir = _transformSystem.GetWorldPosition(uid);
        var playerDir = _transformSystem.GetWorldPosition(target);

        _throwing.TryThrow(target, playerDir - fieldDir, baseThrowSpeed: 25);
    }

    public bool TryMakeSainted(EntityUid user, EntityUid uid)
    {
        var meta = MetaData(uid);
        if (HasComp<SaintedComponent>(uid))
        {
            _popupSystem.PopupEntity($"{meta.EntityName} уже освящен", uid, user);
            return false;
        }

        if (!TryComp<SaintableComponent>(uid, out var saintable))
        {
            _popupSystem.PopupEntity($"{meta.EntityName} не может быть освящен", uid, user);
            return false;
        }

        MakeSainted(user, uid, saintable, meta);
        return true;
    }

    private void MakeSainted(EntityUid user, EntityUid uid, SaintableComponent saintable, MetaDataComponent meta)
    {
        var saintedComponent = EnsureComp<SaintedComponent>(uid);
        saintedComponent.DamageOnCollide = saintable.DamageOnCollide;
        saintedComponent.PushOnCollide = saintable.PushOnCollide;

        var ev = new OnItemSainted();
        RaiseLocalEvent(uid, ev);

        _popupSystem.PopupEntity($"Вы освятили {meta.EntityName}", uid, user);
    }
}
