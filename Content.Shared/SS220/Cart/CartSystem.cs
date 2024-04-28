// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.DoAfter;
using Content.Shared.DragDrop;
using Content.Shared.Foldable;
using Content.Shared.Friction;
using Content.Shared.Item;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Pulling.Events;
using Content.Shared.SS220.Cart.Components;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Cart;

public sealed class CartSystem : EntitySystem
{
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly TileFrictionController _tileFriction = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CartComponent, GetVerbsEvent<InteractionVerb>>(AddCartVerbs);
        SubscribeLocalEvent<CartComponent, CartAttachDoAfterEvent>(OnAttachDoAfter);
        SubscribeLocalEvent<CartComponent, CartDeattachDoAfterEvent>(OnDeattachDoAfter);
        SubscribeLocalEvent<CartComponent, AttemptStopPullingEvent>(OnStopPull);
        SubscribeLocalEvent<CartComponent, PullAttemptEvent>(OnPullAttempt);
        SubscribeLocalEvent<CartComponent, FoldAttemptEvent>(OnFoldAttempt);
        SubscribeLocalEvent<CartComponent, ComponentShutdown>(OnShutdown);
        //SubscribeLocalEvent<CartComponent, CanDragEvent>(OnCanDrag);
        //SubscribeLocalEvent<CartComponent, CanDropDraggedEvent>(OnCanDropDragged);
    }

    private void OnShutdown(EntityUid uid, CartComponent component, ComponentShutdown args)
    {
        if (!component.IsAttached)
            return;

        TryDeattachCart((uid, component), null);
    }

    private void OnFoldAttempt(EntityUid uid, CartComponent component, ref FoldAttemptEvent args)
    {
        if (!component.IsAttached)
            return;

        args.Cancelled = true;
    }

    //private void OnCanDrag(EntityUid uid, CartComponent component, ref CanDragEvent args)
    //{
    //    // Can't drag cart if it's already attached
    //    if (!component.IsAttached)
    //        args.Handled = true;
    //}

    //private void OnCanDropDragged(EntityUid uid, CartComponent component, ref CanDropDraggedEvent args)
    //{
    //    if (!HasComp<CartPullerComponent>(args.Target))
    //        return;

    //    args.CanDrop = true;
    //    args.Handled = true;
    //}

    private void OnPullAttempt(EntityUid uid, CartComponent component, PullAttemptEvent args)
    {
        // Here I'm trying to make that you possibly could make a
        // infinite "snake" of cart pullers if the entity has
        // both a CartComponent and a CartPullerComponent.
        if (args.PullerUid == uid)
            return;

        if (!component.IsAttached)
            return;

        args.Cancelled = true;
    }

    private void OnStopPull(EntityUid uid, CartComponent component, AttemptStopPullingEvent args)
    {
        // Cancel pull stop if the cart is attached,
        // so you have to properly deattach it first.
        if (component.IsAttached)
            args.Cancelled = true;
    }

    private void OnAttachDoAfter(EntityUid uid, CartComponent component, CartAttachDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (!HasComp<CartPullerComponent>(args.AttachTarget))
            return;

        if (!TryComp<PullableComponent>(uid, out var pullable))
            return;

        // So here we are adding the puller component to the cart puller
        // in order to pull the cart with it.
        // We are later removing this component from the cart puller.
        // This was made just because I wanted to reuse pulling system for this task.
        EnsureComp<PullerComponent>(args.AttachTarget);
        _pulling.TryStopPull(uid, pullable);
        if (!_pulling.TryStartPull(args.AttachTarget, uid))
            return;

        // This is the simpliest way to change pulling speed I could've imagined.
        // So, if the cart has a ItemComponent we just take it's size and divide it by 166, if not - take the default 0.15 value.
        var frictionModifierComp = EnsureComp<TileFrictionModifierComponent>(uid);
        float frictionModifier = .15f;
        if (TryComp<ItemComponent>(uid, out var itemComp))
        {
            if (_prototype.TryIndex(itemComp.Size, out var itemSize))
            {
                frictionModifier = itemSize.Weight / 26.6f;
            }
        }
        _tileFriction.SetModifier(uid, frictionModifier, frictionModifierComp);

        var ev = new CartAttachEvent(args.AttachTarget, uid);
        RaiseLocalEvent(args.AttachTarget, ref ev);

        component.Puller = args.AttachTarget;
        component.IsAttached = true;
        Dirty(uid, component);
        args.Handled = true;
    }

    private void OnDeattachDoAfter(EntityUid uid, CartComponent component, CartDeattachDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (!TryComp<PullableComponent>(uid, out var pullable))
            return;

        _pulling.TryStopPull(uid, pullable);
        RemComp<PullerComponent>(args.DeattachTarget);
        RemComp<TileFrictionModifierComponent>(uid);

        var ev = new CartDeattachEvent(args.DeattachTarget, uid);
        RaiseLocalEvent(args.DeattachTarget, ref ev);

        component.Puller = null;
        component.IsAttached = false;
        Dirty(uid, component);
        args.Handled = true;
    }

    private void AddCartVerbs(EntityUid uid, CartComponent component, GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (!component.IsAttached)
            return;

        InteractionVerb verb = new()
        {
            Text = Name(uid),
            Act = () => TryDeattachCart((uid, component), args.User),
            Category = VerbCategory.DeattachCart,
            // Prioritize deattaching itself
            Priority = 1
        };
        args.Verbs.Add(verb);
    }

    public bool TryAttachCart(EntityUid target, CartComponent cartComp, EntityUid user)
    {
        if (!IsAttachable(target, cartComp.Owner))
            return false;

        var doAfterEventArgs = new DoAfterArgs(EntityManager, user, cartComp.AttachToggleTime, new CartAttachDoAfterEvent(target),
            cartComp.Owner, target: cartComp.Owner)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true
        };
        _doAfter.TryStartDoAfter(doAfterEventArgs);
        return true;
    }

    public bool TryDeattachCart(Entity<CartComponent> cart, EntityUid? user)
    {
        if (!cart.Comp.IsAttached)
            return false;

        var target = cart.Comp.Puller;
        if (!target.HasValue)
            return false;

        return TryDeattachCart(target.Value, cart, user);
    }

    public bool TryDeattachCart(EntityUid target, Entity<CartComponent> cart, EntityUid? user)
    {
        if (!cart.Comp.IsAttached)
            return false;

        if (!HasComp<CartPullerComponent>(target))
            return false;

        if (!HasComp<PullerComponent>(target))
            return false;

        if (!HasComp<PullableComponent>(cart))
            return false;

        if (user == null)
        {
            // Disconnect the cart by force
            ForceDeattach(target, cart);
            return true;
        }

        var doAfterEventArgs = new DoAfterArgs(EntityManager, (EntityUid) user, cart.Comp.AttachToggleTime, new CartDeattachDoAfterEvent(target),
            cart, target: cart)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true
        };
        _doAfter.TryStartDoAfter(doAfterEventArgs);
        return true;
    }

    private void ForceDeattach(EntityUid target, Entity<CartComponent> cart)
    {
        if (!HasComp<CartPullerComponent>(target))
            return;

        if (!TryComp<PullableComponent>(cart, out var pullable))
            return;

        _pulling.TryStopPull(cart, pullable);
        RemComp<PullerComponent>(target);

        var ev = new CartDeattachEvent(target, cart);
        RaiseLocalEvent(target, ref ev);

        cart.Comp.Puller = null;
        cart.Comp.IsAttached = false;
        Dirty(cart);
    }

    /// <summary>
    /// Returns true if the cart attachable to the target
    /// </summary>
    /// <param name="target">Target for the cart to attach to</param>
    /// <param name="cart"></param>
    /// <returns></returns>
    public bool IsAttachable(EntityUid target, EntityUid cart)
    {
        // God have mercy on me for all of this
        // Return if trying to attach to themselves
        if (target == cart)
            return false;

        if (!TryComp<CartComponent>(cart, out var cartComp) || !TryComp<CartPullerComponent>(target, out var cartPullerComp))
            return false;

        if (cartComp.IsAttached)
            return false;

        if (cartPullerComp.AttachedCart.HasValue)
            return false;

        if (!HasComp<PullableComponent>(cart))
            return false;

        // Prevent folded entities from attaching
        if (TryComp<FoldableComponent>(cart, out var foldableComp) && foldableComp.IsFolded)
            return false;

        return true;
    }
}
