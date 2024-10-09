using System.Linq;
using Content.Server.SS220.DarkForces.Narsi.Progress.Objectives.Offering;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.Rejuvenate;
using Robust.Shared.Player;
using NarsiReviveRuneComponent = Content.Server.SS220.DarkForces.Narsi.Runes.Components.NarsiReviveRuneComponent;
using Content.Shared.SS220.DarkForces.Narsi.Runes;

namespace Content.Server.SS220.DarkForces.Narsi.Runes;

public sealed partial class NarsiRuneSystem
{
    private int _runesCharge;

    private void InitReviveRune()
    {
        SubscribeLocalEvent<NarsiReviveRuneComponent, ReviveNarsiRuneDoAfterEvent>(OnNarsiRuneDoAfter);
        SubscribeLocalEvent<HumanoidAppearanceComponent, NarsiCultOfferingTargetEvent>(OnOfferingEvent);
    }

    private void OnOfferingEvent(EntityUid uid, HumanoidAppearanceComponent component, NarsiCultOfferingTargetEvent args)
    {
        _runesCharge = 0;
    }

    private void OnRoundEndedReviveRune()
    {
        _runesCharge = 0;
    }

    private void OnNarsiRuneDoAfter(EntityUid uid, NarsiReviveRuneComponent component, ReviveNarsiRuneDoAfterEvent args)
    {
        if (args.Handled || args.Target == null || args.Cancelled)
        {
            HandleRuneUsed(uid, false);
            return;
        }

        HandleRuneUsed(uid, false);
        RaiseLocalEvent((EntityUid)args.Target, new RejuvenateEvent());

        args.Handled = true;
        _runesCharge -= 1;
        _audioSystem.PlayEntity(RuneSound, Filter.Pvs(args.User, entityManager: EntityManager), args.User, true, RuneSound.Params);
    }

    private void TryToRevive(EntityUid rune, EntityUid user)
    {
        if (_runesCharge <= 0)
        {
            _popupSystem.PopupEntity("Недостаточно заряда... Принесите жертву на руне предложения", rune);
            return;
        }

        if (HandleRuneInUse(rune))
            return;

        var entitiesInRange = FindCultistsNearRune(rune)
            .Where(uid => _mobStateSystem.IsDead(uid))
            .ToList();

        if (!entitiesInRange.Any())
            return;

        var target = entitiesInRange.First();

        var doAfterEvent = new ReviveNarsiRuneDoAfterEvent();
        var doAfterEventArgs = new DoAfterArgs(
            EntityManager,
            user: user,
            delay: TimeSpan.FromSeconds(3),
            @event: doAfterEvent,
            eventTarget: rune,
            target: target,
            used: rune
        );

        StartDoAfter(doAfterEventArgs);
        HandleRuneUsed(rune, true);
    }
}
