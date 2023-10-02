// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.DoAfter;
using Content.Shared.Pulling;
using Content.Shared.Pulling.Components;
using Content.Shared.SS220.Cart.Components;
using Content.Shared.Vehicle.Components;
using Content.Shared.Verbs;

namespace Content.Shared.SS220.Cart;

public sealed class CartSystem : EntitySystem
{
    [Dependency] private readonly SharedPullingSystem _pulling = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CartComponent, GetVerbsEvent<Verb>>(AddCartVerbs);
        SubscribeLocalEvent<CartComponent, CartAttachDoAfterEvent>(OnAttachDoAfter);
        SubscribeLocalEvent<CartComponent, CartDeattachDoAfterEvent>(OnDeattachDoAfter);
    }

    private void OnAttachDoAfter(EntityUid uid, CartComponent component, CartAttachDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (!TryComp<VehicleComponent>(args.AttachTarget, out var vehicle))
            return;

        if (!TryComp<SharedPullableComponent>(uid, out var pullable))
            return;

        // So here we are adding the puller component to the vehicle
        // in order to pull the cart with it.
        // We are later removing this component from vehicle.
        // This was made just because I wanted to reuse pulling system for this task.
        var puller = EnsureComp<SharedPullerComponent>(args.AttachTarget);
        _pulling.TryStopPull(pullable);
        _pulling.TryStartPull(args.AttachTarget, uid);

        component.Puller = args.AttachTarget;
        component.IsAttached = true;
        Dirty(component);
        args.Handled = true;
    }

    private void OnDeattachDoAfter(EntityUid uid, CartComponent component, CartDeattachDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (!TryComp<VehicleComponent>(args.DeattachTarget, out var vehicle))
            return;

        if (!TryComp<SharedPullableComponent>(uid, out var pullable))
            return;

        _pulling.TryStopPull(pullable);
        RemComp<SharedPullerComponent>(args.DeattachTarget);

        component.Puller = null;
        component.IsAttached = false;
        Dirty(component);
        args.Handled = true;
    }

    private void AddCartVerbs(EntityUid uid, CartComponent component, GetVerbsEvent<Verb> args)
    {

    }

    public bool TryAttachCart(EntityUid target, CartComponent cartComp, EntityUid user)
    {
        if (!TryComp<VehicleComponent>(target, out var vehicle))
            return false;

        if (!TryComp<SharedPullableComponent>(cartComp.Owner, out var pullable))
            return false;

        var doAfterEventArgs = new DoAfterArgs(EntityManager, user, cartComp.AttachToggleTime, new CartAttachDoAfterEvent(target),
            cartComp.Owner, target: cartComp.Owner)
        {
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
            BreakOnDamage = true,
            NeedHand = true
        };
        _doAfter.TryStartDoAfter(doAfterEventArgs);
        return true;
    }

    public bool TryDeattachCart(EntityUid target, CartComponent cartComp, EntityUid user)
    {
        if (!TryComp<VehicleComponent>(target, out var vehicle))
            return false;

        if (!TryComp<SharedPullerComponent>(target, out var puller))
            return false;

        if (!TryComp<SharedPullableComponent>(cartComp.Owner, out var pullable))
            return false;

        var doAfterEventArgs = new DoAfterArgs(EntityManager, user, cartComp.AttachToggleTime, new CartDeattachDoAfterEvent(target),
            cartComp.Owner, target: cartComp.Owner)
        {
            BreakOnTargetMove = true,
            BreakOnUserMove = true,
            BreakOnDamage = true,
            NeedHand = true
        };
        _doAfter.TryStartDoAfter(doAfterEventArgs);
        return true;
    }
}
