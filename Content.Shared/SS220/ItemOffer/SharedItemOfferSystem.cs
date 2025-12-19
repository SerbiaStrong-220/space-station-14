// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.ItemOffer;

public abstract partial class SharedItemOfferSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    private static readonly SpriteSpecifier OfferIcon = new SpriteSpecifier.Texture(new("/Textures/SS220/Interface/VerbIcons/present.png"));

    public override void Initialize()
    {
        InitializeRestrictions();

        SubscribeLocalEvent<HandsComponent, GetVerbsEvent<EquipmentVerb>>(AddOfferVerb);
    }

    private void AddOfferVerb(Entity<HandsComponent> ent, ref GetVerbsEvent<EquipmentVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var item = _hands.GetActiveItem(args.User);

        if (item == null)
            return;

        var evItem = new CanOfferItemEvent(args.User, args.Target);
        RaiseLocalEvent(item.Value, ref evItem, true);

        if (evItem.Cancelled)
            return;

        var evUser = new CanOfferItemEvent(args.User, args.Target);
        RaiseLocalEvent(args.User, ref evUser, true);

        if (evUser.Cancelled)
            return;

        var user = args.User;
        var verb = new EquipmentVerb
        {
            Text = Loc.GetString("offer-verb-text"),
            Act = () =>
            {
                DoItemOffer(user, ent.Owner);
            },
            Icon = OfferIcon,
        };

        args.Verbs.Add(verb);
    }

    protected abstract void DoItemOffer(EntityUid user, EntityUid target);
}

/// <summary>
/// This event handle that this item can't be offered by users.
/// </summary>
/// <param name="User">User, that trade offer item</param>
/// <param name="TargetUser">User, that can take item</param>
/// <param name="Cancelled">If true, that means that item can't be offered</param>
[ByRefEvent]
public record struct CanOfferItemEvent(EntityUid User, EntityUid TargetUser, bool Cancelled = false);
