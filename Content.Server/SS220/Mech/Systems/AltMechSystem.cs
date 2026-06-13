// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Systems;
using Content.Server.Hands.Systems;
using Content.Server.Mind;
using Content.Server.Power.EntitySystems;
using Content.Server.Temperature.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.Atmos.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.SS220.Mind;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
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
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly GasTankSystem _gasTank = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly MechPartSystem _parts = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly BlindableSystem _blindable = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly SharedToolSystem _toolSystem = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;

    private readonly ProtoId<AlertPrototype> _mechIntegrityAlert = "MechHealth";

    public PrototypeFlags<ToolQualityPrototype> SawToolQualities = [];

    /// <inheritdoc/>
    public override void Initialize()
    {
        SawToolQualities.Add("Welding", _protoManager);

        base.Initialize();

        SubscribeLocalEvent<AltMechComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<AltMechComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<AltMechComponent, GetVerbsEvent<AlternativeVerb>>(OnAlternativeVerb);
        SubscribeLocalEvent<AltMechComponent, MechBoltsSawedEvent>(OnMechBoltsSawed);
        SubscribeLocalEvent<AltMechComponent, MechOpenUiEvent>(OnOpenUi);
        SubscribeLocalEvent<AltMechComponent, MechEntryEvent>(OnMechEntry);
        SubscribeLocalEvent<AltMechComponent, OnMechExitEvent>(OnMechExited);
        SubscribeLocalEvent<AltMechComponent, MechExitEvent>(OnMechExit);

        SubscribeLocalEvent<MechPartComponent, ChargeChangedEvent>(OnChargeChanged);

        SubscribeLocalEvent<AltMechComponent, DamageChangedEvent>(OnDamageChanged);
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

    private void OnInteractUsing(Entity<AltMechComponent> ent, ref InteractUsingEvent args)
    {
        if (!ent.Comp.MaintenanceMode)
            return;

        if (TryComp<GasTankComponent>(args.Used, out var tank))
            InsertTank(ent.Owner, args.Used, ent.Comp, tank);
    }

    private void InsertTank(EntityUid uid, EntityUid toInsert, AltMechComponent? component = null, GasTankComponent? tank = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        if (!component.MaintenanceMode)
            return;

        if (!Resolve(toInsert, ref tank, false))
            return;

        if (component.TankSlot.ContainedEntity != null)
            _container.EmptyContainer(component.TankSlot);

        _container.Insert(toInsert, component.TankSlot);

        Dirty(uid, component);
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

    private void OnAlternativeVerb(EntityUid uid, AltMechComponent component, GetVerbsEvent<AlternativeVerb> args)//not by-ref because VS tells me i can't
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        if(_hands.TryGetActiveItem(args.User, out var item))
        {
            var text = Loc.GetString("mech-saw-bolts-verb");

            if (component.BoltsSawed)
                text = Loc.GetString("mech-repair-bolts-verb");

            var sawVerb = new AlternativeVerb
            {
                Text = text,
                Priority = 1,
                Act = () =>
                {
                    if (args.User == uid || args.User == component.PilotSlot.ContainedEntity)
                    {
                        return;
                    }

                    _toolSystem.UseTool((EntityUid)item, args.User, uid, 30f, SawToolQualities, new MechBoltsSawedEvent(), 30f);
                }
            };
            args.Verbs.Add(sawVerb);
        }

        if (component.Bolted)
            return;

        if (CanInsert(uid, args.User, component))
        {
            var enterVerb = new AlternativeVerb
            {
                Text = Loc.GetString("mech-verb-enter"),
                Act = () =>
                {
                    var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, component.EntryDelay, new MechEntryEvent(), uid, target: uid)
                    {
                        BreakOnMove = true,
                    };

                    _doAfter.TryStartDoAfter(doAfterEventArgs);
                }
            };
            args.Verbs.Add(enterVerb);
        }
        else if (!IsEmpty(component) && (!component.Bolted || component.BoltsSawed))
        {

            var ejectVerb = new AlternativeVerb
            {
                Text = Loc.GetString("mech-verb-exit"),
                Priority = 1,
                Act = () =>
                {
                    if (args.User == uid || args.User == component.PilotSlot.ContainedEntity)
                    {
                        ExitMech((uid,component));
                        return;
                    }

                    var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, component.ExitDelay, new MechExitEvent(), uid, target: uid)
                    {
                        BreakOnMove = true,
                    };
                    _popup.PopupEntity(Loc.GetString("mech-eject-pilot-alert", ("item", uid), ("user", args.User)), uid, PopupType.Large);

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

        if (!TryInsert(ent.Owner, args.Args.User, ent.Comp))
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
        if (ent.Comp.PilotSlot.ContainedEntity == null)
            return;

        var pilot = (EntityUid)ent.Comp.PilotSlot.ContainedEntity;

        if (TryComp<MobStateComponent>(pilot, out var stateComp) && (stateComp.CurrentState == MobState.Critical || stateComp.CurrentState == MobState.Dead || stateComp.CurrentState == MobState.Invalid))
            return;

        if (!HasComp<MindContainerComponent>(pilot) || !_mind.TryGetMind(pilot, out var mindId, out var mind))
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

        _alerts.ShowAlert(ent.Owner, _mechIntegrityAlert, (short)integrity);

        if (TryComp<AlertsComponent>(pilot,out var pilotAlerts))
        {
            foreach (var alert in pilotAlerts.Alerts)
            {
                _alerts.ShowAlert(ent.Owner, alert.Value);
            }
        }

        if (ent.Comp.ContainerDict["head"].ContainedEntity != null || ent.Comp.Transparent)
        {
            if (!TryComp<BlindableComponent>(ent.Owner, out var blindableCompMech))
                return;

            if (TryComp<BlindableComponent>(pilot, out var blindableComp))
            {
                _blindable.AdjustEyeDamage((ent.Owner, blindableCompMech), (-blindableCompMech.EyeDamage + blindableComp.EyeDamage)); //Mech cannot see anything if it has no eyes
                return;
            }

            _blindable.AdjustEyeDamage((ent.Owner, blindableCompMech), -blindableCompMech.EyeDamage);
            return;
        }

    }

    public void TransferMindIntoPilot(Entity<AltMechComponent> ent)
    {
        if (!TryComp<VisitingMindComponent>(ent.Owner, out var mechVisitComp))
            return;

        var mindId = mechVisitComp.MindId;

        if (mindId == null)
            return;

        _mind.UnVisit((EntityUid)mindId);

        if (TryComp<AlertsComponent>(ent.Owner, out var alertsComp))
        {
            foreach (var alert in alertsComp.Alerts)
            {
                _alerts.ClearAlert(ent.Owner, alert.Value.Type);
            }
        }

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

        var LeftArmEquipment = mechComp.ContainerDict["left-arm"].ContainedEntity;

        if (LeftArmEquipment != null)
        {
            _parts.ProvideItems(mech, (EntityUid)LeftArmEquipment);
        }

        var RightArmEquipment = mechComp.ContainerDict["right-arm"].ContainedEntity;

        if (RightArmEquipment != null)
        {
            _parts.ProvideItems(mech, (EntityUid)RightArmEquipment);
        }
    }

    public void RemoveItemsFromMech(EntityUid mech)
    {
        if (!TryComp<AltMechComponent>(mech, out var mechComp))
            return;

        var LeftArmEquipment = mechComp.ContainerDict["left-arm"].ContainedEntity;

        if (LeftArmEquipment != null)
        {
            _parts.RemoveProvidedItems(mech, (EntityUid)LeftArmEquipment);
        }

        var RightArmEquipment = mechComp.ContainerDict["right-arm"].ContainedEntity;

        if (RightArmEquipment != null)
        {
            _parts.RemoveProvidedItems(mech, (EntityUid)RightArmEquipment);
        }
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

        SetIntegrity(ent.Owner, integrity, ent.Comp);
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

    public void UpdateUserInterface(EntityUid uid, AltMechComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var ev = new MechEquipmentUiStateReadyEvent();
        foreach (var ent in component.ContainerDict.Values)
        {
            if (TryComp<MechPartComponent>(ent.ContainedEntity, out var partcomp) || partcomp == null)
                continue;

            foreach (var equip in component.EquipmentContainer.ContainedEntities)
            {
                RaiseLocalEvent(equip, ev);
            }
        }

        var state = new AltMechBoundUiState
        {
            EquipmentStates = ev.States
        };

        _ui.SetUiState(uid, MechUiKey.Key, state);
    }

    public override void BreakMech(Entity<AltMechComponent> ent)
    {
        TransferMindIntoPilot(ent);

        base.BreakMech(ent);

        _ui.CloseUi(ent.Owner, MechUiKey.Key);
        _actionBlocker.UpdateCanMove(ent.Owner);
    }

    public override void SetIntegrity(EntityUid uid, FixedPoint2 value, AltMechComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        component.Integrity = FixedPoint2.Clamp(value, 0, component.MaxIntegrity);

        if (component.Integrity <= 0)
        {
            component.Broken = true;
            TransferMindIntoPilot((uid,component));
        }
        else if (component.Broken)
        {
            component.Broken = false;
            TransferMindIntoMech((uid, component));
        }

        Dirty(uid, component);
    }

    public override bool TryChangeEnergy(EntityUid uid, FixedPoint2 delta, AltMechComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (!base.TryChangeEnergy(uid, delta, component))
            return false;

        var battery = component.ContainerDict["power"].ContainedEntity;
        if (battery is not {Valid: true } batteryValidated)
            return false;

        if (!TryComp<BatteryComponent>(battery, out var batteryComp))
            return false;

        _battery.UseCharge(batteryValidated, delta.Float());
        if (batteryComp.LastCharge != component.Energy) //if there's a discrepency, we have to resync them
        {
            Log.Debug($"Battery charge was not equal to mech charge. Battery {batteryComp.LastCharge}. Mech {component.Energy}");
            component.Energy = batteryComp.LastCharge;
            Dirty(uid, component);
        }

        _actionBlocker.UpdateCanMove(uid);
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
        {
            return;
        }

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
