using Content.Server.SS220.DarkForces.Narsi.Cultist.Abilities.Silence;
using Content.Server.SS220.DarkForces.Saint.Reagent.Events;
using Content.Server.SS220.DarkForces.Saint.Saintable.Events;
using Content.Shared.SS220.DarkForces.Narsi.Abilities.Events;
using Content.Shared.SS220.DarkForces.Narsi.Cultist.Muzzle;
using Content.Shared.SS220.DarkForces.Narsi.Roles;
using Content.Shared.Speech.Muting;

namespace Content.Server.SS220.DarkForces.Narsi.Cultist.Abilities;

public sealed partial class NarsiCultistAbilitiesSystem
{
    private void InitializeSilence()
    {
        SubscribeLocalEvent<NarsiCultistComponent, NarsiCultistSilenceEvent>(OnSilenceEvent);
        SubscribeLocalEvent<NarsiSilenceComponent, OnSaintEntityAfterInteract>(OnSaintEntityContact);
        SubscribeLocalEvent<NarsiSilenceComponent, OnSaintEntityCollide>(OnSaintEntityContact);
        SubscribeLocalEvent<NarsiSilenceComponent, OnSaintWaterDrinkEvent>(OnSaintWaterDrink);
    }

    private void OnSaintWaterDrink(EntityUid uid, NarsiSilenceComponent component, OnSaintWaterDrinkEvent args)
    {
        ClearSilence(uid);
    }

    private void OnSaintEntityContact(EntityUid uid, NarsiSilenceComponent component, ISaintEntityEvent args)
    {
        ClearSilence(uid);
    }

    private void ClearSilence(EntityUid uid)
    {
        _appearanceSystem.SetData(uid, NarsiCultistMuzzleStatus.Status, NarsiCultistMuzzleState.Empty);

        RemComp<NarsiSilenceComponent>(uid);
        RemComp<MutedComponent>(uid);
        RemComp<NarsiCultistMuzzleVisualizerComponent>(uid);
    }

    private void OnSilenceEvent(EntityUid uid, NarsiCultistComponent component, NarsiCultistSilenceEvent args)
    {
        if (args.Handled)
            return;

        var target = args.Target;
        var level = _progressSystem.GetAbilityLevel(SilenceAction);
        var duration = level switch
        {
            1 => 10,
            2 => 20,
            _ => 30
        };
        var silenceComponent = EnsureComp<NarsiSilenceComponent>(target);
        silenceComponent.TickToRemove = _timing.CurTime + TimeSpan.FromSeconds(duration);

        EnsureComp<MutedComponent>(target);
        EnsureComp<NarsiCultistMuzzleVisualizerComponent>(target);

        _appearanceSystem.SetData(target, NarsiCultistMuzzleStatus.Status, NarsiCultistMuzzleState.Muzzle);
        OnCultistAbility(uid, args);

        args.Handled = true;
    }

    private void UpdateSilence()
    {
        var query = EntityQueryEnumerator<NarsiSilenceComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.TickToRemove > _timing.CurTime)
                continue;

            ClearSilence(uid);
        }
    }
}
