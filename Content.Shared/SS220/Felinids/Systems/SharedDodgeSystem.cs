// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Administration.Logs;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Nutrition.Components;
using Content.Shared.Projectiles;
using Content.Shared.SS220.Felinids.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Shared.SS220.Felinids.Systems;

public sealed class SharedDodgeSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedProjectileSystem _projectileSystem = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DodgeComponent, HitScanReflectAttemptEvent>(OnHitscanAttempt);
        SubscribeLocalEvent<DodgeComponent, ProjectileReflectAttemptEvent>(OnProjectileAttempt);
    }

    private void OnHitscanAttempt(Entity<DodgeComponent> ent, ref HitScanReflectAttemptEvent args)
    {
        UpdateDodgeChance(ent, out var dodgeChance);

        if (dodgeChance <= 0
            || !_random.Prob(dodgeChance))
            return;

        args.Reflected = true;
        _adminLogger.Add(LogType.HitScanHit, LogImpact.Medium, $"{ToPrettyString(ent)} dodged hitscan from {ToPrettyString(args.SourceItem)}");
    }

    private void OnProjectileAttempt(Entity<DodgeComponent> ent, ref ProjectileReflectAttemptEvent args)
    {
        UpdateDodgeChance(ent, out var dodgeChance);

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

    private void UpdateDodgeChance(Entity<DodgeComponent> ent, out float dodgeChance)
    {
        dodgeChance = ent.Comp.BaseDodgeChance;

        if (TryComp<MobThresholdsComponent>(ent, out var mobThresholds))
        {
            if (mobThresholds.CurrentThresholdState >= MobState.Critical)
            {
                dodgeChance = 0f;
                return;
            }

            if (TryComp<DamageableComponent>(ent, out var damageable))
            {
                // 200 хуман
                // 1 урона - отлично: +2,5% - 0.005f
                // 13 урона - хорошо: Нет в диздоке - 0.065f
                // 38 урона - не очень: 0 - 0.19f
                // 63 урона - плохо : -2,5% - 0.315f
                // 88 урона  ужасно: -5% - 0.44f
                // 180 фелинид
                // 79 > урона - ужасно
                // 56 > урона - плохо
                // 33 > урона - не очень
                // 11 > урона - хорошо
                // 0 = отлично

                var damagePercent = (float)damageable.TotalDamage / (float)mobThresholds.Thresholds.Last().Key;
                dodgeChance -= damagePercent * damagePercent * ent.Comp.DamageAffect;
            }
        }

        if (TryComp<HungerComponent>(ent, out var hunger))
        {
            switch (hunger.CurrentThreshold)
            {
                case HungerThreshold.Peckish: dodgeChance += -0.05f; break;
                case HungerThreshold.Starving: dodgeChance += -0.1f; break;
            }
        }

        if (TryComp<ThirstComponent>(ent, out var thirst))
        {
            switch (thirst.CurrentThirstThreshold)
            {
                case ThirstThreshold.Thirsty: dodgeChance += -0.05f; break;
                case ThirstThreshold.Parched: dodgeChance += -0.1f; break;
            }
        }
    }
}
