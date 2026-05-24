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
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;
using Robust.Shared.Prototypes;
using Content.Shared.Humanoid;
using Content.Server.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Player;
using Content.Shared.Body;
using Content.Shared.MagicMirror;
using Content.Shared.Body.Components;

namespace Content.Server.SS220.Ipc;

public sealed partial class IpcSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
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
        SubscribeLocalEvent<IpcComponent, ToggleDrainActionEvent>(OnToggleAction);
        SubscribeLocalEvent<IpcComponent, EmpPulseEvent>(OnEmpPulse);
        SubscribeLocalEvent<IpcComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeedModifiers);
        SubscribeLocalEvent<IpcComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<IpcComponent, OpenIpcFaceActionEvent>(OnOpenFaceAction);
        SubscribeLocalEvent<IpcComponent, DamageChangedEvent>(OnDamageChanged);
        //SubscribeLocalEvent<IpcComponent, OnTemperatureChangeEvent>(OnTemperatureChanged);
    }

    private void OnMapInit(Entity<IpcComponent> ent, ref MapInitEvent args)
    {
        UpdateBatteryAlert((ent, ent.Comp));
        _action.AddAction(ent, ref ent.Comp.DrainBatteryActionEntity, ent.Comp.DrainBatteryAction);
        _action.AddAction(ent, ref ent.Comp.ChangeFaceActionEntity, ent.Comp.ChangeFaceAction);
        _movementSpeedModifier.RefreshMovementSpeedModifiers(ent);
    }

    private void OnComponentShutdown(Entity<IpcComponent> ent, ref ComponentShutdown args)
    {
        _action.RemoveAction(ent.Owner, ent.Comp.DrainBatteryActionEntity);
        _action.RemoveAction(ent.Owner, ent.Comp.ChangeFaceActionEntity);
    }

    private void OnPowerCellChanged(Entity<IpcComponent> ent, ref PowerCellChangedEvent args)
    {
        if (MetaData(ent).EntityLifeStage >= EntityLifeStage.Terminating)
            return;

        UpdateBatteryAlert((ent, ent.Comp));

    }

    private void OnToggleAction(Entity<IpcComponent> ent, ref ToggleDrainActionEvent args)
    {
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
    }

    private void UpdateBatteryAlert(Entity<IpcComponent> ent, PowerCellSlotComponent? slot = null)
    {
        if (!_powerCell.TryGetBatteryFromSlot(ent.Owner, out var battery) || _battery.GetCharge(battery.Value.AsNullable()) / battery.Value.Comp.MaxCharge < 0.01f)
        {
            _alerts.ClearAlert(ent.Owner, ent.Comp.BatteryAlert);
            _alerts.ShowAlert(ent.Owner, ent.Comp.NoBatteryAlert);

            _movementSpeedModifier.RefreshMovementSpeedModifiers(ent.Owner);
            return;
        }

        var chargePercent = (short) MathF.Round(_battery.GetCharge(battery.Value.AsNullable()) / battery.Value.Comp.MaxCharge * 10f); //if 100f - crash

        if (chargePercent == 0 && _powerCell.HasDrawCharge((ent.Owner, null, slot)))
            chargePercent = 1;


        _movementSpeedModifier.RefreshMovementSpeedModifiers(ent.Owner);

        _alerts.ClearAlert(ent.Owner, ent.Comp.NoBatteryAlert);
        _alerts.ShowAlert(ent.Owner, ent.Comp.BatteryAlert, chargePercent);
    }

    private void OnRefreshMovementSpeedModifiers(Entity<IpcComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (!_powerCell.TryGetBatteryFromSlot(ent.Owner, out var battery) || _battery.GetCharge(battery.Value.AsNullable()) / battery.Value.Comp.MaxCharge < 0.01f)
        {
            args.ModifySpeed(ent.Comp.LowChargeSpeed);
        }
    }

    private void OnOpenFaceAction(Entity<IpcComponent> ent, ref OpenIpcFaceActionEvent args)
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
    private void OnTemperatureChanged(Entity<IpcComponent> ent, ref OnTemperatureChangeEvent args)
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
