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
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Physics.Events;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.Felinids.Systems;

public sealed class SharedDodgeSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedProjectileSystem _projectileSystem = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DodgeComponent, HitScanReflectAttemptEvent>(OnHitscanAttempt);
        SubscribeLocalEvent<DodgeComponent, ProjectileReflectAttemptEvent>(OnProjectileAttempt);
        SubscribeLocalEvent<DodgeComponent, PreventCollideEvent>(OnPreventCollide);
    }

    private void OnPreventCollide(Entity<DodgeComponent> ent, ref PreventCollideEvent args)
    {
        if (args.OtherFixture.Hard)
            return;

        var collidedEntity = args.OtherEntity;

        if (!TryComp<ProjectileComponent>(collidedEntity, out _) && !TryComp<DamageOnHighSpeedImpactComponent>(collidedEntity, out _))
            return;

        if (ent.Comp.EntityWhitelist.Contains(collidedEntity))
        {
            args.Cancelled = true;
            return;
        }

        GetDodgeChance(ent, out var dodgeChance);

        if (dodgeChance <= 0
        || !_random.Prob(dodgeChance))
            return;

        args.Cancelled = true;
        _adminLogger.Add(LogType.ThrowHit, LogImpact.Medium, $"{ToPrettyString(ent)} dodged {ToPrettyString(collidedEntity)} from throw hit");

        ent.Comp.EntityWhitelist.Add(collidedEntity);
        // OnPreventCollide вызывается множество раз, пока предмет пролетает через существо.
        // Таймер на 150 миллисекунд для предотвращения множественных проверок в этот промежуток.
        Timer.Spawn(150, () => { ent.Comp.EntityWhitelist.Remove(collidedEntity); });
    }

    private void OnHitscanAttempt(Entity<DodgeComponent> ent, ref HitScanReflectAttemptEvent args)
    {
        GetDodgeChance(ent, out var dodgeChance);

        if (dodgeChance <= 0
            || !_random.Prob(dodgeChance))
            return;

        args.Reflected = true;
        _adminLogger.Add(LogType.HitScanHit, LogImpact.Medium, $"{ToPrettyString(ent)} dodged hitscan from {ToPrettyString(args.SourceItem)}");
    }

    private void OnProjectileAttempt(Entity<DodgeComponent> ent, ref ProjectileReflectAttemptEvent args)
    {
        GetDodgeChance(ent, out var dodgeChance);

        if (dodgeChance <= 0
            || !_random.Prob(dodgeChance)
            || !TryComp<ProjectileComponent>(args.ProjUid, out var projectile))
            return;

        args.Cancelled = true;
        _adminLogger.Add(LogType.BulletHit, LogImpact.Medium, $"{ToPrettyString(ent)} dodged {ToPrettyString(args.ProjUid)} from {ToPrettyString(args.Component.Weapon)}");

        projectile.Shooter = ent;
        projectile.Weapon = ent;
        _projectileSystem.SetShootPositions(args.ProjUid, projectile, ent);
        Dirty(args.ProjUid, projectile);
    }

    private void GetDodgeChance(Entity<DodgeComponent> ent, out float dodgeChance)
    {
        dodgeChance = ent.Comp.BaseDodgeChance;

        if (TryComp<MobThresholdsComponent>(ent, out var mobThresholds))
        {
            if (mobThresholds.CurrentThresholdState >= MobState.Critical)
            {
                dodgeChance = 0f;
                return;
            }
            if (TryComp<DamageableComponent>(ent, out var damageable)
                && _mobThreshold.TryGetThresholdForState(ent, MobState.Critical, out var criticalThreshold, mobThresholds))
            {
                var damagePercent = (float)damageable.TotalDamage / (float)criticalThreshold;
                dodgeChance -= damagePercent * damagePercent * ent.Comp.BaseDodgeChance * ent.Comp.DamageAffect;
            }
        }

        if (TryComp<HungerComponent>(ent, out var hunger)
            && _hunger.GetHunger(hunger) < hunger.Thresholds[HungerThreshold.Okay])
        {
            var hungerPercent = Math.Clamp(1 - (_hunger.GetHunger(hunger) / hunger.Thresholds[HungerThreshold.Okay]), 0f, 1f);
            dodgeChance -= ent.Comp.BaseDodgeChance * ent.Comp.HungerAffect * hungerPercent;
        }

        if (TryComp<ThirstComponent>(ent, out var thirst)
            && thirst.CurrentThirst < thirst.ThirstThresholds[ThirstThreshold.Okay])
        {
            var thirstPercent = Math.Clamp(1 - (thirst.CurrentThirst / thirst.ThirstThresholds[ThirstThreshold.Okay]), 0f, 1f);
            dodgeChance -= ent.Comp.BaseDodgeChance * ent.Comp.ThirstAffect * thirstPercent;
        }
    }
}
