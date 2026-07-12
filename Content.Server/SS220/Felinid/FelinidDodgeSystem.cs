using System.Numerics;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Systems;
using Content.Server.Disposal.Tube;
using Content.Server.Disposal.Unit;
using Content.Server.Popups;
using Content.Shared.Atmos;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Disposal.Components;
using Content.Shared.Disposal.Tube;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Projectiles;
using Content.Shared.SS220.Felinid;
using Content.Shared.SS220.Felinid.Components;
using Content.Shared.SS220.Maths;
using Content.Shared.Stunnable;
using Content.Shared.Slippery;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Hitscan.Events;
using Robust.Shared.Containers;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using SharedFelinidPipecrawlSystem = Content.Shared.SS220.Felinid.FelinidPipecrawlSystem;

namespace Content.Server.SS220.Felinid;

public sealed class FelinidDodgeSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _mobThresholds = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<FelinidDodgeComponent, PreventCollideEvent>(OnDodgeProjectileAttempt);
        SubscribeLocalEvent<FelinidDodgeComponent, AttemptHitscanRaycastHitEvent>(OnDodgeHitscanAttempt);
    }

    private void OnDodgeProjectileAttempt(Entity<FelinidDodgeComponent> ent, ref PreventCollideEvent args)
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

    private void OnDodgeHitscanAttempt(Entity<FelinidDodgeComponent> ent, ref AttemptHitscanRaycastHitEvent args)
    {
        if (args.Cancelled)
            return;

        if (TryDodgeShot((ent.Owner, ent.Comp)))
            args.Cancelled = true;
    }

    private bool TryDodgeShot(Entity<FelinidDodgeComponent> ent)
    {
        if (_mobState.IsIncapacitated(ent.Owner) ||
            HasComp<BeingDisposedComponent>(ent.Owner))
        {
            return false;
        }

        var dodgeChance = GetDodgeChance(ent);
        return dodgeChance > 0f && _random.Prob(dodgeChance);
    }

    private float GetDodgeChance(Entity<FelinidDodgeComponent> ent)
    {
        var chance = ent.Comp.BaseDodgeChance;

        if (TryComp<DamageableComponent>(ent.Owner, out var damageable) &&
            _mobThresholds.TryGetDeadPercentage(ent.Owner, _damageable.GetTotalDamage((ent.Owner, damageable)), out FixedPoint2? healthPercent))
        {
            var health = healthPercent.Value.Float();

            if (health <= 0.25f)
                chance += ent.Comp.ExcellentHealthBonus;
            else if (health > 0.50f && health <= 0.75f)
                chance -= ent.Comp.PoorHealthPenalty;
            else if (health > 0.75f)
                chance -= ent.Comp.TerribleHealthPenalty;
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

        return Math.Clamp(chance, 0f, ent.Comp.MaxDodgeChance);
    }
}
