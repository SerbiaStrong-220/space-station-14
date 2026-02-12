// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Verbs;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.ClinkGlasses;

public abstract partial class SharedClinkGlassesSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    private static readonly SpriteSpecifier VerbIcon = new SpriteSpecifier.Texture(new("/Textures/SS220/Interface/VerbIcons/glass-celebration.png"));

    public override void Initialize()
    {
        SubscribeLocalEvent<ClinkGlassesComponent, GetVerbsEvent<Verb>>(OnVerb);
        SubscribeLocalEvent<ClinkGlassesComponent, GotEquippedHandEvent>(OnGotEquippedHand);
        SubscribeLocalEvent<ClinkGlassesComponent, GotUnequippedHandEvent>(OnGotUnequippedHand);
        SubscribeLocalEvent<ClinkGlassesInitiatorComponent, GetVerbsEvent<AlternativeVerb>>(OnInitiatorAlternativeVerb);
    }


    private void OnVerb(Entity<ClinkGlassesComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (!TryComp<ClinkGlassesInitiatorComponent>(args.User, out var comp))
            return;

        if (_gameTiming.CurTime < comp.NextClinkTime)
            return;

        var user = args.User;
        var verb = new Verb
        {
            Text = Loc.GetString("raise-glass-verb-text"),
            Act = () =>
            {
                DoRaiseGlass(user, ent.Owner);
            },
            Icon = VerbIcon,
        };

        args.Verbs.Add(verb);
    }

    private void OnGotEquippedHand(Entity<ClinkGlassesComponent> ent, ref GotEquippedHandEvent args)
    {
        if (!_gameTiming.IsFirstTimePredicted)
            return;

        EnsureComp<ClinkGlassesInitiatorComponent>(args.User, out var comp);
        comp.Items.Add(ent);
    }

    private void OnGotUnequippedHand(Entity<ClinkGlassesComponent> ent, ref GotUnequippedHandEvent args)
    {
        if (!_gameTiming.IsFirstTimePredicted)
            return;

        if (!TryComp<ClinkGlassesInitiatorComponent>(args.User, out var comp))
            return;

        comp.Items.Remove(ent);

        if (comp.Items.Count == 0)
            RemComp<ClinkGlassesInitiatorComponent>(args.User);
    }

    private void OnInitiatorAlternativeVerb(Entity<ClinkGlassesInitiatorComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (!TryComp<ClinkGlassesInitiatorComponent>(args.User, out var comp))
            return;

        if (_gameTiming.CurTime < comp.NextClinkTime)
            return;

        if (args.User == args.Target)
            return;

        if (!_hands.TryGetActiveItem(args.User, out var itemInHand) || !HasComp<ClinkGlassesComponent>(itemInHand))
            return;

        var initiator = args.User;
        var receiver = args.Target;
        var verb = new AlternativeVerb
        {
            Text = Loc.GetString("clink-glasses-verb-text"),
            Act = () =>
            {
                DoClinkGlassesOffer(initiator, receiver, itemInHand.Value);
            },
            Icon = VerbIcon,
        };

        args.Verbs.Add(verb);
    }

    protected abstract void DoRaiseGlass(EntityUid initiator, EntityUid item);
    protected abstract void DoClinkGlassesOffer(EntityUid initiator, EntityUid receiver, EntityUid item);
}
