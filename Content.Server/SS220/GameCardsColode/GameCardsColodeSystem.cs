using Content.Server.Popups;
using Content.Shared.Storage;
using Content.Shared.Verbs;
using Robust.Server.Containers;
using Robust.Shared.Random;

namespace Content.Server.SS220.GameCardsColode;

public sealed class GameCardsColodeSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GameCardsColodeComponent, GetVerbsEvent<Verb>>(OnVerb);
    }

    private void OnVerb(Entity<GameCardsColodeComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanInteract || !TryComp<StorageComponent>(ent.Owner, out var storage))
            return;

        if (storage.Container.ContainedEntities.Count == 0)
            return;

        var user = args.User;

        var verb = new Verb
        {
            Text = Loc.GetString("verb-shuffle-cards"),
            Act = () => ShuffleDeck(ent.Owner, user),
        };

        args.Verbs.Add(verb);
    }

    private void ShuffleDeck(EntityUid deck, EntityUid user)
    {
        if (!TryComp<StorageComponent>(deck, out var storage))
            return;

        var deckCards = _container.EmptyContainer(storage.Container);
        _random.Shuffle(deckCards);

        foreach (var card in deckCards)
        {
            _container.Insert(card, storage.Container);
        }

        _popup.PopupEntity(Loc.GetString("cards-shuffled"), user);
    }
}
