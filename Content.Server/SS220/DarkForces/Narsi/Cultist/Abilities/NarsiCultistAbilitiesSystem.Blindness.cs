using Content.Server.SS220.DarkForces.Narsi.Cultist.Abilities.Blindness;
using Content.Server.SS220.DarkForces.Saint.Reagent.Events;
using Content.Server.SS220.DarkForces.Saint.Saintable.Events;
using Content.Shared.SS220.DarkForces.Narsi.Abilities.Events;
using Content.Shared.SS220.DarkForces.Narsi.Cultist.Blindness;
using Content.Shared.SS220.DarkForces.Narsi.Roles;
using Content.Shared.StatusEffect;

namespace Content.Server.SS220.DarkForces.Narsi.Cultist.Abilities;

public sealed partial class NarsiCultistAbilitiesSystem
{
    [Dependency] private readonly StatusEffectsSystem _statusEffectsSystem = default!;

    private void InitializeBlindness()
    {
        SubscribeLocalEvent<NarsiCultistComponent, NarsiCultistBlindnessEvent>(OnBlindnessEvent);
        SubscribeLocalEvent<NarsiBlindnessComponent, OnSaintWaterDrinkEvent>(OnSaintWaterDrinkBlindness);
        SubscribeLocalEvent<NarsiBlindnessComponent, OnSaintWaterFlammableEvent>(OnSaintWaterFlammableBlindness);
        SubscribeLocalEvent<NarsiBlindnessComponent, OnSaintEntityAfterInteract>(OnSaintAfterInteractBlindness);
    }

    private void OnSaintWaterDrinkBlindness(EntityUid uid, NarsiBlindnessComponent component, OnSaintWaterDrinkEvent args)
    {
        ClearBlindness(uid);
    }

    private void OnSaintWaterFlammableBlindness(EntityUid uid, NarsiBlindnessComponent component, OnSaintWaterFlammableEvent args)
    {
        ClearBlindness(uid);
    }

    private void OnSaintAfterInteractBlindness(EntityUid uid, NarsiBlindnessComponent component, OnSaintEntityAfterInteract args)
    {
        ClearBlindness(uid);
    }

    private void ClearBlindness(EntityUid uid)
    {
        _appearanceSystem.SetData(uid, NarsiCultistBlindnessStatus.Status, NarsiCultistBlindnessState.Empty);
        RemComp<NarsiBlindnessComponent>(uid);
        RemComp<NarsiCultistBlindnessVisualizeComponent>(uid);
        _statusEffectsSystem.TryRemoveStatusEffect(uid, "TemporaryBlindness");
    }

    private void UpdateBlindness()
    {
        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<NarsiBlindnessComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.TimeToRemove > curTime)
                continue;

            ClearBlindness(uid);
        }
    }

    private void OnBlindnessEvent(EntityUid uid, NarsiCultistComponent component, NarsiCultistBlindnessEvent args)
    {
        if (args.Handled)
            return;

        var target = args.Target;
        var level = _progressSystem.GetAbilityLevel(BlindnessAction);

        var blindnessTime = level switch
        {
            1 => 10,
            2 => 20,
            _ => 30
        };

        _statusEffectsSystem.TryAddStatusEffect(target, "TemporaryBlindness", TimeSpan.FromSeconds(blindnessTime), true, "TemporaryBlindness");

        var blindness = EnsureComp<NarsiBlindnessComponent>(target);
        blindness.TimeToRemove = _timing.CurTime + TimeSpan.FromSeconds(blindnessTime);

        EnsureComp<NarsiCultistBlindnessVisualizeComponent>(target);

        _appearanceSystem.SetData(target, NarsiCultistBlindnessStatus.Status, NarsiCultistBlindnessState.Blindness);
        OnCultistAbility(uid, args);
        args.Handled = true;
    }
}
