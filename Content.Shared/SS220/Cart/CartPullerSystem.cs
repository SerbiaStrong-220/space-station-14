// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Verbs;
using Content.Shared.Pulling.Components;
using Content.Shared.SS220.Cart.Components;
using Content.Shared.DragDrop;

namespace Content.Shared.SS220.Cart;

public sealed partial class CartPullerSystem : EntitySystem
{
    [Dependency] private readonly CartSystem _cart = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CartPullerComponent, GetVerbsEvent<Verb>>(AddCartVerbs);
        SubscribeLocalEvent<CartPullerComponent, CanDropTargetEvent>(OnCanDrop);
        SubscribeLocalEvent<CartPullerComponent, DragDropTargetEvent>(OnDragDropTarget);
        SubscribeLocalEvent<CartPullerComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<CartPullerComponent, CartAttachEvent>(OnAttachCart);
        SubscribeLocalEvent<CartPullerComponent, CartDeattachEvent>(OnDeattachCart);
    }

    private void OnShutdown(EntityUid uid, CartPullerComponent component, ComponentShutdown args)
    {
        if (!component.AttachedCart.HasValue)
            return;

        if (!TryComp<CartComponent>(component.AttachedCart, out var cartComp))
            return;

        _cart.TryDeattachCart(uid, cartComp, null);
    }

    private void OnCanDrop(EntityUid uid, CartPullerComponent component, ref CanDropTargetEvent args)
    {
        if (!component.AttachedCart.HasValue)
            args.Handled = true;
    }

    private void OnDragDropTarget(EntityUid uid, CartPullerComponent component, ref DragDropTargetEvent args)
    {
        // Cart drag-drop attaching
        if (!TryComp<CartComponent>(args.Dragged, out var cartComp))
            return;

        _cart.TryAttachCart(uid, cartComp, args.User);
        args.Handled = true;
    }

    private void AddCartVerbs(EntityUid uid, CartPullerComponent component, GetVerbsEvent<Verb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (component.AttachedCart.HasValue)
        {
            // If cart puller already have an attached cart - add verb to deattach it
            if (!TryComp<CartComponent>(component.AttachedCart, out var attachedCart))
                return;

            Verb deattachVerb = new()
            {
                Text = Loc.GetString("cart-verb-deattach-attached-cart"),
                Act = () => _cart.TryDeattachCart(attachedCart, args.User),
                DoContactInteraction = false
            };
            args.Verbs.Add(deattachVerb);
            return;
        }

        if (!TryComp<SharedPullerComponent>(args.User, out var userPullerComp))
            return;

        var cart = userPullerComp.Pulling;
        // If trying to attach themselves - return
        if (cart == uid)
            return;

        // If not pulling entity with cart component - return
        if (!TryComp<CartComponent>(cart, out var cartComp))
            return;

        Verb verb = new()
        {
            Text = Loc.GetString("cart-verb-attach"),
            Act = () => _cart.TryAttachCart(uid, cartComp, args.User),
            DoContactInteraction = false
        };
        args.Verbs.Add(verb);
    }

    private void OnDeattachCart(EntityUid uid, CartPullerComponent component, ref CartDeattachEvent args)
    {
        component.AttachedCart = null;
        Dirty(component);
    }

    private void OnAttachCart(EntityUid uid, CartPullerComponent component, ref CartAttachEvent args)
    {
        component.AttachedCart = args.Attaching;
        Dirty(component);
    }
}
