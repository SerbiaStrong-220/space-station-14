using Content.Server.PowerCell;
using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.SS220.Kpb;
using Content.Shared.Ninja.Components;
using Content.Shared.Ninja.Systems;
using Content.Shared.Popups;
using Content.Shared.PowerCell.Components;
using Content.Shared.Damage;
using Content.Shared.Emp;
using Content.Shared.Movement.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Sound.Components;
using Content.Shared.UserInterface;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;
using Robust.Shared.Prototypes;
using Content.Shared.Humanoid;
using Content.Server.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Shared.Player;

namespace Content.Server.SS220.Kpb;

public sealed partial class KpbSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly SharedBatteryDrainerSystem _batteryDrainer = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly MarkingManager _markingManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KpbComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<KpbComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<KpbComponent, PowerCellChangedEvent>(OnPowerCellChanged);
        SubscribeLocalEvent<KpbComponent, ToggleDrainActionEvent>(OnToggleAction);
        SubscribeLocalEvent<KpbComponent, EmpPulseEvent>(OnEmpPulse);
        SubscribeLocalEvent<KpbComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshMovementSpeedModifiers);
        SubscribeLocalEvent<KpbComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<KpbComponent, OpenKpbFaceActionEvent>(OnOpenFaceAction);
        Subs.BuiEvents<KpbComponent>(KpbFaceUiKey.Face, subs =>
        {
            subs.Event<KpbFaceSelectMessage>(OnFaceSelected);
        });
    }

    private void OnMapInit(Entity<KpbComponent> ent, ref MapInitEvent args)
    {
        UpdateBatteryAlert((ent, ent.Comp));
        _action.AddAction(ent, ref ent.Comp.ActionEntity, ent.Comp.DrainBatteryAction);
        _action.AddAction(ent, ref ent.Comp.ChangeFaceActionEntity, ent.Comp.ChangeFaceAction);
        _movementSpeedModifier.RefreshMovementSpeedModifiers(ent);

        if (TryComp<HumanoidAppearanceComponent>(ent, out var appearance) &&
            appearance.MarkingSet.TryGetCategory(MarkingCategories.Snout, out var markings) &&
            markings.Count > 0)
        {
            ent.Comp.SelectedFace = markings[0].MarkingId;
            Dirty(ent);
        }
    }

    private void OnComponentShutdown(Entity<KpbComponent> ent, ref ComponentShutdown args)
    {
        _action.RemoveAction(ent.Owner, ent.Comp.ActionEntity);
        _action.RemoveAction(ent.Owner, ent.Comp.ChangeFaceActionEntity);
    }

    private void OnPowerCellChanged(Entity<KpbComponent> ent, ref PowerCellChangedEvent args)
    {
        if (MetaData(ent).EntityLifeStage >= EntityLifeStage.Terminating)
            return;

        UpdateBatteryAlert((ent, ent.Comp));

    }

    private void OnToggleAction(Entity<KpbComponent> ent, ref ToggleDrainActionEvent args)
    {
        if (args.Handled)
            return;

        ent.Comp.DrainActivated = !ent.Comp.DrainActivated;
        _action.SetToggled(ent.Comp.ActionEntity, ent.Comp.DrainActivated);
        args.Handled = true;

        if (ent.Comp.DrainActivated && _powerCell.TryGetBatteryFromSlot(ent, out var battery, out var _))
        {
            EnsureComp<BatteryDrainerComponent>(ent);
            _batteryDrainer.SetBattery(ent.Owner, battery);
        }
        else
            RemComp<BatteryDrainerComponent>(ent);

        var message = ent.Comp.DrainActivated ? "Kpb-component-ready" : "Kpb-component-disabled";
        _popup.PopupEntity(Loc.GetString(message), ent, ent);
    }

    private void UpdateBatteryAlert(Entity<KpbComponent> ent, PowerCellSlotComponent? slot = null)
    {
        if (!_powerCell.TryGetBatteryFromSlot(ent, out var battery, slot) || battery.CurrentCharge / battery.MaxCharge < 0.01f)
        {
            _alerts.ClearAlert(ent.Owner, ent.Comp.BatteryAlert);
            _alerts.ShowAlert(ent.Owner, ent.Comp.NoBatteryAlert);

            _movementSpeedModifier.RefreshMovementSpeedModifiers(ent.Owner);
            return;
        }

        var chargePercent = (short) MathF.Round(battery.CurrentCharge / battery.MaxCharge * 10f); //if 100f - crash

        if (chargePercent == 0 && _powerCell.HasDrawCharge(ent, cell: slot))
            chargePercent = 1;


        _movementSpeedModifier.RefreshMovementSpeedModifiers(ent.Owner);

        _alerts.ClearAlert(ent.Owner, ent.Comp.NoBatteryAlert);
        _alerts.ShowAlert(ent.Owner, ent.Comp.BatteryAlert, chargePercent);
    }

    private void OnRefreshMovementSpeedModifiers(Entity<KpbComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (!_powerCell.TryGetBatteryFromSlot(ent, out var battery) || battery.CurrentCharge / battery.MaxCharge < 0.01f)
        {
            args.ModifySpeed(ent.Comp.LowChargeSpeed);
        }
    }

    private void OnOpenFaceAction(Entity<KpbComponent> ent, ref OpenKpbFaceActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<ActorComponent>(ent, out var actor))
            return;

        _ui.SetUiState(ent.Owner, KpbFaceUiKey.Face, new KpbFaceBuiState(ent.Comp.FaceProfile, ent.Comp.SelectedFace));
        _ui.TryToggleUi(ent.Owner, KpbFaceUiKey.Face, actor.PlayerSession);
        args.Handled = true;
    }

    private void OnFaceSelected(Entity<KpbComponent> ent, ref KpbFaceSelectMessage msg)
    {
        if (TryComp<HumanoidAppearanceComponent>(ent.Owner, out var appearance))
        {
            var category = MarkingCategories.Snout;
            if (appearance.MarkingSet.TryGetCategory(category, out var markings) && markings.Count > 0)
            {
                _humanoid.SetMarkingId(ent.Owner, category, 0, msg.State, appearance);
            }
            else if (_markingManager.Markings.TryGetValue(msg.State, out var proto))
            {
                appearance.MarkingSet.AddBack(category, proto.AsMarking());
                Dirty(ent.Owner, appearance);
            }
        }

        ent.Comp.SelectedFace = msg.State;
        Dirty(ent);
        _ui.CloseUi(ent.Owner, KpbFaceUiKey.Face);
    }

    private void OnEmpPulse(Entity<KpbComponent> ent, ref EmpPulseEvent args)
    {
        args.Affected = true;

        var damage = new DamageSpecifier();
        damage.DamageDict.Add("Shock", ent.Comp.DamageFromEmp);
        _damageable.TryChangeDamage(ent, damage);

    }

    private void OnMobStateChanged(Entity<KpbComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState is MobState.Critical)
        {
            var sound = EnsureComp<SpamEmitSoundComponent>(ent);
            sound.Sound = new SoundPathSpecifier("/Audio/Machines/buzz-two.ogg");
            sound.MinInterval = TimeSpan.FromSeconds(15);
            sound.MaxInterval = TimeSpan.FromSeconds(30);
            sound.PopUp = Loc.GetString("sleep-Kpb");
        }
        else
        {
            RemComp<SpamEmitSoundComponent>(ent);
        }
    }
}
