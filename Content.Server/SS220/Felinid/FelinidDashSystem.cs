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

public sealed class FelinidDashSystem : EntitySystem
{
    private static readonly EntProtoId DashStatusEffect = "StatusEffectFelinidDash";
    [Dependency] private readonly MovementModStatusSystem _movementMod = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly ThirstSystem _thirst = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<FelinidDashComponent, FelinidDashActionEvent>(OnDash);
    }

    public override void Update(float frameTime)
    {
        UpdateDashCosts();
    }

    private void UpdateDashCosts()
    {
        var query = EntityQueryEnumerator<FelinidDashComponent>();
        while (query.MoveNext(out var uid, out var dash))
        {
            if (dash.CostAt is not { } costAt || costAt > _timing.CurTime)
                continue;

            dash.CostAt = null;
            ApplyDashCosts((uid, dash));
        }
    }

    private void OnDash(Entity<FelinidDashComponent> ent, ref FelinidDashActionEvent args)
    {
        if (args.Handled)
            return;

        if (!_movementMod.TryUpdateMovementSpeedModDuration(
            ent.Owner,
            DashStatusEffect,
            ent.Comp.Duration,
            ent.Comp.SpeedModifier,
            ent.Comp.SpeedModifier))
        {
            return;
        }

        args.Handled = true;
        ent.Comp.CostAt = _timing.CurTime + ent.Comp.Duration;
    }

    private void ApplyDashCosts(Entity<FelinidDashComponent> ent)
    {
        if (TryComp(ent.Owner, out HungerComponent? hunger))
        {
            if (hunger.Thresholds.TryGetValue(HungerThreshold.Overfed, out var hungerPool))
                _hunger.ModifyHunger(ent.Owner, -hungerPool * ent.Comp.HungerCostRatio, hunger);
        }

        if (TryComp(ent.Owner, out ThirstComponent? thirst))
        {
            if (thirst.ThirstThresholds.TryGetValue(ThirstThreshold.OverHydrated, out var thirstPool))
                _thirst.ModifyThirst(ent.Owner, thirst, -thirstPool * ent.Comp.ThirstCostRatio);
        }
    }
}
