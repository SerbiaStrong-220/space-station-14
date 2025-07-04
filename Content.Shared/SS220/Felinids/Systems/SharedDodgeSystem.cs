// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Administration.Logs;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Database;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Projectiles;
using Content.Shared.SS220.Felinids.Components;
using Content.Shared.SS220.Weapons.Ranged.Events;
using Robust.Shared.Physics.Events;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.Felinids.Systems;

public sealed class SharedDodgeSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DodgeComponent, HitscanAttempt>(OnHitscanAttempt);
        SubscribeLocalEvent<DodgeComponent, PreventCollideEvent>(OnPreventCollide);
    }

    private void OnPreventCollide(Entity<DodgeComponent> ent, ref PreventCollideEvent args)
    {
        if (ent.Comp.EntityWhitelist.Contains(args.OtherEntity))
        {
            args.Cancelled = true;
            return;
        }

        if (args.OtherFixture.Hard)
            return;

        var collidedEntity = args.OtherEntity;

        if (!HasComp<ProjectileComponent>(collidedEntity)
            && !HasComp<DamageOnHighSpeedImpactComponent>(collidedEntity))
            return;

        if (!TryDodge(ent, out _))
            return;

        args.Cancelled = true;
        _adminLogger.Add(LogType.ThrowHit, LogImpact.Medium, $"{ToPrettyString(ent)} dodged {ToPrettyString(collidedEntity)} from throw hit");

        ent.Comp.EntityWhitelist.Add(collidedEntity);
        Dirty(ent);
        // OnPreventCollide вызывается множество раз, пока предмет пролетает через существо.
        // Таймер на 220 миллисекунд для предотвращения множественных проверок в этот промежуток.
        Timer.Spawn(220, () => { ent.Comp.EntityWhitelist.Remove(collidedEntity); });
    }

    private void OnHitscanAttempt(Entity<DodgeComponent> ent, ref HitscanAttempt args)
    {
        if (!TryDodge(ent, out _))
            return;

        args.Cancelled = true;
        _adminLogger.Add(LogType.HitScanHit, LogImpact.Medium, $"{ToPrettyString(ent)} dodged hitscan from {ToPrettyString(args.User)}");
    }

    private bool TryDodge(Entity<DodgeComponent> ent, out float resultDodgeChance)
    {
        resultDodgeChance = ent.Comp.BaseDodgeChance;

        if (TryComp<MobThresholdsComponent>(ent, out var mobThresholds))
        {
            if (mobThresholds.CurrentThresholdState >= MobState.Critical)
            {
                resultDodgeChance = 0f;
                return false;
            }
            if (TryComp<DamageableComponent>(ent, out var damageable)
                && _mobThreshold.TryGetThresholdForState(ent, MobState.Critical, out var criticalThreshold, mobThresholds))
            {
                var damagePercent = (float)damageable.TotalDamage / (float)criticalThreshold;
                resultDodgeChance -= damagePercent * damagePercent * ent.Comp.BaseDodgeChance * ent.Comp.DamageAffect;
            }
        }

        if (TryComp<HungerComponent>(ent, out var hunger)
            && _hunger.GetHunger(hunger) < hunger.Thresholds[HungerThreshold.Okay])
        {
            var hungerPercent = Math.Clamp(1 - (_hunger.GetHunger(hunger) / hunger.Thresholds[HungerThreshold.Okay]), 0f, 1f);
            resultDodgeChance -= ent.Comp.BaseDodgeChance * ent.Comp.HungerAffect * hungerPercent;
        }

        if (TryComp<ThirstComponent>(ent, out var thirst)
            && thirst.CurrentThirst < thirst.ThirstThresholds[ThirstThreshold.Okay])
        {
            var thirstPercent = Math.Clamp(1 - (thirst.CurrentThirst / thirst.ThirstThresholds[ThirstThreshold.Okay]), 0f, 1f);
            resultDodgeChance -= ent.Comp.BaseDodgeChance * ent.Comp.ThirstAffect * thirstPercent;
        }

        if (resultDodgeChance <= 0
        || !_random.Prob(resultDodgeChance))
            return false;

        return true;
    }
}
