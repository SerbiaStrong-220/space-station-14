// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using Content.Server.DeviceNetwork.Components;
using Content.Server.Medical.SuitSensors;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.Forensics.Components;
using Content.Shared.Medical.SuitSensor;
using Content.Shared.Pinpointer;
using Content.Shared.SS220.Pinpointer;
using Robust.Shared.Timing;

namespace Content.Server.SS220.Pinpointer;

public sealed class PinpointerSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private readonly SharedPinpointerSystem _pinpointer = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SuitSensorSystem _suit = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PinpointerComponent, PinpointerTargetPick>(OnPickCrew);
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
        switch (comp.Mode)
        {
            case PinpointerMode.Crew:
                UpdateCrewTrackers(uid, comp);
                break;

            case PinpointerMode.Item:
                UpdateItemTrackers(uid, comp);
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

    private bool IsTargetValid(PinpointerComponent comp)
    {
        return comp.Mode switch
        {
            PinpointerMode.Crew => comp.Sensors.Any(sensor => GetEntity(sensor.Entity) == comp.Target),
            PinpointerMode.Item => comp.TrackedItems.Any(item => item.Entity == GetNetEntity(comp.Target!.Value)),
            _ => false,
        };
    }

    private void OnPickCrew(Entity<PinpointerComponent> ent, ref PinpointerTargetPick args)
    {
        _pinpointer.SetTarget(ent.Owner, GetEntity(args.Target));
        _pinpointer.SetActive(ent.Owner, true);
    }

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

        switch (ent.Comp.Mode)
        {
            case PinpointerMode.Crew:
                _uiSystem.SetUiState(ent.Owner, PinpointerUIKey.Key, new PinpointerCrewUIState(ent.Comp.Sensors));
                break;

            case PinpointerMode.Item:
                _uiSystem.SetUiState(ent.Owner, PinpointerUIKey.Key, new PinpointerItemUIState(ent.Comp.TrackedItems));
                break;
        }
    }
}
