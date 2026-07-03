// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Systems;
using Content.Server.Hands.Systems;
using Content.Server.Mind;
using Content.Server.Power.EntitySystems;
using Content.Server.Temperature.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Alert;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Clothing.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Mech;
using Content.Shared.Mech.EntitySystems;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.SS220.AltMech;
using Content.Shared.SS220.Mech.Components;
using Content.Shared.SS220.Mech.Parts.Components;
using Content.Shared.SS220.Mech.Systems;
using Content.Shared.SS220.Mind;
using Content.Shared.Temperature;
using Content.Shared.Tools;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.SS220.Mech.Systems;

/// <inheritdoc/>
public sealed partial class AltMechSystem : SharedAltMechSystem
{
    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private GasTankSystem _gasTank = default!;
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private BatterySystem _battery = default!;
    [Dependency] private ContainerSystem _container = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private MechPartSystem _parts = default!;
    [Dependency] private MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private MindSystem _mind = default!;
    [Dependency] private BlindableSystem _blindable = default!;
    [Dependency] private HandsSystem _hands = default!;
    [Dependency] private SharedToolSystem _toolSystem = default!;
    [Dependency] private IPrototypeManager _protoManager = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private DamageableSystem _damageableSystem = default!;
    [Dependency] private ItemSlotsSystem _itemSlots = default!;

    private readonly ProtoId<AlertPrototype> _mechIntegrityAlert = "MechHealth";

    public PrototypeFlags<ToolQualityPrototype> SawToolQualities = [];

    /// <inheritdoc/>
    public override void Initialize()
    {
        SawToolQualities.Add("Welding", _protoManager);

        base.Initialize();

        SubscribeLocalEvent<AltMechComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<AltMechComponent, GetVerbsEvent<AlternativeVerb>>(OnAlternativeVerb);
        SubscribeLocalEvent<AltMechComponent, MechBoltsSawedEvent>(OnMechBoltsSawed);
        SubscribeLocalEvent<AltMechComponent, MechOpenUiEvent>(OnOpenUi);
        SubscribeLocalEvent<AltMechComponent, MechEntryEvent>(OnMechEntry);
        SubscribeLocalEvent<AltMechComponent, OnMechExitEvent>(OnMechExited);
        SubscribeLocalEvent<AltMechComponent, MechExitEvent>(OnMechExit);

        SubscribeLocalEvent<MechPartComponent, ChargeChangedEvent>(OnChargeChanged);

        SubscribeLocalEvent<AltMechComponent, DamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<AltMechComponent, MechPilotRelayedEvent<GetFireProtectionEvent>>(OnPilotBurning);
        SubscribeLocalEvent<AltMechComponent, DestructionEventArgs>(OnMechDestroyed);

        SubscribeLocalEvent<AltMechPilotComponent, MobStateChangedEvent>(OnPilotStateChanged);
        SubscribeLocalEvent<AltMechPilotComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<AltMechPilotComponent, EntityVisitedEvent>(OnMindVisited);
        SubscribeLocalEvent<AltMechPilotComponent, EntityUnvisitedEvent>(OnMindUnvisited);

        #region MechMenu UI messages
        SubscribeLocalEvent<AltMechComponent, AltMechEquipmentRemoveMessage>(OnRemoveEquipmentMessage);

        SubscribeLocalEvent<AltMechComponent, MechPartRemoveMessage>(OnRemovePartMessage);
        SubscribeLocalEvent<AltMechComponent, MechMaintenanceToggleMessage>(OnMaintenanceToggledMessage);
        SubscribeLocalEvent<AltMechComponent, MechBoltMessage>(OnMechBoltMessage);
        SubscribeLocalEvent<AltMechComponent, MechSealMessage>(OnMechSealMessage);
        SubscribeLocalEvent<AltMechComponent, MechDetachTankMessage>(OnTankDetachMessage);
        #endregion

        SubscribeLocalEvent<AltMechComponent, UpdateCanMoveEvent>(OnMechCanMoveEvent);
        SubscribeLocalEvent<AltMechComponent, MassChangedEvent>(OnMassChanged);
        SubscribeLocalEvent<AltMechPilotComponent, ToolUserAttemptUseEvent>(OnToolUseAttempt);
        SubscribeLocalEvent<AltMechPilotComponent, InhaleLocationEvent>(OnInhale);

        SubscribeLocalEvent<AltMechPilotComponent, ModifyChangedTemperatureEvent>(OnTemperatureChange);

        #region Equipment UI message relays
        SubscribeLocalEvent<AltMechComponent, MechGrabberEjectMessage>(ReceiveEquipmentUiMesssages);
        SubscribeLocalEvent<AltMechComponent, MechSoundboardPlayMessage>(ReceiveEquipmentUiMesssages);
        #endregion
    }

    private void OnMechCanMoveEvent(Entity<AltMechComponent> ent, ref UpdateCanMoveEvent args)
    {
        if (ent.Comp.Broken || ent.Comp.Integrity <= 0 || !ent.Comp.Online)
            args.Cancel();
    }

    private void OnMapInit(Entity<AltMechComponent> ent, ref MapInitEvent args)
    {
        var xform = Transform(ent.Owner);

        _actionBlocker.UpdateCanMove(ent.Owner);
        Dirty(ent);
    }

    private void OnToolUseAttempt(Entity<AltMechPilotComponent> ent, ref ToolUserAttemptUseEvent args)
    {
        if (args.Target == ent.Comp.Mech)
            args.Cancelled = true;
    }

    private void OnMechBoltsSawed(Entity<AltMechComponent> ent, ref MechBoltsSawedEvent args)
    {
        if(ent.Comp.BoltsSawed)
        {
            ent.Comp.BoltsSawed = false;
            Dirty(ent);
            return;
        }
        ent.Comp.BoltsSawed = true;
        Dirty(ent);
    }

    private void OnAlternativeVerb(Entity<AltMechComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;

        if (_hands.TryGetActiveItem(args.User, out var item) && item is { Valid: true } itemValidated)
        {
            var text = Loc.GetString("mech-saw-bolts-verb");

            if (ent.Comp.BoltsSawed)
                text = Loc.GetString("mech-repair-bolts-verb");

            args.Verbs.Add(new AlternativeVerb
            {
                Text = text,
                Priority = 1,
                Act = () =>
                {
                    if (user == ent.Owner || user == ent.Comp.PilotSlot.ContainedEntity)
                        return;

                    _toolSystem.UseTool(itemValidated, user, ent, 30f, SawToolQualities, new MechBoltsSawedEvent(), 30f);
                }
            });
        }

        if (ent.Comp.Bolted)
            return;

        if (CanInsert(ent, args.User))
        {
            var enterVerb = new AlternativeVerb
            {
                Text = Loc.GetString("mech-verb-enter"),
                Act = () =>
                {
                    var doAfterEventArgs = new DoAfterArgs(EntityManager, user, ent.Comp.EntryDelay, new MechEntryEvent(), ent, target: ent)
                    {
                        BreakOnMove = true,
                    };

                    _doAfter.TryStartDoAfter(doAfterEventArgs);
                }
            };
            args.Verbs.Add(enterVerb);
        }
        else if (!IsEmpty(ent.Comp) && (!ent.Comp.Bolted || ent.Comp.BoltsSawed))
        {

            var ejectVerb = new AlternativeVerb
            {
                Text = Loc.GetString("mech-verb-exit"),
                Priority = 1,
                Act = () =>
                {
                    if (user == ent.Owner || user == ent.Comp.PilotSlot.ContainedEntity)
                    {
                        ExitMech(ent);
                        return;
                    }

                    var doAfterEventArgs = new DoAfterArgs(EntityManager, user, ent.Comp.ExitDelay, new MechExitEvent(), ent, target: ent)
                    {
                        BreakOnMove = true,
                    };
                    _popup.PopupEntity(Loc.GetString("mech-eject-pilot-alert", ("item", ent), ("user", user)), ent, PopupType.Large);

                    _doAfter.TryStartDoAfter(doAfterEventArgs);
                }
            };
            args.Verbs.Add(ejectVerb);
        }
    }

    private void OnMechEntry(Entity<AltMechComponent> ent, ref MechEntryEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (_whitelistSystem.IsWhitelistFail(ent.Comp.PilotWhitelist, args.User))
        {
            _popup.PopupEntity(Loc.GetString("mech-no-enter", ("item", ent.Owner)), args.User);
            return;
        }

        if (!TryInsert(ent, args.Args.User))
            return;

        if (TryComp<BarotraumaComponent>(ent.Comp.PilotSlot.ContainedEntity, out var barotraumaComp))
            barotraumaComp.HasImmunity = ent.Comp.Airtight;

        if (!ent.Comp.Online)
        {
            _actionBlocker.UpdateCanMove(ent.Owner);

            args.Handled = true;
            return;
        }

        TransferMindIntoMech(ent);

        args.Handled = true;
    }

    public void TransferMindIntoMech(Entity<AltMechComponent> ent)
    {
        if (ent.Comp.PilotSlot.ContainedEntity is not { Valid: true } pilotValidated)
            return;

        if (TryComp<MobStateComponent>(pilotValidated, out var stateComp) && (stateComp.CurrentState == MobState.Critical || stateComp.CurrentState == MobState.Dead || stateComp.CurrentState == MobState.Invalid))
            return;

        if (!HasComp<MindContainerComponent>(pilotValidated) || !_mind.TryGetMind(pilotValidated, out var mindId, out var mind))
            return;

        _mind.Visit(mindId, ent.Owner);

        if (!TryComp<DamageableComponent>(ent.Comp.PilotSlot.ContainedEntity, out var damageComp))
            return;

        var health = (4 - ((100 - _damageableSystem.GetTotalDamage(ent.Owner)) / 25));
        if (health > 4)
            health = 4;

        _actionBlocker.UpdateCanMove(ent.Owner);

        var integrity = (4 - (ent.Comp.Integrity / (ent.Comp.MaxIntegrity / 4)));
        if (integrity > 4)
            integrity = 4;

        if (TryComp<AlertsComponent>(ent.Owner, out var alertsComp))
        {
            foreach (var alert in alertsComp.Alerts)
                _alerts.ClearAlert(ent.Owner, alert.Value.Type);
        }

        _alerts.ShowAlert(ent.Owner, _mechIntegrityAlert, (short)integrity);

        if (TryComp<AlertsComponent>(pilotValidated, out var pilotAlerts))
        {
            foreach (var alert in pilotAlerts.Alerts)
                _alerts.ShowAlert(ent.Owner, alert.Value);
        }

        if (ent.Comp.ContainerDict["head"].ContainedEntity != null || ent.Comp.Transparent)
        {
            if (!TryComp<BlindableComponent>(ent.Owner, out var blindableCompMech))
                return;

            if (TryComp<AltMechPilotComponent>(pilotValidated, out var pilotComp))
            {
                _blindable.AdjustEyeDamage((ent.Owner, blindableCompMech), -blindableCompMech.EyeDamage + pilotComp.PilotEyeDamage); //Mech cannot see anything if it has no eyes
                return;
            }

            _blindable.AdjustEyeDamage((ent.Owner, blindableCompMech), -blindableCompMech.EyeDamage);
            return;
        }

    }

    public void TransferMindIntoPilot(Entity<AltMechComponent> ent)
    {
        if (TryComp<AlertsComponent>(ent.Owner, out var alertsComp))
        {
            foreach (var alert in alertsComp.Alerts)
                _alerts.ClearAlert(ent.Owner, alert.Value.Type);
        }

        if (!TryComp<VisitingMindComponent>(ent.Owner, out var mechVisitComp))
            return;

        var mindId = mechVisitComp.MindId;

        if (mindId is not { Valid: true } mindValidated)
            return;

        _mind.UnVisit(mindValidated);

        if (ent.Comp.PilotSlot.ContainedEntity == null)
            return;
    }

    private void OnMechExited(Entity<AltMechComponent> ent, ref OnMechExitEvent args)
    {
        ExitMech(ent);
    }

    private void ExitMech(Entity<AltMechComponent> ent)
    {
        if (ent.Comp.PilotSlot.ContainedEntity == null)
            return;

        if (ent.Comp.Bolted && !ent.Comp.BoltsSawed)
        {
            _popup.PopupEntity(Loc.GetString("mech-bolted-no-exit"), (EntityUid)ent.Comp.PilotSlot.ContainedEntity);
            return;
        }

        EntityUid pilot = (EntityUid)ent.Comp.PilotSlot.ContainedEntity;

        TransferMindIntoPilot(ent);
        if (TryComp<BarotraumaComponent>(pilot, out var barotraumaComp))
            barotraumaComp.HasImmunity = false;

        _alerts.ShowAlert(pilot, "Internals", 2);

        if (!TryEject(ent))
        {
            TransferMindIntoMech(ent);

            if (barotraumaComp != null)
                barotraumaComp.HasImmunity = true;
        }
    }

    public void AddItemsToMech(EntityUid mech)
    {
        if (!TryComp<AltMechComponent>(mech, out var mechComp))
            return;

        var leftArmEquipment = mechComp.ContainerDict["left-arm"].ContainedEntity;

        if (leftArmEquipment is { Valid: true } leftArmEquipmentValidated)
            _parts.ProvideItems(mech, leftArmEquipmentValidated);

        var rightArmEquipment = mechComp.ContainerDict["right-arm"].ContainedEntity;

        if (rightArmEquipment is { Valid: true } rightArmEquipmentValidated)
            _parts.ProvideItems(mech, rightArmEquipmentValidated);
    }

    public void RemoveItemsFromMech(EntityUid mech)
    {
        if (!TryComp<AltMechComponent>(mech, out var mechComp))
            return;

        var leftArmEquipment = mechComp.ContainerDict["left-arm"].ContainedEntity;

        if (leftArmEquipment is { Valid: true } leftArmEquipmentValidated)
            _parts.RemoveProvidedItems(mech, leftArmEquipmentValidated);

        var rightArmEquipment = mechComp.ContainerDict["right-arm"].ContainedEntity;

        if (rightArmEquipment is { Valid: true } rightArmEquipmentValidated)
            _parts.RemoveProvidedItems(mech, rightArmEquipmentValidated);
    }

    private void OnMechDestroyed(Entity<AltMechComponent> ent, ref DestructionEventArgs args)
    {
        TransferMindIntoPilot(ent);
        BreakMech(ent);
    }

    private void OnDamageChanged(Entity<AltMechComponent> ent, ref DamageChangedEvent args)
    {
        _alerts.ClearAlert(ent.Owner, _mechIntegrityAlert);

        var integrity = ent.Comp.MaxIntegrity - _damageableSystem.GetTotalDamage(ent.Owner);

        var severity = (4 - (integrity / (ent.Comp.MaxIntegrity / 4)));

        if (severity > 4)
            severity = 4;

        _alerts.ShowAlert(ent.Owner, _mechIntegrityAlert, (short)severity);

        SetIntegrity(ent, integrity);
    }

    private void OnPilotBurning(Entity<AltMechComponent> ent, ref MechPilotRelayedEvent<GetFireProtectionEvent> args)
    {
        if (!TryComp<FireProtectionComponent>(ent.Owner, out var fireProtectionComp))
            return;

        if (!ent.Comp.Airtight || !ent.Comp.Sealable)
            return;

        args.Args.Reduce(fireProtectionComp.Reduction);
    }

    private void OnPilotStateChanged(Entity<AltMechPilotComponent> ent, ref MobStateChangedEvent args)
    {
        if(args.NewMobState == MobState.Critical || args.NewMobState == MobState.Dead || args.NewMobState == MobState.Invalid)
        {
            if (!HasComp<VisitingMindComponent>(ent.Comp.Mech) || !_mind.TryGetMind(ent.Owner, out var mindId, out var mind))
                return;

            _mind.UnVisit(mindId);
            return;
        }
        if (!HasComp<MindContainerComponent>(ent.Owner) || !_mind.TryGetMind(ent.Owner, out var mindIdpilot, out var pilotmind))
            return;

        _mind.Visit(mindIdpilot, ent.Comp.Mech, mind: pilotmind);
    }

    private void OnMindAdded(Entity<AltMechPilotComponent> ent, ref MindAddedMessage args)
    {
        if (!TryComp<AltMechComponent>(ent.Comp.Mech, out var mechComp))
            return;

        if(mechComp.Online && !mechComp.Broken)
        {
            TransferMindIntoMech((ent.Comp.Mech, mechComp));
            return;
        }
        _mind.UnVisit(args.Mind);
    }

    private void OnMindVisited(Entity<AltMechPilotComponent> ent, ref EntityVisitedEvent args)
    {
        if (!TryComp<AltMechComponent>(ent.Comp.Mech, out var mechComp))
            return;

        if (mechComp.Online && !mechComp.Broken)
        {
            TransferMindIntoMech((ent.Comp.Mech, mechComp));
            return;
        }
        _mind.UnVisit(args.MindEntity);
    }

    private void OnMindUnvisited(Entity<AltMechPilotComponent> ent, ref EntityUnvisitedEvent args)
    {
        if (!TryComp<AltMechComponent>(ent.Comp.Mech, out var mechComp))
            return;

        if (mechComp.Online && !mechComp.Broken)
        {
            TransferMindIntoMech((ent.Comp.Mech, mechComp));
            return;
        }
        _mind.UnVisit(args.MindEntity);
    }

    private void OnMassChanged(Entity<AltMechComponent> ent, ref MassChangedEvent args)
    {
        FixedPoint2 maxMass = 1;

        if (TryComp<MechChassisComponent>(ent.Comp.ContainerDict["chassis"].ContainedEntity, out var chassisComp))
            maxMass = chassisComp.MaximalMass;

        var massDiff = ent.Comp.OverallMass - maxMass;

        if (massDiff < 0)
            massDiff = 0;

        FixedPoint2 massRel = 1 - (massDiff / maxMass);

        ent.Comp.MovementSpeedModifier = massRel.Float();

        if (TryComp<MovementSpeedModifierComponent>(ent.Owner, out var movementComp) && ent.Comp.Online)
            _movementSpeedModifier.ChangeBaseSpeed(ent.Owner, ent.Comp.OverallBaseMovementSpeed * ent.Comp.MovementSpeedModifier * 0.5f, ent.Comp.OverallBaseMovementSpeed * ent.Comp.MovementSpeedModifier, ent.Comp.OverallBaseAcceleration * ent.Comp.MovementSpeedModifier);
    }

    private void ToggleMechUi(EntityUid uid, AltMechComponent? component = null, EntityUid? user = null)
    {
        if (!Resolve(uid, ref component))
            return;

        user ??= component.PilotSlot.ContainedEntity;

        if (user == null)
            return;

        if (!TryComp<ActorComponent>(uid, out var actor))
        {
            if(TryComp<ActorComponent>(user, out var actorPilot))
            {
                _ui.TryToggleUi(uid, MechUiKey.Key, actorPilot.PlayerSession);
                return;
            }
            return;
        }

        _ui.TryToggleUi(uid, MechUiKey.Key, actor.PlayerSession);
    }

    private void ReceiveEquipmentUiMesssages<T>(EntityUid uid, AltMechComponent component, T args) where T : MechEquipmentUiMessage
    {
        if (!TryComp<MechPartComponent>(uid, out var partComp))
            return;

        var ev = new MechEquipmentUiMessageRelayEvent(args);
        var allEquipment = new List<EntityUid>(component.EquipmentContainer.ContainedEntities);
        var argEquip = GetEntity(args.Equipment);

        foreach (var equipment in allEquipment)
        {
            if (argEquip == equipment)
                RaiseLocalEvent(equipment, ev);
        }
    }

    public void UpdateUserInterface(Entity<AltMechComponent> ent)
    {
        if (!HasComp<AltMechComponent>(ent))
            return;

        var ev = new MechEquipmentUiStateReadyEvent();
        foreach (var part in ent.Comp.ContainerDict.Values)
        {
            if (TryComp<MechPartComponent>(part.ContainedEntity, out var partcomp) || partcomp == null)
                continue;

            foreach (var equip in ent.Comp.EquipmentContainer.ContainedEntities)
                RaiseLocalEvent(equip, ev);
        }

        var state = new AltMechBoundUiState { EquipmentStates = ev.States };

        _ui.SetUiState(ent.Owner, MechUiKey.Key, state);
    }

    public override void BreakMech(Entity<AltMechComponent> ent)
    {
        TransferMindIntoPilot(ent);

        base.BreakMech(ent);

        _ui.CloseUi(ent.Owner, MechUiKey.Key);
        _actionBlocker.UpdateCanMove(ent.Owner);
    }

    public override void SetIntegrity(Entity<AltMechComponent> ent, FixedPoint2 value)
    {
        if (!HasComp<AltMechComponent>(ent))
            return;

        ent.Comp.Integrity = FixedPoint2.Clamp(value, 0, ent.Comp.MaxIntegrity);

        if (ent.Comp.Integrity <= 0)
        {
            ent.Comp.Broken = true;
            TransferMindIntoPilot(ent);
        }
        else if (ent.Comp.Broken)
        {
            ent.Comp.Broken = false;
            TransferMindIntoMech(ent);
        }

        Dirty(ent);
    }

    public override bool TryChangeEnergy(Entity<AltMechComponent> ent, FixedPoint2 delta)
    {
        if (!base.TryChangeEnergy(ent,delta))
            return false;

        var battery = ent.Comp.ContainerDict["power"].ContainedEntity;

        if (battery is not { Valid: true } batteryValidated)
            return false;

        if (!TryComp<BatteryComponent>(battery, out var batteryComp))
            return false;

        _battery.UseCharge(batteryValidated, delta.Float());

        if (batteryComp.LastCharge != ent.Comp.Energy) //if there's a discrepency, we have to resync them
        {
            Log.Debug($"Battery charge was not equal to mech charge. Battery {batteryComp.LastCharge}. Mech {ent.Comp.Energy}");
            ent.Comp.Energy = batteryComp.LastCharge;
            Dirty(ent);
        }

        _actionBlocker.UpdateCanMove(ent);
        return true;
    }

    public override void OnStartupServer(Entity<AltMechComponent> ent)
    {
        AddItemsToMech(ent.Owner);
    }

    private void OnTemperatureChange(Entity<AltMechPilotComponent> ent, ref ModifyChangedTemperatureEvent args)
    {
        if (!TryComp<TemperatureProtectionComponent>(ent.Comp.Mech, out var mechComp))
            return;

        var coefficient = args.TemperatureDelta < 0
            ? mechComp.CoolingCoefficient
            : mechComp.HeatingCoefficient;

        args.TemperatureDelta *= coefficient;
    }

    #region Atmos Handling
    private void OnInhale(Entity<AltMechPilotComponent> ent, ref InhaleLocationEvent args)
    {
        if (!TryComp<AltMechComponent>(ent.Comp.Mech, out var mechComp) || mechComp.TankSlot == null || mechComp.TankSlot.ContainedEntity == null)
            return;

        if (!TryComp<GasTankComponent>(mechComp.TankSlot.ContainedEntity, out var tankComp))
            return;

        if (mechComp.Airtight)
        {
            args.Gas = _gasTank.RemoveAirVolume((mechComp.TankSlot.ContainedEntity.Value, tankComp), args.Respirator.BreathVolume);
            _alerts.ShowAlert(ent.Owner, "Internals", GetSeverity((ent.Comp.Mech, mechComp)));
        }
    }

    private short GetSeverity(Entity<AltMechComponent> ent)
    {
        short severity = 2;

        if (ent.Comp.Airtight && ent.Comp.TankSlot.ContainedEntity != null)
        {
            --severity;

            if (TryComp<GasTankComponent>(ent.Comp.TankSlot.ContainedEntity, out var tankComp) && tankComp.IsLowPressure)
                --severity;
        }

        return severity;
    }
    #endregion
}
