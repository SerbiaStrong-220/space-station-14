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
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<TarotColodeComponent, UseInHandEvent>(OnUseInHand);
    }


    private void OnUseInHand(Entity<TarotColodeComponent> ent, ref UseInHandEvent args)
    {
        var curTime = _gameTiming.CurTime;

        if (curTime < ent.Comp.NextUseTime)
        {
            _popup.PopupEntity("Колода пока не готова!", args.User, args.User);
            return;
        }

        if (EntityQuery<TarotCardComponent>().Count() >= 3)
        {
            _popup.PopupEntity("В мире уже 3 карты! Больше нельзя.", args.User, args.User);
            return;
        }

        _audio.PlayLocal(new SoundPathSpecifier("/Audio/Effects/unwrap.ogg"), ent.Owner, args.User);

        // Выбираем случайную карту
        var randomCard = _random.Pick(ent.Comp.CardsName);

        // Спавним карту рядом с пользователем
        var userCoords = Transform(args.User).Coordinates;
        var entity = Spawn(randomCard, userCoords);

        _hands.PickupOrDrop(args.User, entity);

        // Устанавливаем время следующего использования
        ent.Comp.NextUseTime = curTime + TimeSpan.FromSeconds(25);
    }
}
