// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Physics.Pull;
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
        SubscribeLocalEvent<CartComponent, StopPullingEvent>(OnStopPull);
        SubscribeLocalEvent<CartComponent, PullAttemptEvent>(OnPullAttempt);
        SubscribeLocalEvent<CartComponent, CanDragEvent>(OnCanDrag);
        SubscribeLocalEvent<CartComponent, CanDropDraggedEvent>(OnCanDropDragged);
    }

    private void OnCanDrag(EntityUid uid, CartComponent component, ref CanDragEvent args)
    {
        // Can't drag cart if it's already attached
        if (!component.IsAttached)
            args.Handled = true;
    }

    private void OnCanDropDragged(EntityUid uid, CartComponent component, ref CanDropDraggedEvent args)
    {
        if (TryComp<VehicleComponent>(args.Target, out var vehicleComp))
            args.Handled = true;
    }

    private void OnPullAttempt(EntityUid uid, CartComponent component, PullAttemptEvent args)
    {
        // Have to additionaly do it to make it prettier
        if (component.IsAttached)
            args.Cancelled = true;
    }

    private void OnStopPull(EntityUid uid, CartComponent component, StopPullingEvent args)
    {
        // Cancel pull stop if the cart is attached,
        // so you have to properly deattach it first.
        if (component.IsAttached)
            args.Cancel();
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

        var ev = new CartAttachEvent(args.AttachTarget, uid);
        RaiseLocalEvent(args.AttachTarget, ref ev);

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

        var ev = new CartDeattachEvent(args.DeattachTarget, uid);
        RaiseLocalEvent(args.DeattachTarget, ref ev);

        component.Puller = null;
        component.IsAttached = false;
        Dirty(component);
        args.Handled = true;
    }

    private void AddCartVerbs(EntityUid uid, CartComponent component, GetVerbsEvent<Verb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (!component.IsAttached)
            return;

        Verb verb = new()
        {
            Text = Loc.GetString("cart-verb-deattach"),
            Act = () => TryDeattachCart(component, args.User),
            DoContactInteraction = false
        };
        args.Verbs.Add(verb);
    }

    public bool TryAttachCart(EntityUid target, CartComponent cartComp, EntityUid user)
    {
        if (cartComp.IsAttached)
            return false;

        if (!TryComp<VehicleComponent>(target, out var vehicle))
            return false;

        if (vehicle.AttachedCart.HasValue)
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

    public bool TryDeattachCart(CartComponent cartComp, EntityUid user)
    {
        if (!cartComp.IsAttached)
            return false;

        var target = cartComp.Puller;
        if (target == null)
            return false;

        return TryDeattachCart((EntityUid) target, cartComp, user);
    }

    public bool TryDeattachCart(EntityUid target, CartComponent cartComp, EntityUid? user)
    {
        if (!cartComp.IsAttached)
            return false;

        if (!TryComp<VehicleComponent>(target, out var vehicle))
            return false;

        if (!TryComp<SharedPullerComponent>(target, out var puller))
            return false;

        if (!TryComp<SharedPullableComponent>(cartComp.Owner, out var pullable))
            return false;

        if (user == null)
        {
            // Disconnect the cart by force
            ForceDeattach(target, cartComp);
            return true;
        }

        var doAfterEventArgs = new DoAfterArgs(EntityManager, (EntityUid) user, cartComp.AttachToggleTime, new CartDeattachDoAfterEvent(target),
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

    private void ForceDeattach(EntityUid target, CartComponent cartComp)
    {
        if (!TryComp<VehicleComponent>(target, out var vehicle))
            return;

        if (!TryComp<SharedPullableComponent>(cartComp.Owner, out var pullable))
            return;

        _pulling.TryStopPull(pullable);
        RemComp<SharedPullerComponent>(target);

        var ev = new CartDeattachEvent(target, cartComp.Owner);
        RaiseLocalEvent(target, ref ev);

        cartComp.Puller = null;
        cartComp.IsAttached = false;
        Dirty(cartComp);
    }
}
