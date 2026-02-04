using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Systems;
using Content.Server.Hands.Systems;
using Content.Server.Mech.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.ActionBlocker;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.EntityEffects.Effects.StatusEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Mech;
using Content.Shared.Mech.EntitySystems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Power.Components;
using Content.Shared.SS220.AltMech;
using Content.Shared.SS220.ArmorBlock;
using Content.Shared.SS220.Mech.Components;
using Content.Shared.SS220.Mech.Equipment.Components;
using Content.Shared.SS220.Mech.Systems;
using Content.Shared.SS220.MechRobot; //SS220-AddMechToClothing
using Content.Shared.Tools;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using Content.Shared.Wires;
using JetBrains.FormatRipper.Elf;
using NetCord.Gateway;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server.SS220.Mech.Systems;

/// <inheritdoc/>
public sealed partial class AltMechSystem : SharedAltMechSystem
{
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

    private static readonly ProtoId<ToolQualityPrototype> PryingQuality = "Prying";

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
        SubscribeLocalEvent<MechPartComponent, MechEquipmentRemoveMessage>(OnRemoveEquipmentMessage);
        SubscribeLocalEvent<AltMechComponent, MechPartRemoveMessage>(OnRemovePartMessage);

        SubscribeLocalEvent<AltMechComponent, UpdateCanMoveEvent>(OnMechCanMoveEvent);

        SubscribeLocalEvent<AltMechComponent, MassChangedEvent>(OnMassChanged);

        SubscribeLocalEvent<AltMechPilotComponent, ToolUserAttemptUseEvent>(OnToolUseAttempt);
        SubscribeLocalEvent<AltMechPilotComponent, InhaleLocationEvent>(OnInhale);
        SubscribeLocalEvent<AltMechPilotComponent, ExhaleLocationEvent>(OnExhale);
        SubscribeLocalEvent<AltMechPilotComponent, AtmosExposedGetAirEvent>(OnExpose);

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

        if (TryComp<WiresPanelComponent>(ent.Owner, out var panel) && !panel.Open)
            return;

        if (ent.Comp.ContainerDict["power"].ContainedEntity == null && TryComp<BatteryComponent>(args.Used, out var battery) && TryComp<MechPartComponent>(args.Used, out var _))
        {
            //InsertBattery(ent.Owner, args.Used, ent.Comp, battery);
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

    private void OnInsertBattery(Entity<AltMechComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container != ent.Comp.ContainerDict["power"] || !TryComp<BatteryComponent>(args.Entity, out var battery))
            return;

        ent.Comp.Energy = battery.CurrentCharge;
        ent.Comp.MaxEnergy = battery.MaxCharge;

        Dirty(ent.Owner, ent.Comp);
        _actionBlocker.UpdateCanMove(ent.Owner);
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

        if (!component.EquipmentContainer.ContainedEntities.Contains(equip))
            return;

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

    private void OnMechEntry(EntityUid uid, AltMechComponent component, MechEntryEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        //SS220-AddMechToClothing-start
        if (!TryComp<MechRobotComponent>(uid, out var _))
            return;
        //SS220-AddMechToClothing-end

        if (_whitelistSystem.IsWhitelistFail(component.PilotWhitelist, args.User))
        {
            _popup.PopupEntity(Loc.GetString("mech-no-enter", ("item", uid)), args.User);
            return;
        }

        TryInsert(uid, args.Args.User, component);

        if(!component.Online)
        {
            _actionBlocker.UpdateCanMove(uid);

            args.Handled = true;
            return;
        }

        AddItemsToUser(uid);

        _actionBlocker.UpdateCanMove(uid);

        args.Handled = true;
    }

    private void OnMechExit(Entity<AltMechComponent> ent, ref OnMechExitEvent args)
    {

        RemoveItemsFromUser(ent.Owner);

        TryEject(ent.Owner, ent.Comp);
    }

    public void AddItemsToUser(EntityUid mech)
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

    public void RemoveItemsFromUser(EntityUid mech)
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

    private void OnDamageChanged(Entity<AltMechComponent> ent, ref DamageChangedEvent args)
    {
        var integrity = ent.Comp.MaxIntegrity - args.Damageable.TotalDamage;
        SetIntegrity(ent.Owner, integrity, ent.Comp);
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

        if (!TryComp<ActorComponent>(user, out var actor))
            return;

        _ui.TryToggleUi(uid, MechUiKey.Key, actor.PlayerSession);
        //UpdateUserInterface(uid, component);
    }

    private void ReceiveEquipmentUiMesssages<T>(EntityUid uid, AltMechComponent component, T args) where T : MechEquipmentUiMessage
    {
        if (!TryComp<MechPartComponent>(uid, out var partComp))
            return;

        var ev = new MechEquipmentUiMessageRelayEvent(args);
        var allEquipment = new List<EntityUid>(partComp.EquipmentContainer.ContainedEntities);
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

            foreach (var equip in partcomp.EquipmentContainer.ContainedEntities)
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

    #region Atmos Handling
    private void OnInhale(EntityUid uid, AltMechPilotComponent component, InhaleLocationEvent args)
    {
        if (!TryComp<AltMechComponent>(component.Mech, out var mech) ||
            !TryComp<MechAirComponent>(component.Mech, out var mechAir))
        {
            return;
        }

        if (mech.Airtight)
            args.Gas = mechAir.Air;
    }

    private void OnExhale(EntityUid uid, AltMechPilotComponent component, ExhaleLocationEvent args)
    {
        if (!TryComp<AltMechComponent>(component.Mech, out var mech) ||
            !TryComp<MechAirComponent>(component.Mech, out var mechAir))
        {
            return;
        }

        if (mech.Airtight)
            args.Gas = mechAir.Air;
    }

    private void OnExpose(EntityUid uid, AltMechPilotComponent component, ref AtmosExposedGetAirEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp(component.Mech, out AltMechComponent? mech))
            return;

        if (mech.Airtight && TryComp(component.Mech, out MechAirComponent? air))
        {
            args.Handled = true;
            args.Gas = air.Air;
            return;
        }

        args.Gas =  _atmosphere.GetContainingMixture(component.Mech, excite: args.Excite);
        args.Handled = true;
    }

    private void OnGetFilterAir(EntityUid uid, MechAirComponent comp, ref GetFilterAirEvent args)
    {
        if (args.Air != null)
            return;

        // only airtight mechs get internal air
        if (!TryComp<AltMechComponent>(uid, out var mech) || !mech.Airtight)
            return;

        args.Air = comp.Air;
    }
    #endregion
}
