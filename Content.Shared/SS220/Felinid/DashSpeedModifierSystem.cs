using Content.Shared.Actions;
using Content.Shared.Movement.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.SS220.Felinid.Components;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.Felinid;

public sealed partial class DashSpeedModifierSystem : EntitySystem
{
    private static readonly EntProtoId DashStatusEffect = "StatusEffectDashSpeedModifier";
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private MovementModStatusSystem _movementMod = default!;
    [Dependency] private HungerSystem _hunger = default!;
    [Dependency] private ThirstSystem _thirst = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DashSpeedModifierComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<DashSpeedModifierComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<DashSpeedModifierComponent, DashSpeedModifierActionEvent>(OnDash);
    }

    private void OnMapInit(Entity<DashSpeedModifierComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent.Owner, ref ent.Comp.ActionEntity, ent.Comp.Action);
    }

    private void OnShutdown(Entity<DashSpeedModifierComponent> ent, ref ComponentShutdown args)
    {
        if (Terminating(ent.Owner))
            return;

        _actions.RemoveAction(ent.Owner, ent.Comp.ActionEntity);
    }

    public override void Update(float frameTime)
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
        if (args.Handled ||
            !_movementMod.TryUpdateMovementSpeedModDuration(
                ent.Owner,
                DashStatusEffect,
                ent.Comp.Duration,
                ent.Comp.SpeedModifier))
        {
            return;
        }

        args.Handled = true;
        ent.Comp.CostAt = _timing.CurTime + ent.Comp.Duration;
    }

    private void ApplyDashCosts(Entity<DashSpeedModifierComponent> ent)
    {
        // Satiation is server-authoritative; predicting it locally would only desync until the next state.
        if (!_net.IsServer)
            return;

        if (TryComp(ent.Owner, out HungerComponent? hunger))
            _hunger.ModifyHunger(ent.Owner, -ent.Comp.HungerCost, hunger);

        if (TryComp(ent.Owner, out ThirstComponent? thirst))
            _thirst.ModifyThirst(ent.Owner, thirst, -ent.Comp.ThirstCost);
    }
}
