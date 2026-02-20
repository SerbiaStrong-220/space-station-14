// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Actions;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.SS220.Felinids.Components;
using Content.Shared.SS220.Felinids.Events;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.Felinids.Systems;

public sealed class SharedSpeedUpSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speedModifier = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly ThirstSystem _thirst = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpeedUpComponent, MapInitEvent>(OnSpeedUpMapInit);
        SubscribeLocalEvent<SpeedUpComponent, ComponentShutdown>(OnSpeedUpShutdown);
        SubscribeLocalEvent<SpeedUpComponent, SpeedUpActionEvent>(OnSpeedUpToggle);
        SubscribeLocalEvent<SpeedUpComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
    }

    private void OnSpeedUpMapInit(EntityUid uid, SpeedUpComponent component, MapInitEvent args)
    {
        _actions.AddAction(uid, ref component.ActionEntity, component.Action, uid);
    }

    private void OnSpeedUpShutdown(EntityUid uid, SpeedUpComponent component, ComponentShutdown args)
    {
        _actions.RemoveAction(uid, component.ActionEntity);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<SpeedUpComponent>();
        while (query.MoveNext(out var uid, out var speedUpComp))
        {
            if (!speedUpComp.Active)
                continue;

            if (!ContinueSpeedUp((uid, speedUpComp)))
                EndSpeedUp((uid, speedUpComp));
        }
    }

    public void EndSpeedUp(Entity<SpeedUpComponent> ent)
    {
        if (!ent.Comp.Active)
        {
            _speedModifier.RefreshMovementSpeedModifiers(ent);
            return;
        }

        ent.Comp.Active = false;
        _speedModifier.RefreshMovementSpeedModifiers(ent);

        if (TryComp<HungerComponent>(ent, out var hunger))
            _hunger.SetHunger(ent, hunger.LastAuthoritativeHungerValue - hunger.Thresholds[HungerThreshold.Overfed] * ent.Comp.HungerCost, hunger);
        if (TryComp<ThirstComponent>(ent, out var thirst))
            _thirst.SetThirst(ent, thirst, thirst.CurrentThirst - thirst.ThirstThresholds[ThirstThreshold.OverHydrated] * ent.Comp.ThirstCost);
    }

    private bool ContinueSpeedUp(Entity<SpeedUpComponent> ent)
    {
        if (!ent.Comp.Active)
            return false;

        if (ent.Comp.EndTime <= _gameTiming.CurTime)
            return false;

        if (TryComp<MobStateComponent>(ent, out var mobState) &&
           mobState.CurrentState >= MobState.Critical)
            return false;

        return true;
    }

    private void OnSpeedUpToggle(EntityUid uid, SpeedUpComponent speedUpComp, ref SpeedUpActionEvent args)
    {
        if (args.Handled || speedUpComp.Active)
            return;

        if (TryComp<HungerComponent>(uid, out var hunger)
            && hunger.CurrentThreshold < speedUpComp.HungerThreshold)
        {
            _popup.PopupClient(Loc.GetString("popup-speedup-no-hunger"), uid, PopupType.Small);
            return;
        }

        if (TryComp<ThirstComponent>(uid, out var thirst)
            && thirst.CurrentThirstThreshold < speedUpComp.ThirstThreshold)
        {
            _popup.PopupClient(Loc.GetString("popup-speedup-no-thirst"), uid, PopupType.Small);
            return;
        }

        args.Handled = true;
        speedUpComp.Active = true;
        _speedModifier.RefreshMovementSpeedModifiers(uid);
        speedUpComp.EndTime = _gameTiming.CurTime + TimeSpan.FromSeconds(speedUpComp.Duration);
    }

    private void OnRefreshSpeed(EntityUid uid, SpeedUpComponent speedUpComp, RefreshMovementSpeedModifiersEvent args)
    {
        if (speedUpComp.Active)
            args.ModifySpeed(speedUpComp.SpeedModifier);
    }
}
