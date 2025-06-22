// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Administration.Logs;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Reflect;
using Robust.Shared.Random;

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
        _adminLogger.Add(LogType.HitScanHit, LogImpact.Medium, $"{ToPrettyString(ent)} dodge hitscan from {ToPrettyString(args.SourceItem)}");
    }

    private void OnProjectileAttempt(Entity<DodgeComponent> ent, ref ProjectileReflectAttemptEvent args)
    {
        UpdateDodgeChance(ent, out var dodgeChance);

        if (dodgeChance <= 0
            || !_random.Prob(dodgeChance)
            || !TryComp<ProjectileComponent>(args.ProjUid, out var projectile))
            return;

        args.Cancelled = true;
        _adminLogger.Add(LogType.BulletHit, LogImpact.Medium, $"{ToPrettyString(ent)} dodge {ToPrettyString(args.ProjUid)} from {ToPrettyString(args.Component.Weapon)}");

        projectile.Shooter = ent;
        projectile.Weapon = ent;
        _projectileSystem.SetShootPositions(args.ProjUid, projectile, ent);
        Dirty(args.ProjUid, projectile);
    }

    private void UpdateDodgeChance(Entity<DodgeComponent> ent, out float dodgeChance)
    {
        dodgeChance = ent.Comp.BaseDodgeChance;

        if (TryComp<MobThresholdsComponent>(ent, out var mobThresholds)
            && mobThresholds.CurrentThresholdState is MobState.Dead)
        {
            dodgeChance = 0f;
            return;
        }

    }
}
