using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Projectiles;
using Content.Shared.Random.Helpers;
using Content.Shared.SS220.Felinid.Components;
using Content.Shared.Weapons.Hitscan.Events;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Events;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.Felinid;

/// <summary>
/// Lets shots miss an entity with <see cref="DodgeChanceComponent"/>.
/// Seeded per shot and target: the client predicts it and an overlapping projectile is not re-rolled.
/// </summary>
public sealed partial class DodgeChanceSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private MobThresholdSystem _mobThresholds = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<DodgeChanceComponent, PreventCollideEvent>(OnDodgeProjectileAttempt);
        SubscribeLocalEvent<DodgeChanceComponent, AttemptHitscanRaycastHitEvent>(OnDodgeHitscanAttempt);
    }

    private void OnDodgeProjectileAttempt(Entity<DodgeChanceComponent> ent, ref PreventCollideEvent args)
    {
        if (args.Cancelled ||
            !TryComp<ProjectileComponent>(args.OtherEntity, out var projectile) ||
            projectile.Shooter == ent.Owner)
        {
            return;
        }

        var seed = SharedRandomExtensions.HashCodeCombine(GetNetEntity(args.OtherEntity).Id, GetNetEntity(ent).Id);
        if (TryDodgeShot(ent, seed))
            args.Cancelled = true;
    }

    private void OnDodgeHitscanAttempt(Entity<DodgeChanceComponent> ent, ref AttemptHitscanRaycastHitEvent args)
    {
        if (args.Cancelled)
            return;

        var seed = SharedRandomExtensions.HashCodeCombine(
            (int) _timing.CurTick.Value,
            GetNetEntity(args.HitScanEntity).Id,
            GetNetEntity(ent).Id);

        if (TryDodgeShot(ent, seed))
            args.Cancelled = true;
    }

    private bool TryDodgeShot(Entity<DodgeChanceComponent> ent, int seed)
    {
        if (_mobState.IsIncapacitated(ent.Owner) || _container.IsEntityInContainer(ent.Owner))
            return false;

        var chance = ent.Comp.BaseDodgeChance;

        // Measured against the incapacitation threshold: an entity past it is already excluded above,
        // so scaling by the death threshold would leave the lower bands unreachable.
        if (TryComp<DamageableComponent>(ent.Owner, out var damageable) &&
            _mobThresholds.TryGetIncapPercentage(
                ent.Owner,
                _damageable.GetTotalDamage((ent.Owner, damageable)),
                out FixedPoint2? healthPercent))
        {
            chance += healthPercent.Value.Float() switch
            {
                var p when p >= ent.Comp.AwfulHealthThreshold => -ent.Comp.AwfulHealthPenalty,
                var p when p >= ent.Comp.BadHealthThreshold => -ent.Comp.BadHealthPenalty,
                var p when p < ent.Comp.GoodHealthThreshold => ent.Comp.ExcellentHealthBonus,
                _ => 0f,
            };
        }

        if (TryComp<HungerComponent>(ent.Owner, out var hunger))
        {
            chance -= hunger.CurrentThreshold switch
            {
                HungerThreshold.Peckish => ent.Comp.MinorNeedPenalty,
                HungerThreshold.Starving or HungerThreshold.Dead => ent.Comp.MajorNeedPenalty,
                _ => 0f,
            };
        }

        if (TryComp<ThirstComponent>(ent.Owner, out var thirst))
        {
            chance -= thirst.CurrentThirstThreshold switch
            {
                ThirstThreshold.Thirsty => ent.Comp.MinorNeedPenalty,
                ThirstThreshold.Parched or ThirstThreshold.Dead => ent.Comp.MajorNeedPenalty,
                _ => 0f,
            };
        }

        return new System.Random(seed).Prob(Math.Clamp(chance, 0f, 1f));
    }
}
