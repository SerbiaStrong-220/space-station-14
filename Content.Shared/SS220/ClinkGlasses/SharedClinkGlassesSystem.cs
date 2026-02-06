// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.ClinkGlasses;

public abstract partial class SharedClinkGlassesSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    private static readonly SpriteSpecifier VerbIcon = new SpriteSpecifier.Texture(new("/Textures/SS220/Interface/VerbIcons/glass-celebration.png"));

    public override void Initialize()
    {
        SubscribeLocalEvent<HandsComponent, GetVerbsEvent<Verb>>(AddClinkGlassesVerb);
    }

    private void AddClinkGlassesVerb(Entity<HandsComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var itemInitiator = _hands.GetActiveItem(args.User);

        if (itemInitiator == null || !HasComp<ClinkGlassesComponent>(itemInitiator))
            return;

        var itemReceiver = _hands.GetActiveItem(args.Target);

        if (itemReceiver == null || !HasComp<ClinkGlassesComponent>(itemReceiver))
            return;

        if (args.User == args.Target)
            return;

        var user = args.User;
        var target = args.Target;
        var verb = new Verb
        {
            Text = Loc.GetString("clink-glasses-verb-text"),
            Act = () =>
            {
                DoClinkGlassesOffer(user, target);
            },
            Icon = VerbIcon,
        };

        args.Verbs.Add(verb);
    }

    protected abstract void DoClinkGlassesOffer(EntityUid user, EntityUid target);
}
