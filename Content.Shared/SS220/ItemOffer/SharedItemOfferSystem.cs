// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Components;
using Content.Shared.SS220.StuckOnEquip;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.ItemOffer;

public abstract class SharedItemOfferSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    private static readonly SpriteSpecifier OfferIcon = new SpriteSpecifier.Texture(new("/Textures/SS220/Interface/VerbIcons/present.png"));

    public override void Initialize()
    {
        SubscribeLocalEvent<HandsComponent, GetVerbsEvent<EquipmentVerb>>(AddOfferVerb);
    }

    private void AddOfferVerb(Entity<HandsComponent> ent, ref GetVerbsEvent<EquipmentVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var item = _hands.GetActiveItem(args.User);

        if (item == null)
            return;

        if (HasComp<UnremoveableComponent>(item))
            return;

        if (TryComp<StuckOnEquipComponent>(item, out var equip) && equip.IsStuck)//not sure it should be here ot in a momemt of transfering
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
