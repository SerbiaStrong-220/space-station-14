// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Unary.EntitySystems;
using Content.Server.Body.Systems;
using Content.Server.Hands.Systems;
using Content.Server.Mech.Components;
using Content.Server.Mind;
using Content.Server.Power.EntitySystems;
using Content.Server.Temperature.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.Atmos.Components;
using Content.Shared.Damage;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Mech;
using Content.Shared.Mech.EntitySystems;
using Content.Shared.Mind;
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
using Content.Shared.SS220.Language.Components;
using Content.Shared.SS220.Language.Systems;
using Content.Shared.SS220.Mech.Components;
using Content.Shared.SS220.Mech.Equipment.Components;
using Content.Shared.SS220.Mech.Systems;
using Content.Shared.SS220.MechRobot;
using Content.Shared.Temperature;
using Content.Shared.Tools;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using static Content.Shared.Inventory.InventorySystem;

namespace Content.Server.SS220.Mech.Systems;

/// <inheritdoc/>
public sealed partial class AltMechSystem : SharedAltMechSystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly GasTankSystem _gasTank = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly BatterySystem _battery = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly SharedToolSystem _toolSystem = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly MechPartSystem _parts = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeedModifier = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedLanguageSystem _languages = default!;
    [Dependency] private readonly GasCanisterSystem _gasCanisterSystem = default!;
    [Dependency] private readonly BarotraumaSystem _barotrauma = default!;

    private static readonly ProtoId<ToolQualityPrototype> PryingQuality = "Prying";

    private readonly ProtoId<AlertPrototype> _mechIntegrityAlert = "MechHealth";

    private readonly ProtoId<AlertPrototype> _userHealthAlert = "HumanHealth";

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AltMechComponent, InteractUsingEvent>(OnInteractUsing);
        //SubscribeLocalEvent<AltMechComponent, EntInsertedIntoContainerMessage>(OnInsertBattery);
        SubscribeLocalEvent<AltMechComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<AltMechComponent, GetVerbsEvent<AlternativeVerb>>(OnAlternativeVerb);
        SubscribeLocalEvent<AltMechComponent, MechOpenUiEvent>(OnOpenUi);
        SubscribeLocalEvent<AltMechComponent, RemoveBatteryEvent>(OnRemoveBattery);
        SubscribeLocalEvent<AltMechComponent, MechEntryEvent>(OnMechEntry);
        SubscribeLocalEvent<AltMechComponent, OnMechExitEvent>(OnMechExit);

        SubscribeLocalEvent<MechPartComponent, ChargeChangedEvent>(OnChargeChanged);
        SubscribeLocalEvent<AltMechComponent, DamageChangedEvent>(OnDamageChanged);

        SubscribeLocalEvent<AltMechComponent, DestructionEventArgs>(OnMechDestroyed);

        SubscribeLocalEvent<AltMechPilotComponent, DamageChangedEvent>(OnPilotDamageChanged);
        SubscribeLocalEvent<AltMechPilotComponent, MobStateChangedEvent>(OnPilotStateChanged);
        SubscribeLocalEvent<AltMechPilotComponent, MindAddedMessage>(OnMindAdded);

        SubscribeLocalEvent<MechPartComponent, MechEquipmentRemoveMessage>(OnRemoveEquipmentMessage);
        SubscribeLocalEvent<AltMechComponent, MechPartRemoveMessage>(OnRemovePartMessage);
        SubscribeLocalEvent<AltMechComponent, MechMaintenanceToggleMessage>(OnMaintenanceToggledMessage);
        SubscribeLocalEvent<AltMechComponent, MechAirMixMessage>(OnMixAirMessage);
        SubscribeLocalEvent<AltMechComponent, MechSealMessage>(OnMechSealMessage);
        SubscribeLocalEvent<AltMechComponent, MechDetachTankMessage>(OnTankDetachMessage);

        SubscribeLocalEvent<AltMechComponent, UpdateCanMoveEvent>(OnMechCanMoveEvent);

        SubscribeLocalEvent<AltMechComponent, MassChangedEvent>(OnMassChanged);

        SubscribeLocalEvent<AltMechPilotComponent, ToolUserAttemptUseEvent>(OnToolUseAttempt);
        SubscribeLocalEvent<AltMechPilotComponent, InhaleLocationEvent>(OnInhale);
        //SubscribeLocalEvent<AltMechPilotComponent, ExhaleLocationEvent>(OnExhale);
        //SubscribeLocalEvent<AltMechPilotComponent, AtmosExposedGetAirEvent>(OnExpose);

        SubscribeLocalEvent<AltMechPilotComponent, ModifyChangedTemperatureEvent>(OnTemperatureChange);

        #region Equipment UI message relays
        SubscribeLocalEvent<AltMechComponent, MechGrabberEjectMessage>(ReceiveEquipmentUiMesssages);
        SubscribeLocalEvent<AltMechComponent, MechSoundboardPlayMessage>(ReceiveEquipmentUiMesssages);
        #endregion
    }

    private void OnMechCanMoveEvent(EntityUid uid, AltMechComponent component, UpdateCanMoveEvent args)
    {
        if (component.Broken || component.Integrity <= 0 || !component.Online)
            args.Cancel();
    }

    private void OnInteractUsing(Entity<AltMechComponent> ent, ref InteractUsingEvent args)
    {
        if (!ent.Comp.MaintenanceMode)
            return;

        if (TryComp<GasTankComponent>(args.Used, out var tank))
        {
            InsertTank(ent.Owner, args.Used, ent.Comp, tank);
            //_actionBlocker.UpdateCanMove(ent.Owner);
            //return;
        }

        //if (_toolSystem.HasQuality(args.Used, PryingQuality) && ent.Comp.BatterySlot.ContainedEntity != null)
        //{
        //    var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, ent.Comp.BatteryRemovalDelay,
        //        new RemoveBatteryEvent(), ent.Owner, target: ent.Owner, used: args.Target)
        //    {
        //        BreakOnMove = true
        //    };

        //    _doAfter.TryStartDoAfter(doAfterEventArgs);
        //}
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

    private void OnRemoveBattery(EntityUid uid, AltMechComponent component, RemoveBatteryEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        RemoveBattery(uid, component);
        _actionBlocker.UpdateCanMove(uid);

        args.Handled = true;
    }

    private void OnMapInit(EntityUid uid, AltMechComponent component, MapInitEvent args)
    {
        var xform = Transform(uid);
        // TODO: this should use containerfill
        // TODO: this should just be damage and battery
        component.Integrity = component.MaxIntegrity;
        component.Energy = component.MaxEnergy;

        _actionBlocker.UpdateCanMove(uid);
        Dirty(uid, component);
    }

    private void OnRemoveEquipmentMessage(EntityUid uid, MechPartComponent component, MechEquipmentRemoveMessage args)
    {
        var equip = GetEntity(args.Equipment);

        if (!Exists(equip) || Deleted(equip))
            return;

        //if (!component.EquipmentContainer.ContainedEntities.Contains(equip))
            //return;

        RemoveEquipment(uid, equip, component);
    }

    private void OnRemovePartMessage(Entity<AltMechComponent> ent, ref MechPartRemoveMessage args)
    {
        var equip = (ent.Comp.ContainerDict[args.Part].ContainedEntity);

        if (!Exists(equip) || Deleted(equip))
            return;

        RemovePart((EntityUid)ent.Owner, (EntityUid)equip);

        //UpdateUserInterface(ent.Owner);
    }

    private void OnMaintenanceToggledMessage(Entity<AltMechComponent> ent, ref MechMaintenanceToggleMessage args)
    {
        ent.Comp.MaintenanceMode = args.Toggled;
        Dirty(ent.Owner, ent.Comp);
    }

    private void OnOpenUi(EntityUid uid, AltMechComponent component, MechOpenUiEvent args)
    {
        args.Handled = true;
        ToggleMechUi(uid, component);
    }

    private void OnToolUseAttempt(EntityUid uid, AltMechPilotComponent component, ref ToolUserAttemptUseEvent args)
    {
        if (args.Target == component.Mech)
            args.Cancelled = true;
    }

    private void OnAlternativeVerb(EntityUid uid, AltMechComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || component.Broken)
            return;

        //SS220-AddMechToClothing-start
        if (!TryComp<MechRobotComponent>(uid, out var _))
            return;
        //SS220-AddMechToClothing-end

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
            var openUiVerb = new AlternativeVerb //can't hijack someone else's mech
            {
                Act = () => ToggleMechUi(uid, component, args.User),
                Text = Loc.GetString("mech-ui-open-verb")
            };
            args.Verbs.Add(enterVerb);
            args.Verbs.Add(openUiVerb);
        }
        else if (!IsEmpty(component, uid)) //SS220-AddMechToClothing
        {
            //SS220-AddMechToClothing-start
            if (!TryComp<MechRobotComponent>(uid, out var _))
                return;
            //SS220-AddMechToClothing-end

            var ejectVerb = new AlternativeVerb
            {
                Text = Loc.GetString("mech-verb-exit"),
                Priority = 1, // Promote to top to make ejecting the ALT-click action
                Act = () =>
                {
                    if (args.User == uid || args.User == component.PilotSlot.ContainedEntity)
                    {
                        TryEject(uid, component);
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

        //SS220-AddMechToClothing-start
        if (!TryComp<MechRobotComponent>(ent.Owner, out var _))
            return;
        //SS220-AddMechToClothing-end

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

        //_mind.TransferTo(mindId, ent.Owner, mind: mind);
        _mind.Visit(mindId, ent.Owner);

        _actions.AddAction(ent.Owner, ref ent.Comp.MechUiActionEntity, ent.Comp.MechUiAction, ent.Owner);
        _actions.AddAction(ent.Owner, ref ent.Comp.MechEjectActionEntity, ent.Comp.MechEjectAction, ent.Owner);

        if (!TryComp<DamageableComponent>(ent.Comp.PilotSlot.ContainedEntity, out var damageComp))
            return;

        var health = (4 - ((100 - damageComp.TotalDamage) / 25));
        if (health > 4)
            health = 4;
        //_alerts.ShowAlert(ent.Owner, _userHealthAlert, (short)health);

        _actionBlocker.UpdateCanMove(ent.Owner);

        var integrity = (4 - (ent.Comp.Integrity / (ent.Comp.MaxIntegrity / 4)));
        if (integrity > 4)
            integrity = 4;

        _alerts.ShowAlert(ent.Owner, _mechIntegrityAlert, (short)integrity);

        if(TryComp<AlertsComponent>(pilot,out var pilotAlerts))
        {
            foreach (var alert in pilotAlerts.Alerts)
            {
                _alerts.ShowAlert(ent.Owner, alert.Value);
            }
        }

        if (TryComp<LanguageComponent>(pilot, out var languageComp) && (TryComp<LanguageComponent>(ent.Owner, out var languageCompMech)))
        {
            foreach(var language in languageComp.AvailableLanguages)
            {
                _languages.AddLanguage((ent.Owner, languageCompMech), language);
            }
            Dirty(ent.Owner,languageCompMech);
        }

    }

    public void TransferMindIntoPilot(Entity<AltMechComponent> ent)
    {
        if (ent.Comp.PilotSlot.ContainedEntity == null)
            return;

        if (!TryComp<VisitingMindComponent>(ent.Owner, out var mechVisitComp)) //|| !_mind.TryGetMind(ent.Owner, out var mindId, out var mind))
            return;

        var mindId = mechVisitComp.MindId;

        if (mindId == null)
            return;

        //_mind.TransferTo(mindId, ent.Comp.PilotSlot.ContainedEntity.Value, mind: mind);
        _mind.UnVisit((EntityUid)mindId);

        if (TryComp<AlertsComponent>(ent.Owner, out var alertsComp))
        {
            foreach (var alert in alertsComp.Alerts)
            {
                _alerts.ClearAlert(ent.Owner, alert.Value.Type);
            }
        }

        _actions.AddAction(ent.Comp.PilotSlot.ContainedEntity.Value, ref ent.Comp.MechEjectActionEntity, ent.Comp.MechEjectAction, ent.Owner);
        _actions.AddAction(ent.Comp.PilotSlot.ContainedEntity.Value, ref ent.Comp.MechUiActionEntity, ent.Comp.MechUiAction, ent.Owner);

        if (TryComp<LanguageComponent>(ent.Owner, out var languageCompMech))
        {
            _languages.ClearLanguages((ent.Owner, languageCompMech));
            Dirty(ent.Owner, languageCompMech);
        }
    }

    public void OnAfterHandledState(Entity<AltMechComponent> ent, ref LanguageChangedEvent args)
    {

        if (ent.Comp.PilotSlot.ContainedEntity == null || !TryComp<LanguageComponent>(ent.Comp.PilotSlot.ContainedEntity, out var languageComp))
            return;

        if (args.newLanguageId == "")
            return;

        _languages.TrySelectLanguage((ent.Comp.PilotSlot.ContainedEntity.Value, languageComp), args.newLanguageId);
    }

    public void OnMixAirMessage(Entity<AltMechComponent> ent, ref MechAirMixMessage args)
    {
        if (!TryComp<MechAirComponent>(ent.Owner,out var airComp))
            return;

        if (!TryComp<GasTankComponent>(ent.Comp.TankSlot.ContainedEntity, out var tankComp))
            return;

        _gasCanisterSystem.MixContainerWithPipeNet(airComp.Air, tankComp.Air);
    }

    public void OnMechSealMessage(Entity<AltMechComponent> ent, ref MechSealMessage args)
    {
        ent.Comp.Airtight = args.Toggled;

        if (ent.Comp.PilotSlot == null || ent.Comp.PilotSlot.ContainedEntity == null)
            return;

        if (TryComp<BarotraumaComponent>(ent.Comp.PilotSlot.ContainedEntity, out var barotraumaComp))
            barotraumaComp.HasImmunity = ent.Comp.Airtight;

        Dirty(ent);
    }

    public void OnTankDetachMessage(Entity<AltMechComponent> ent, ref MechDetachTankMessage args)
    {
        if(ent.Comp.TankSlot.ContainedEntity != null)
            _container.Remove(ent.Comp.TankSlot.ContainedEntity.Value, ent.Comp.TankSlot);
    }

    private void OnMechExit(Entity<AltMechComponent> ent, ref OnMechExitEvent args)
    {
        TransferMindIntoPilot(ent);

        if (ent.Comp.PilotSlot.ContainedEntity == null)
            return;

        EntityUid pilot = (EntityUid)ent.Comp.PilotSlot.ContainedEntity;

        if (TryEject(ent.Owner, ent.Comp))
            if (TryComp<BarotraumaComponent>(pilot, out var barotraumaComp))
                barotraumaComp.HasImmunity = ent.Comp.Airtight;
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
        BreakMech(ent.Owner, ent.Comp);
    }

    private void OnDamageChanged(Entity<AltMechComponent> ent, ref DamageChangedEvent args)
    {
        _alerts.ClearAlert(ent.Owner, _mechIntegrityAlert);

        var integrity = ent.Comp.MaxIntegrity - args.Damageable.TotalDamage;

        var severity = (4 - (integrity / (ent.Comp.MaxIntegrity / 4)));

        if (severity > 4)
            severity = 4;

        _alerts.ShowAlert(ent.Owner, _mechIntegrityAlert, (short)severity);

        SetIntegrity(ent.Owner, integrity, ent.Comp);
    }

    private void OnPilotDamageChanged(Entity<AltMechPilotComponent> ent, ref DamageChangedEvent args)
    {
        if (!TryComp<AltMechComponent>(ent.Comp.Mech, out var mechComp))
            return;

        if (ent.Owner != mechComp.PilotSlot.ContainedEntity)
            return;

        var health = 100 - args.Damageable.TotalDamage;
        var severity = (4 - (health / 25));
        if (severity > 4)
            severity = 4;

        //_alerts.ClearAlert(ent.Comp.Mech, _userHealthAlert);
        //_alerts.ShowAlert(ent.Comp.Mech, _userHealthAlert, (short)severity);
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
        //UpdateUserInterface(uid, component);
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

        //base.UpdateUserInterface(uid, component);

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

    public override void BreakMech(EntityUid uid, AltMechComponent? component = null)
    {
        if(!Resolve(uid, ref component))
            return;

        TransferMindIntoPilot((uid, component));

        base.BreakMech(uid, component);

        _ui.CloseUi(uid, MechUiKey.Key);
        _actionBlocker.UpdateCanMove(uid);
    }

    public override bool TryChangeEnergy(EntityUid uid, FixedPoint2 delta, AltMechComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (!base.TryChangeEnergy(uid, delta, component))
            return false;

        var battery = component.ContainerDict["power"].ContainedEntity;
        if (battery == null)
            return false;

        if (!TryComp<BatteryComponent>(battery, out var batteryComp))
            return false;

        _battery.SetCharge(battery!.Value, batteryComp.CurrentCharge + delta.Float(), batteryComp);
        if (batteryComp.CurrentCharge != component.Energy) //if there's a discrepency, we have to resync them
        {
            Log.Debug($"Battery charge was not equal to mech charge. Battery {batteryComp.CurrentCharge}. Mech {component.Energy}");
            component.Energy = batteryComp.CurrentCharge;
            Dirty(uid, component);
        }
        _actionBlocker.UpdateCanMove(uid);
        return true;
    }

    public override void OnStartupServer(Entity<AltMechComponent> ent)
    {
        AddItemsToMech(ent.Owner);
    }

    public void InsertBattery(EntityUid uid, EntityUid toInsert, AltMechComponent? component = null, BatteryComponent? battery = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        if (!Resolve(toInsert, ref battery, false))
            return;

        _container.Insert(toInsert, component.ContainerDict["power"]);
        component.Energy = battery.CurrentCharge;
        component.MaxEnergy = battery.MaxCharge;

        _actionBlocker.UpdateCanMove(uid);

        Dirty(uid, component);
        //UpdateUserInterface(uid, component);
    }

    public void RemoveBattery(EntityUid uid, AltMechComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        _container.EmptyContainer(component.ContainerDict["power"]);
        component.Energy = 0;
        component.MaxEnergy = 0;

        _actionBlocker.UpdateCanMove(uid);

        Dirty(uid, component);
        //UpdateUserInterface(uid, component);
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
        if (!TryComp<AltMechComponent>(ent.Comp.Mech, out var mech) || mech.TankSlot == null || mech.TankSlot.ContainedEntity == null)
        {
            return;
        }

        if (!TryComp<GasTankComponent>(mech.TankSlot.ContainedEntity, out var tankComp))
            return;

        if (mech.Airtight)
        {
            args.Gas = _gasTank.RemoveAirVolume((mech.TankSlot.ContainedEntity.Value, tankComp), args.Respirator.BreathVolume);
            // TODO: Should listen to gas tank updates instead I guess?
            //_alerts.ShowAlert(ent.Owner, "Internals", GetSeverity(ent));
        }
    }
    #endregion
}
