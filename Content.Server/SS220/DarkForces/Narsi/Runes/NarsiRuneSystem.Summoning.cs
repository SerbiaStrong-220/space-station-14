using System.Linq;
using Content.Server.SS220.DarkForces.Narsi.Runes.Components;
using Content.Shared.Cuffs.Components;
using Content.Shared.DoAfter;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.SS220.DarkForces.Narsi.Roles;
using Content.Shared.SS220.FastUI;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Content.Shared.SS220.DarkForces.Narsi.Runes;

namespace Content.Server.SS220.DarkForces.Narsi.Runes;

public sealed partial class NarsiRuneSystem
{
    [Dependency] private readonly PullingSystem _pullingSystem = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    private void InitSummoningRune()
    {
        SubscribeLocalEvent<NarsiSummonRuneComponent, SelectItemMessage>(OnSelectCultists);
        SubscribeLocalEvent<NarsiCultistComponent, SummonNarsiRuneDoAfterEvent>(OnSummonDoAfterEvent);
    }

    private void OnSelectCultists(EntityUid uid, NarsiSummonRuneComponent component, SelectItemMessage args)
    {
        if (args.Key != "NarsiSummonRune")
            return;

        var target = EntityUid.Parse(args.Data.ID);

        if (!HasComp<CuffableComponent>(target) && _pullingSystem.IsPulling(target))
        {
            _popupSystem.PopupEntity("Что-то мешает призвать культиста", uid);
            return;
        }

        var doAfterEvent = new SummonNarsiRuneDoAfterEvent();
        var doAfterEventArgs = new DoAfterArgs(
            EntityManager,
            user: args.Actor,
            delay: TimeSpan.FromSeconds(3),
            @event: doAfterEvent,
            eventTarget: args.Actor,
            target: target,
            used: uid
        );

        StartDoAfter(doAfterEventArgs);
    }

    private void OnSummonDoAfterEvent(EntityUid uid, NarsiCultistComponent component, SummonNarsiRuneDoAfterEvent args)
    {
        if (args.Handled || args.Target == null || args.Cancelled)
        {
            HandleRuneUsed(uid, false);
            return;
        }

        var target = args.Target ?? EntityUid.Invalid;

        _transformSystem.SetCoordinates(target, Transform(args.User).Coordinates);
        _transformSystem.AttachToGridOrMap(target);

        args.Handled = true;
        _audioSystem.PlayEntity(RuneSound, Filter.Pvs(args.User, entityManager: EntityManager), args.User, true, RuneSound.Params);
    }

    private void TryToSummond(EntityUid rune, EntityUid user)
    {
        if (!TryComp<ActorComponent>(user, out var actorComponent))
            return;

        var data = GetCultistsListing(user, EntityQueryEnumerator<NarsiCultistComponent>());
        if (!data.Any())
        {
            _popupSystem.PopupEntity("Нет культистов для призыва", rune);
            return;
        }

        if (!_ui.HasUi(rune, SecretListingKey.Key))
            return;

        var state = new SecretListingInitDataState("NarsiSummonRune", data, "Выберите культиста", "Если культист связан И его кто-то держит, призыв не получится", GetNetEntity(user));
        _ui.SetUiState(rune, SecretListingKey.Key, state);
    }

    private List<ListingData> GetCultistsListing(EntityUid user, EntityQueryEnumerator<NarsiCultistComponent> enumerator)
    {
        var data = new List<ListingData>();
        while (enumerator.MoveNext(out var cultist, out _))
        {
            if (cultist == user)
                continue;

            data.Add(
                new ListingData(
                    id: cultist.ToString(),
                    title: MetaData(cultist).EntityName,
                    description: "Верный слуга Нар'Си",
                    subDescription: "Призыв длится около трех секунд",
                    buttonText: "Призвать",
                    buttonState: ButtonState.Enabled
                )
            );
        }

        return data;
    }
}
