using Content.Shared.Movement.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.SS220.Felinid;
using Content.Shared.SS220.Felinid.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server.SS220.Felinid;

public sealed partial class DashSpeedModifierSystem : EntitySystem
{
    private static readonly EntProtoId DashStatusEffect = "StatusEffectDashSpeedModifier";
    [Dependency] private MovementModStatusSystem _movementMod = default!;
    [Dependency] private HungerSystem _hunger = default!;
    [Dependency] private ThirstSystem _thirst = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<DashSpeedModifierComponent, DashSpeedModifierActionEvent>(OnDash);
    }

    public override void Update(float frameTime)
    {
        UpdateDashCosts();
    }

    private void UpdateDashCosts()
    {
        var query = EntityQueryEnumerator<DashSpeedModifierComponent>();
        while (query.MoveNext(out var uid, out var dash))
        {
            if (dash.CostAt is not { } costAt || costAt > _timing.CurTime)
                continue;

            dash.CostAt = null;
            ApplyDashCosts((uid, dash));
        }
    }

    private void OnDash(Entity<DashSpeedModifierComponent> ent, ref DashSpeedModifierActionEvent args)
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

    private void ApplyDashCosts(Entity<DashSpeedModifierComponent> ent)
    {
        if (TryComp(ent.Owner, out HungerComponent? hunger))
        {
            var thresholds = hunger.Thresholds;
            if (thresholds.TryGetValue(HungerThreshold.Overfed, out var hungerPool))
                _hunger.ModifyHunger(ent.Owner, -hungerPool * ent.Comp.HungerCostRatio, hunger);
        }

        if (TryComp(ent.Owner, out ThirstComponent? thirst))
        {
            var thresholds = thirst.ThirstThresholds;
            if (thresholds.TryGetValue(ThirstThreshold.OverHydrated, out var thirstPool))
                _thirst.ModifyThirst(ent.Owner, thirst, -thirstPool * ent.Comp.ThirstCostRatio);
        }
    }
}
