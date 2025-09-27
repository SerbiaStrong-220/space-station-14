using Content.Server.PowerCell;
using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.SS220.Kpb;
using Content.Shared.Ninja.Components;
using Content.Shared.Ninja.Systems;
using Content.Shared.Popups;
using Content.Shared.PowerCell.Components;
using Content.Shared.Damage;
using Content.Server.Emp;
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

    private void OnMapInit(EntityUid uid, KpbComponent component, MapInitEvent args)
    {
        UpdateBatteryAlert((uid, component));
        _action.AddAction(uid, ref component.ActionEntity, component.DrainBatteryAction);
        _action.AddAction(uid, ref component.ChangeFaceActionEntity, component.ChangeFaceAction);
        _movementSpeedModifier.RefreshMovementSpeedModifiers(uid);

        if (TryComp<HumanoidAppearanceComponent>(uid, out var appearance) &&
            appearance.MarkingSet.TryGetCategory(MarkingCategories.Snout, out var markings) &&
            markings.Count > 0)
        {
            component.SelectedFace = markings[0].MarkingId;
            Dirty(uid, component);
        }
    }

    private void OnComponentShutdown(EntityUid uid, KpbComponent component, ComponentShutdown args)
    {
        _action.RemoveAction(uid, component.ActionEntity);
        _action.RemoveAction(uid, component.ChangeFaceActionEntity);
    }

    private void OnPowerCellChanged(EntityUid uid, KpbComponent component, PowerCellChangedEvent args)
    {
        if (MetaData(uid).EntityLifeStage >= EntityLifeStage.Terminating)
            return;

        UpdateBatteryAlert((uid, component));

    }

    private void OnToggleAction(EntityUid uid, KpbComponent component, ToggleDrainActionEvent args)
    {
        if (args.Handled)
            return;

        component.DrainActivated = !component.DrainActivated;
        _action.SetToggled(component.ActionEntity, component.DrainActivated);
        args.Handled = true;

        if (component.DrainActivated && _powerCell.TryGetBatteryFromSlot(uid, out var battery, out var _))
        {
            EnsureComp<BatteryDrainerComponent>(uid);
            _batteryDrainer.SetBattery(uid, battery);
        }
        else
            RemComp<BatteryDrainerComponent>(uid);

        var message = component.DrainActivated ? "Kpb-component-ready" : "Kpb-component-disabled";
        _popup.PopupEntity(Loc.GetString(message), uid, uid);
    }
    private void UpdateBatteryAlert(Entity<KpbComponent> ent, PowerCellSlotComponent? slot = null)
    {


        if (!_powerCell.TryGetBatteryFromSlot(ent, out var battery, slot) || battery.CurrentCharge / battery.MaxCharge < 0.01f)
        {
            _alerts.ClearAlert(ent, ent.Comp.BatteryAlert);
        _alerts.ShowAlert(ent, ent.Comp.NoBatteryAlert);

            _movementSpeedModifier.RefreshMovementSpeedModifiers(ent.Owner);
            return;
        }

        var chargePercent = (short) MathF.Round(battery.CurrentCharge / battery.MaxCharge * 10f);

        if (chargePercent == 0 && _powerCell.HasDrawCharge(ent, cell: slot))
            chargePercent = 1;


        _movementSpeedModifier.RefreshMovementSpeedModifiers(ent.Owner);

        _alerts.ClearAlert(ent, ent.Comp.NoBatteryAlert);
        _alerts.ShowAlert(ent, ent.Comp.BatteryAlert, chargePercent);
    }

    private void OnRefreshMovementSpeedModifiers(EntityUid uid, KpbComponent comp, RefreshMovementSpeedModifiersEvent args)
    {
        if (!_powerCell.TryGetBatteryFromSlot(uid, out var battery) || battery.CurrentCharge / battery.MaxCharge < 0.01f)
        {
            args.ModifySpeed(0.2f);
        }
    }

    private void OnOpenFaceAction(EntityUid uid, KpbComponent comp, OpenKpbFaceActionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<ActorComponent>(uid, out var actor))
            return;

        _ui.SetUiState(uid, KpbFaceUiKey.Face, new KpbFaceBuiState(comp.FaceProfile, comp.SelectedFace));
        _ui.TryToggleUi(uid, KpbFaceUiKey.Face, actor.PlayerSession);
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

    private void OnEmpPulse(EntityUid uid, KpbComponent component, ref EmpPulseEvent args)
    {
        args.Affected = true;

        var damage = new DamageSpecifier();
        damage.DamageDict.Add("Shock", 30);
        _damageable.TryChangeDamage(uid, damage);

    }

    private void OnMobStateChanged(EntityUid uid, KpbComponent component, ref MobStateChangedEvent args)
    {
        if (_mobState.IsCritical(uid))
        {
            var sound = EnsureComp<SpamEmitSoundComponent>(uid);
            sound.Sound = new SoundPathSpecifier("/Audio/Machines/buzz-two.ogg");
            sound.MinInterval = TimeSpan.FromSeconds(15);
            sound.MaxInterval = TimeSpan.FromSeconds(30);
            sound.PopUp = Loc.GetString("sleep-Kpb");
        }
        else
        {
            RemComp<SpamEmitSoundComponent>(uid);
        }
    }
}
