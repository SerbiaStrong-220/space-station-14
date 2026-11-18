// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Medical.SuitSensors;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.Forensics.Components;
using Content.Shared.Medical.SuitSensor;
using Content.Shared.Medical.SuitSensors;
using Content.Shared.Pinpointer;
using Content.Shared.SS220.Pinpointer;
using Robust.Shared.Timing;
using System.Linq;
using Content.Shared.Access.Systems;

namespace Content.Server.SS220.Pinpointer;

public sealed partial class PinpointerSystem : EntitySystem
{
    [Dependency] private SharedUserInterfaceSystem _uiSystem = default!;
    [Dependency] private SharedPinpointerSystem _pinpointer = default!;
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private SuitSensorSystem _suit = default!;
    [Dependency] private IComponentFactory _componentFactory = default!;
    [Dependency] private SharedIdCardSystem _idCard = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PinpointerComponent, PinpointerTargetPick>(OnPickTarget);
        SubscribeLocalEvent<PinpointerComponent, PinpointerCrewTargetPick>(OnPickCrew);
        SubscribeLocalEvent<PinpointerComponent, PinpointerDnaPick>(OnDnaPicked);
        SubscribeLocalEvent<PinpointerComponent, PinpointerSelectDnaMessage>(OnSelectDna);
        SubscribeLocalEvent<PinpointerComponent, PinpointerAddDnaMessage>(OnAddDna);
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
            case PinpointerMode.Dna:
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

            string? jobIcon = null;

            if (_idCard.TryFindIdCard(sensorComp.User.Value, out var idCard))
                jobIcon = idCard.Comp.JobIcon;

            comp.Sensors.Add(new TrackedItem(GetNetEntity(sensorUid), stateSensor.Name, jobIcon));
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
        if (comp.Target == null)
            return false;

        return comp.Mode switch
        {
            PinpointerMode.Crew => comp.Sensors.Any(sensor => GetEntity(sensor.Entity) == comp.Target),
            PinpointerMode.Item => comp.TrackedItems.Any(item => item.Entity == GetNetEntity(comp.Target.Value)),
            PinpointerMode.Component => comp.Targets.Any(target => GetEntity(target.Entity) == comp.Target),
            PinpointerMode.Dna => comp.TrackedByDnaEntity == comp.Target,
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

            if (!string.IsNullOrEmpty(args.Dna) && !ent.Comp.SavedDnas.Contains(args.Dna))
            {
                ent.Comp.SavedDnas.Add(args.Dna);
                Dirty(ent.Owner, ent.Comp);
            }
            break;
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

            case PinpointerMode.Dna:
                UpdateDnaUI(ent.Owner, ent.Comp);
                break;
        }
    }

    private void OnSelectDna(Entity<PinpointerComponent> ent, ref PinpointerSelectDnaMessage args)
    {
        var uid = ent.Owner;
        var comp = ent.Comp;

        if (comp.Mode != PinpointerMode.Dna || !comp.SavedDnas.Contains(args.Dna))
            return;

        var query = EntityQueryEnumerator<DnaComponent>();
        while (query.MoveNext(out var target, out var dnaComponent))
        {
            if (dnaComponent.DNA != args.Dna)
                continue;

            if (Transform(target).MapUid != Transform(uid).MapUid)
                continue;

            _pinpointer.SetTarget(uid, target);
            _pinpointer.SetActive(uid, true);
            comp.DnaToTrack = args.Dna;
            comp.TrackedByDnaEntity = target;

            UpdateDnaUI(uid, comp);
            return;
        }

        comp.DnaToTrack = args.Dna;
        comp.TrackedByDnaEntity = null;
        _pinpointer.SetTarget(uid, null);
        _pinpointer.SetActive(uid, true);
        UpdateDnaUI(uid, comp);
    }

    private void OnAddDna(Entity<PinpointerComponent> ent, ref PinpointerAddDnaMessage args)
    {
        var uid = ent.Owner;
        var comp = ent.Comp;

        if (comp.Mode != PinpointerMode.Dna || string.IsNullOrWhiteSpace(args.Dna)) 
            return;

        var cleanDna = args.Dna.ToUpper().Trim();
        if (!comp.SavedDnas.Contains(cleanDna))
        {
            comp.SavedDnas.Add(cleanDna);
            Dirty(uid, comp);
        }
        TrySetDnaTarget(uid, comp, cleanDna);
        UpdateDnaUI(uid, comp);
    }

    private void TrySetDnaTarget(EntityUid uid, PinpointerComponent comp, string dna)
    {
        var query = EntityQueryEnumerator<DnaComponent>();
        while (query.MoveNext(out var target, out var dnaComp))
        {
            if (dnaComp.DNA == dna && Transform(target).MapUid == Transform(uid).MapUid)
            {
                _pinpointer.SetTarget(uid, target); 
                _pinpointer.SetActive(uid, true);
                comp.DnaToTrack = dna;
                comp.TrackedByDnaEntity = target;
                return;
            }
        }
        comp.DnaToTrack = dna;
        comp.TrackedByDnaEntity = null;
        _pinpointer.SetTarget(uid, null);
        _pinpointer.SetActive(uid, true);
    }

    private void UpdateDnaUI(EntityUid uid, PinpointerComponent comp)
    {
        float? dir = null;
        float? dist = null;

        if (comp.TrackedByDnaEntity.HasValue && Exists(comp.TrackedByDnaEntity.Value))
        {
            var targetEnt = comp.TrackedByDnaEntity.Value;
            var userPos = Transform(uid).WorldPosition;
            var targetPos = Transform(targetEnt).WorldPosition;
            var delta = targetPos - userPos;
            dir = (float)Math.Atan2(delta.Y, delta.X);
            dist = delta.Length();
        }

        var state = new PinpointerDnaUIState(comp.SavedDnas, comp.DnaToTrack, dir, dist);
        _uiSystem.SetUiState(uid, PinpointerUIKey.Key, state);
    }
}
