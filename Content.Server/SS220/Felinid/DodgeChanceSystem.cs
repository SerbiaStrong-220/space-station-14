using Content.Server.Disposal.Unit;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Disposal.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Projectiles;
using Content.Shared.SS220.Felinid.Components;
using Content.Shared.Weapons.Hitscan.Events;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Events;
using Robust.Shared.Random;

namespace Content.Server.SS220.Felinid;

public sealed partial class DodgeChanceSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private MobThresholdSystem _mobThresholds = default!;
    [Dependency] private IRobustRandom _random = default!;

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

        if (TryDodgeShot((ent.Owner, ent.Comp)))
            args.Cancelled = true;
    }

    private void OnDodgeHitscanAttempt(Entity<DodgeChanceComponent> ent, ref AttemptHitscanRaycastHitEvent args)
    {
        if (args.Cancelled)
            return;

        if (TryDodgeShot((ent.Owner, ent.Comp)))
            args.Cancelled = true;
    }

    private bool TryDodgeShot(Entity<DodgeChanceComponent> ent)
    {
        if (_mobState.IsIncapacitated(ent.Owner) ||
            HasComp<BeingDisposedComponent>(ent.Owner))
        {
            return false;
        }

        var dodgeChance = GetDodgeChance(ent);
        return dodgeChance > 0f && _random.Prob(dodgeChance);
    }

    private float GetDodgeChance(Entity<DodgeChanceComponent> ent)
    {
        var chance = ent.Comp.BaseDodgeChance;

        if (TryComp<DamageableComponent>(ent.Owner, out var damageable) &&
            _mobThresholds.TryGetDeadPercentage(ent.Owner, _damageable.GetTotalDamage((ent.Owner, damageable)), out FixedPoint2? healthPercent))
        {
            var damageFraction = healthPercent.Value.Float();
            chance *= MathHelper.Lerp(1f, ent.Comp.CriticalHealthMultiplier, damageFraction);
        }

        if (TryComp<HungerComponent>(ent.Owner, out var hunger))
        {
            chance *= hunger.CurrentThreshold switch
            {
                HungerThreshold.Peckish => ent.Comp.MinorNeedMultiplier,
                HungerThreshold.Starving or HungerThreshold.Dead => ent.Comp.MajorNeedMultiplier,
                _ => 1f,
            };
        }

        if (TryComp<ThirstComponent>(ent.Owner, out var thirst))
        {
            chance *= thirst.CurrentThirstThreshold switch
            {
                ThirstThreshold.Thirsty => ent.Comp.MinorNeedMultiplier,
                ThirstThreshold.Parched or ThirstThreshold.Dead => ent.Comp.MajorNeedMultiplier,
                _ => 1f,
            };
        }

        return Math.Clamp(chance, 0f, 1f);
    }
}
