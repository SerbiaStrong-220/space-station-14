using Content.Server.SS220.DarkForces.Narsi.Cultist.Abilities.FireArms;
using Content.Server.SS220.DarkForces.Saint.Reagent.Events;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.SS220.DarkForces.Narsi.Abilities.Events;
using Content.Shared.SS220.DarkForces.Narsi.Cultist.FireArms;
using Content.Shared.SS220.DarkForces.Narsi.Roles;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Server.SS220.DarkForces.Narsi.Cultist.Abilities;

public sealed partial class NarsiCultistAbilitiesSystem
{
    [Dependency] private readonly FlammableSystem _flammableSystem = default!;

    private void InitializeFireArms()
    {
        SubscribeLocalEvent<NarsiCultistComponent, NarsiCultistFireArmsEvent>(OnFireArmsEvent);
        SubscribeLocalEvent<NarsiCultistFireArmsComponent, OnSaintWaterFlammableEvent>(OnSaintWaterFlammable);
        SubscribeLocalEvent<NarsiCultistFireArmsComponent, MeleeHitEvent>(OnAttack);
    }

    private void OnAttack(EntityUid uid, NarsiCultistFireArmsComponent component, MeleeHitEvent args)
    {
        args.BonusDamage = component.DamageSpecifier;

        if (!component.CanFireTargets)
            return;

        foreach (var target in args.HitEntities)
        {
            _flammableSystem.AdjustFireStacks(target, 2);
            _flammableSystem.Ignite(target, uid);
        }
    }

    private void OnSaintWaterFlammable(EntityUid uid, NarsiCultistFireArmsComponent component, OnSaintWaterFlammableEvent args)
    {
        ClearFireArms(uid);
    }

    private void OnFireArmsEvent(EntityUid uid, NarsiCultistComponent component, NarsiCultistFireArmsEvent args)
    {
        if (args.Handled)
            return;

        var fireArmsComponent = EnsureComp<NarsiCultistFireArmsComponent>(uid);
        var level = _progressSystem.GetAbilityLevel(FireAction);
        var duration = level switch
        {
            1 => 10,
            2 => 20,
            _ => 30
        };

        var burnDamage = level switch
        {
            1 => 10,
            2 => 20,
            _ => 30
        };

        var canFireTargets = level switch
        {
            1 => false,
            2 => false,
            _ => true
        };

        fireArmsComponent.TickToRemove = _timing.CurTime + TimeSpan.FromSeconds(duration);
        fireArmsComponent.CanFireTargets = canFireTargets;
        fireArmsComponent.DamageSpecifier = new DamageSpecifier
        {
            DamageDict = new Dictionary<string, FixedPoint2>
            {
                {"Heat", burnDamage}
            }
        };

        EnsureComp<NarsiCultistFireArmsVisualizerComponent>(uid);
        _appearanceSystem.SetData(uid, NarsiCultistFireArmsStatus.Status, NarsiCultistFireArmsState.Fire);
        OnCultistAbility(uid, args);

        args.Handled = true;
    }

    private void UpdateFireArms()
    {
        var query = EntityQueryEnumerator<NarsiCultistFireArmsComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (component.TickToRemove > _timing.CurTime)
                continue;

            ClearFireArms(uid);
        }
    }

    private void ClearFireArms(EntityUid uid)
    {
        _appearanceSystem.SetData(uid, NarsiCultistFireArmsStatus.Status, NarsiCultistFireArmsState.Empty);

        RemComp<NarsiCultistFireArmsComponent>(uid);
        RemComp<NarsiCultistFireArmsVisualizerComponent>(uid);
    }
}
