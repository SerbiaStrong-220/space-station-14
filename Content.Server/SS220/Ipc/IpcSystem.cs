// Taken from: Corvax https://github.com/space-syndicate/space-station-14

using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.SS220.Ipc;
using Content.Shared.Ninja.Components;
using Content.Shared.Ninja.Systems;
using Content.Shared.Popups;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage.Components;
using Content.Shared.Emp;
using Content.Shared.Movement.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Sound.Components;
using Content.Shared.UserInterface;
using Content.Shared.Temperature;
using Robust.Shared.Audio;
using Content.Shared.MagicMirror;
using Content.Shared.Power;

namespace Content.Server.SS220.Ipc;

public sealed partial class IpcSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly SharedBatteryDrainerSystem _batteryDrainer = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly SharedBatterySystem _battery = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly MobThresholdSystem _mobThresholdSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IpcComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<IpcComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<IpcComponent, PowerCellChangedEvent>(OnPowerCellChanged);
        SubscribeLocalEvent<IpcComponent, ToggleDrainActionEvent>(OnToggleDrainAction);
        SubscribeLocalEvent<IpcComponent, EmpPulseEvent>(OnEmpPulse);
        SubscribeLocalEvent<IpcComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeedModifiers);
        SubscribeLocalEvent<IpcComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<IpcComponent, OpenIpcFaceActionEvent>(OnOpenIpcFaceAction);
        SubscribeLocalEvent<IpcComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<IpcComponent, BatteryStateChangedEvent>(OnBatteryStateChanged);
        //SubscribeLocalEvent<IpcComponent, OnTemperatureChangeEvent>(OnTemperatureChange);
    }

    private void OnMapInit(Entity<IpcComponent> ent, ref MapInitEvent args)
    {
        _action.AddAction(ent, ref ent.Comp.DrainBatteryActionEntity, ent.Comp.DrainBatteryAction);
        _action.AddAction(ent, ref ent.Comp.ChangeFaceActionEntity, ent.Comp.ChangeFaceAction);
        _movementSpeedModifier.RefreshMovementSpeedModifiers(ent.Owner);
        Dirty(ent);
    }

    private void OnComponentShutdown(Entity<IpcComponent> ent, ref ComponentShutdown args)
    {
        _action.RemoveAction(ent.Owner, ent.Comp.DrainBatteryActionEntity);
        _action.RemoveAction(ent.Owner, ent.Comp.ChangeFaceActionEntity);
    }

    private void OnBatteryStateChanged(Entity<IpcComponent> ent, ref BatteryStateChangedEvent args)
    {
        if (MetaData(ent).EntityLifeStage >= EntityLifeStage.Terminating)
            return;

        _movementSpeedModifier.RefreshMovementSpeedModifiers(ent.Owner);
    }

    private void OnPowerCellChanged(Entity<IpcComponent> ent, ref PowerCellChangedEvent args)
    {
        if (MetaData(ent).EntityLifeStage >= EntityLifeStage.Terminating)
            return;

        _movementSpeedModifier.RefreshMovementSpeedModifiers(ent.Owner);

        if (!_powerCell.HasBattery(ent.Owner))
        {
            ent.Comp.DrainActivated = false;
            _action.SetToggled(ent.Comp.DrainBatteryActionEntity, ent.Comp.DrainActivated);
            RemComp<BatteryDrainerComponent>(ent);
            _popup.PopupEntity(Loc.GetString("Ipc-component-disabled"), ent, ent);
            Dirty(ent);
        }
    }

    private void OnToggleDrainAction(Entity<IpcComponent> ent, ref ToggleDrainActionEvent args)
    {
        if (!_powerCell.HasBattery(ent.Owner))
        {
            _popup.PopupEntity(Loc.GetString("Ipc-component-no-battery"), ent, ent);
            return;
        }

        if (args.Handled)
            return;

        ent.Comp.DrainActivated = !ent.Comp.DrainActivated;
        _action.SetToggled(ent.Comp.DrainBatteryActionEntity, ent.Comp.DrainActivated);
        args.Handled = true;

        if (ent.Comp.DrainActivated && _powerCell.TryGetBatteryFromSlot(ent.Owner, out var battery))
        {
            EnsureComp<BatteryDrainerComponent>(ent);
            _batteryDrainer.SetBattery(ent.Owner, battery);
        }
        else
            RemComp<BatteryDrainerComponent>(ent);

        var message = ent.Comp.DrainActivated ? "Ipc-component-ready" : "Ipc-component-disabled";
        _popup.PopupEntity(Loc.GetString(message), ent, ent);

        Dirty(ent);
    }

    private void OnRefreshMovementSpeedModifiers(Entity<IpcComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (!_powerCell.TryGetBatteryFromSlot(ent.Owner, out var battery) || _battery.GetCharge(battery.Value.AsNullable()) / battery.Value.Comp.MaxCharge < 0.01f)
        {
            args.ModifySpeed(ent.Comp.LowChargeSpeed);
        }
    }

    private void OnOpenIpcFaceAction(Entity<IpcComponent> ent, ref OpenIpcFaceActionEvent args)
    {
        if (args.Handled)
            return;

        if (!HasComp<MagicMirrorComponent>(ent))
            return;

        args.Handled = true;

        // User open UI event.
        var ev = new BeforeActivatableUIOpenEvent(args.Performer);

        // Raise event on ent. MagicMirrorSystem call UpdateInterface().
        RaiseLocalEvent(ent.Owner, ev);

        // Open magic mirror UI
        _ui.TryOpenUi(ent.Owner, MagicMirrorUiKey.Key, args.Performer);
    }

    private void OnEmpPulse(Entity<IpcComponent> ent, ref EmpPulseEvent args)
    {
        args.Affected = true;

        var damage = new DamageSpecifier();
        damage.DamageDict.Add("Shock", ent.Comp.DamageFromEmp);
        _damageableSystem.TryChangeDamage(ent.Owner, damage);

    }

    private void OnMobStateChanged(Entity<IpcComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState is MobState.Critical)
        {
            var sound = EnsureComp<SpamEmitSoundComponent>(ent);
            sound.Sound = new SoundPathSpecifier("/Audio/Machines/buzz-two.ogg");
            sound.MinInterval = TimeSpan.FromSeconds(15);
            sound.MaxInterval = TimeSpan.FromSeconds(30);
        }
        else
        {
            RemComp<SpamEmitSoundComponent>(ent);
        }

    }

    private void OnDamageChanged(Entity<IpcComponent> ent, ref DamageChangedEvent args)
    {
        if (!TryComp<MobStateComponent>(ent, out var mobComp))
            return;

        if (!_mobState.IsDead(ent, mobComp))
            return;

        if (!TryComp<DamageableComponent>(ent, out var damageableComp))
            return;

        if (!_mobThresholdSystem.TryGetDeadThreshold(ent, out var threshold))
            return;

        if (_damageableSystem.GetTotalDamage(ent.Owner) > threshold)
            return;

        _mobState.ChangeMobState(ent, MobState.Critical);
    }
/*
    private void OnTemperatureChange(Entity<IpcComponent> ent, ref OnTemperatureChangeEvent args)
    {
        if (!TryComp<PowerCellDrawComponent>(ent, out var draw))
            return;

        var delta = Math.Abs(args.CurrentTemperature - ent.Comp.NormalTemperature);

        float newDrawRate = ent.Comp.BaseDrawRate;

        if (delta > ent.Comp.CritDelta)
            newDrawRate = ent.Comp.CritDrawRate;
        else if (delta > ent.Comp.OverDelta)
            newDrawRate = ent.Comp.OverDrawRate;

        if (MathHelper.CloseTo(draw.DrawRate, newDrawRate))
            return;

        draw.DrawRate = newDrawRate;
        Dirty(ent, draw);
    }*/
}
