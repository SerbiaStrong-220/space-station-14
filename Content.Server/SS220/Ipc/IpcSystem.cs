// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
// Original code taken from: Corvax https://github.com/space-syndicate/space-station-14

using Content.Shared.Actions;
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
using Content.Shared.MagicMirror;
using Content.Shared.Power;

namespace Content.Server.SS220.Ipc;

public sealed partial class IpcSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _action = default!;
    [Dependency] private SharedBatteryDrainerSystem _batteryDrainer = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private PowerCellSystem _powerCell = default!;
    [Dependency] private SharedBatterySystem _battery = default!;
    [Dependency] private DamageableSystem _damageableSystem = default!;
    [Dependency] private MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private MobThresholdSystem _mobThresholdSystem = default!;

    private static readonly LocId IpcDrainReady = "ipc-drain-enabled";
    private static readonly LocId IpcDrainDisabled = "ipc-drain-disabled";
    private static readonly LocId IpcNoBattery = "ipc-no-battery";
    private static readonly TimeSpan SoundMinInterval = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan SoundMaXInterval = TimeSpan.FromSeconds(30);

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
        SubscribeLocalEvent<IpcComponent, OnTemperatureChangeEvent>(OnTemperatureChange);
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

    /// <summary>
    /// If the battery is present but discharged, the movement speed is reduced.
    /// </summary>
    private void OnBatteryStateChanged(Entity<IpcComponent> ent, ref BatteryStateChangedEvent args)
    {
        if (Terminating(ent))
            return;

        _movementSpeedModifier.RefreshMovementSpeedModifiers(ent.Owner);
    }

    /// <summary>
    /// The presence of a battery affects the movement speed
    /// and the ability to use the drain ability.
    /// When removing the battery, forcibly disable the drain ability.
    /// </summary>
    private void OnPowerCellChanged(Entity<IpcComponent> ent, ref PowerCellChangedEvent args)
    {
        if (Terminating(ent))
            return;

        _movementSpeedModifier.RefreshMovementSpeedModifiers(ent.Owner);

        if (_powerCell.HasBattery(ent.Owner))
            return;

        ent.Comp.DrainActivated = false;
        _action.SetToggled(ent.Comp.DrainBatteryActionEntity, ent.Comp.DrainActivated);

        // show popup only if comp removed
        if (RemComp<BatteryDrainerComponent>(ent))
        {
            _popup.PopupEntity(Loc.GetString(IpcDrainDisabled), ent, ent);
        }

        Dirty(ent);
    }

    /// <summary>
    /// Enable/disable the drain ability. 
    /// </summary>
    private void OnToggleDrainAction(Entity<IpcComponent> ent, ref ToggleDrainActionEvent args)
    {
        if (args.Handled)
            return;

        if (!_powerCell.HasBattery(ent.Owner))
        {
            _popup.PopupEntity(Loc.GetString(IpcNoBattery), ent, ent);
            return;
        }

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

        var message = ent.Comp.DrainActivated ? IpcDrainReady : IpcDrainDisabled;
        _popup.PopupEntity(Loc.GetString(message), ent, ent);

        Dirty(ent);
    }

    /// <summary>
    /// The movement speed of the IPS is reduced if they are out of charge.
    /// </summary>
    private void OnRefreshMovementSpeedModifiers(Entity<IpcComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (!_powerCell.TryGetBatteryFromSlot(ent.Owner, out var battery) || _battery.GetCharge(battery.Value.AsNullable()) / battery.Value.Comp.MaxCharge < 0.01f)
        {
            args.ModifySpeed(ent.Comp.LowChargeSpeed);
        }
    }

    /// <summary>
    /// The screen image change function is based on the Magic Mirror.
    /// </summary>
    private void OnOpenIpcFaceAction(Entity<IpcComponent> ent, ref OpenIpcFaceActionEvent args)
    {
        if (args.Handled)
            return;

        if (!HasComp<MagicMirrorComponent>(ent))
            return;

        // User open UI event.
        var ev = new BeforeActivatableUIOpenEvent(args.Performer);

        // Raise event on ent. MagicMirrorSystem call UpdateInterface().
        RaiseLocalEvent(ent.Owner, ev);

        // Open magic mirror UI
        args.Handled = _ui.TryOpenUi(ent.Owner, MagicMirrorUiKey.Key, args.Performer);
    }

    /// <summary>
    /// IPS take damage from EMP.
    /// </summary>
    private void OnEmpPulse(Entity<IpcComponent> ent, ref EmpPulseEvent args)
    {
        args.Affected = true;

        var damage = new DamageSpecifier();
        damage.DamageDict.Add("Shock", ent.Comp.DamageFromEmp);
        _damageableSystem.TryChangeDamage(ent.Owner, damage);

    }

    /// <summary>
    /// Organic beings in critical condition make sounds of labored breathing. IPS - beeps.
    /// </summary>
    private void OnMobStateChanged(Entity<IpcComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState is MobState.Critical)
        {
            var sound = EnsureComp<SpamEmitSoundComponent>(ent);
            sound.Sound = ent.Comp.CritStateSound;
            sound.MinInterval = SoundMinInterval;
            sound.MaxInterval = SoundMaXInterval;
        }
        else
        {
            RemComp<SpamEmitSoundComponent>(ent);
        }

    }

    /// <summary>
    /// IPS easily return from a dead state to a critical state if they are repaired.
    /// </summary>
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

    /// <summary>
    /// IPC are sensitive to temperature changes. 
    /// Going into space without a hardsuit? The battery drains faster.
    /// </summary>
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

        _powerCell.SetDrawRate((ent.Owner, draw), newDrawRate);
    }
}
