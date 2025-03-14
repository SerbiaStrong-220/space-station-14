using System.Linq;
using Content.Server.Popups;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.SS220.Tarot;

public sealed class TarotColodeSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    private const int MaxCardsInWorld = 3; // only n cards in world

    public override void Initialize()
    {
        SubscribeLocalEvent<TarotColodeComponent, UseInHandEvent>(OnUseInHand);
    }

    private void OnUseInHand(Entity<TarotColodeComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.CardsProtoIds.Count == 0)
            return;

        if (EntityQuery<TarotCardComponent>().Count() >= MaxCardsInWorld)
        {
            _popup.PopupEntity(Loc.GetString("tarot-cards-failed-more-then-three"), args.User, args.User);
            return;
        }

        var randomCard = Spawn(_random.Pick(ent.Comp.CardsProtoIds), Transform(args.User).Coordinates);

        _hands.PickupOrDrop(args.User, randomCard);
        args.Handled = true;
    }
}
