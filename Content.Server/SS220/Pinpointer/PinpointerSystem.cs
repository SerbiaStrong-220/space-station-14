// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Medical.SuitSensors;
using Content.Shared.Access.Components;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.Forensics.Components;
using Content.Shared.Medical.SuitSensor;
using Content.Shared.Pinpointer;
using Content.Shared.SS220.Pinpointer;
using Content.Shared.Whitelist;
using Robust.Shared.Timing;
using System.Linq;

namespace Content.Server.SS220.Pinpointer;

public sealed class PinpointerSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedPinpointerSystem _pinpointer = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SuitSensorSystem _suit = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly IComponentFactory _componentFactory = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PinpointerComponent, PinpointerTargetPick>(OnPickTarget);
        SubscribeLocalEvent<PinpointerComponent, PinpointerCrewTargetPick>(OnPickCrew);
        SubscribeLocalEvent<PinpointerComponent, PinpointerDnaPick>(OnDnaPicked);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _gameTiming.CurTime;
        var query = EntityQueryEnumerator<PinpointerComponent, UserInterfaceComponent>();

        while (query.MoveNext(out var uid, out var pinpointer, out _))
        {
            if (curTime < pinpointer.NextUpdate)
                continue;

            pinpointer.NextUpdate = curTime + pinpointer.UpdateInterval;

            UpdateTrackers(uid, pinpointer);
        }
    }

    private void UpdateTrackers(EntityUid uid, PinpointerComponent comp)
    {
        switch (comp.Mode)//ToDo_SS220 fix cursed pinpointer https://github.com/SerbiaStrong-220/DevTeam220/issues/219
        {
            case PinpointerMode.Crew:
                UpdateCrewTrackers(uid, comp);
                break;

            case PinpointerMode.Item:
                UpdateItemTrackers(uid, comp);
                break;

            case PinpointerMode.Component:
                UpdateTargetsTrackers(uid, comp);
                break;
        }

        if (comp.Target != null && !IsTargetValid(comp))
        {
            _pinpointer.SetTarget(uid, null);
            _pinpointer.TogglePinpointer(uid);
        }

        UpdateUserInterface((uid, comp));
        Dirty(uid, comp);
    }

    //ToDo_SS220 fix cursed pinpointer https://github.com/SerbiaStrong-220/DevTeam220/issues/219
    private void UpdateCrewTrackers(EntityUid uid, PinpointerComponent comp)
    {
        comp.Sensors.Clear();

        var sensorQuery = EntityQueryEnumerator<SuitSensorComponent, DeviceNetworkComponent>();

        while (sensorQuery.MoveNext(out var sensorUid, out var sensorComp, out _))
        {
            if (sensorComp.Mode != SuitSensorMode.SensorCords || sensorComp.User == null)
                continue;

            var stateSensor = _suit.GetSensorState(sensorUid);
            if (stateSensor == null)
                continue;

            if (Transform(sensorUid).MapUid != Transform(uid).MapUid)
                continue;

            comp.Sensors.Add(new TrackedItem(GetNetEntity(sensorUid), stateSensor.Name));
        }
    }

    //ToDo_SS220 fix cursed pinpointer https://github.com/SerbiaStrong-220/DevTeam220/issues/219
    private void UpdateItemTrackers(EntityUid uid, PinpointerComponent comp)
    {
        comp.TrackedItems.Clear();

        var itemQuery = EntityQueryEnumerator<TrackedItemComponent>();

        while (itemQuery.MoveNext(out var itemUid, out _))
        {
            if (Transform(itemUid).MapUid != Transform(uid).MapUid)
                continue;

            comp.TrackedItems.Add(new TrackedItem(GetNetEntity(itemUid), MetaData(itemUid).EntityName));
        }

        if (string.IsNullOrEmpty(comp.DnaToTrack) || comp.TrackedByDnaEntity == null)
            return;

        comp.TrackedItems.Add(new TrackedItem(GetNetEntity(comp.TrackedByDnaEntity.Value), comp.DnaToTrack));
    }

    //ToDo_SS220 fix cursed pinpointer https://github.com/SerbiaStrong-220/DevTeam220/issues/219
    private void UpdateTargetsTrackers(EntityUid uid, PinpointerComponent comp)
    {
        comp.Targets.Clear();

        if (comp.TargetsComponent is null)
            return;

        if (!_componentFactory.TryGetRegistration(comp.TargetsComponent, out var registration))
            return;

        var query1 = EntityManager.AllEntityQueryEnumerator(registration.Type);
        while (query1.MoveNext(out var target, out _))
        {
            comp.Targets.Add(new TrackedItem(GetNetEntity(target), MetaData(target).EntityName));
        }
    }

    private bool IsTargetValid(PinpointerComponent comp)
    {
        return comp.Mode switch
        {
            PinpointerMode.Crew => comp.Sensors.Any(sensor => GetEntity(sensor.Entity) == comp.Target),
            PinpointerMode.Item => comp.TrackedItems.Any(item => item.Entity == GetNetEntity(comp.Target!.Value)),
            PinpointerMode.Component => comp.Targets.Any(target => GetEntity(target.Entity) == comp.Target),
            _ => false,
        };
    }

    //ToDo_SS220 fix cursed pinpointer https://github.com/SerbiaStrong-220/DevTeam220/issues/219
    private void OnPickCrew(Entity<PinpointerComponent> ent, ref PinpointerCrewTargetPick args)
    {
        _pinpointer.SetTarget(ent.Owner, GetEntity(args.Target));
        _pinpointer.SetActive(ent.Owner, true);
    }

    //ToDo_SS220 fix cursed pinpointer https://github.com/SerbiaStrong-220/DevTeam220/issues/219
    private void OnPickTarget(Entity<PinpointerComponent> ent, ref PinpointerTargetPick args)
    {
        _pinpointer.SetTarget(ent.Owner, GetEntity(args.Target));
        _pinpointer.SetActive(ent.Owner, true);
    }

    //ToDo_SS220 fix cursed pinpointer https://github.com/SerbiaStrong-220/DevTeam220/issues/219
    private void OnDnaPicked(Entity<PinpointerComponent> ent, ref PinpointerDnaPick args)
    {
        var query = EntityQueryEnumerator<DnaComponent>();

        while (query.MoveNext(out var target, out var dnaComponent))
        {
            if (dnaComponent.DNA != args.Dna)
                continue;

            if (Transform(target).MapUid != Transform(ent.Owner).MapUid)
                continue;

            _pinpointer.SetTarget(ent.Owner, target);
            _pinpointer.SetActive(ent.Owner, true);
            ent.Comp.DnaToTrack = args.Dna;
            ent.Comp.TrackedByDnaEntity = target;
        }
    }

    private void UpdateUserInterface(Entity<PinpointerComponent> ent)
    {
        if (!Exists(ent.Owner) || !_uiSystem.IsUiOpen(ent.Owner, PinpointerUIKey.Key))
            return;

        switch (ent.Comp.Mode)//ToDo_SS220 fix cursed pinpointer https://github.com/SerbiaStrong-220/DevTeam220/issues/219
        {
            case PinpointerMode.Crew:
                _uiSystem.SetUiState(ent.Owner, PinpointerUIKey.Key, new PinpointerCrewUIState(ent.Comp.Sensors));
                break;

            case PinpointerMode.Item:
                _uiSystem.SetUiState(ent.Owner, PinpointerUIKey.Key, new PinpointerItemUIState(ent.Comp.TrackedItems));
                break;

            case PinpointerMode.Component:
                _uiSystem.SetUiState(ent.Owner, PinpointerUIKey.Key, new PinpointerComponentUIState(ent.Comp.Targets));
                break;
        }
    }
}
