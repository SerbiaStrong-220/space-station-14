// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.GameTicking;
using Content.Server.Popups;
using Content.Server.Station.Systems;
using Content.Server.StationRecords;
using Content.Server.StationRecords.Systems;
using Content.Shared.Roles;
using Content.Shared.SS220.CriminalRecords;
using Content.Shared.StationRecords;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server.SS220.CriminalRecords;

public sealed class GeneralStationRecordConsoleSystem : EntitySystem
{
    [Dependency] private readonly StationRecordsSystem _stationRecords = default!;
    [Dependency] private readonly CriminalRecordSystem _criminalRecord = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly PopupSystem _popup = default!;

    private static readonly TimeSpan CooldownLagTolerance = TimeSpan.FromSeconds(0.5);

    public override void Initialize()
    {
        SubscribeLocalEvent<CriminalRecordsConsoleComponent, BoundUIOpenedEvent>(UpdateUserInterface);
        SubscribeLocalEvent<CriminalRecordsConsoleComponent, SelectGeneralStationRecord>(OnKeySelected);
        SubscribeLocalEvent<CriminalRecordsConsoleComponent, UpdateCriminalRecordStatus>(OnCriminalStatusUpdate);
        SubscribeLocalEvent<CriminalRecordsConsoleComponent, DeleteCriminalRecordStatus>(OnCriminalStatusDelete);
        SubscribeLocalEvent<RecordModifiedEvent>(OnRecordModified);
        SubscribeLocalEvent<AfterGeneralRecordCreatedEvent>(OnRecordCreated);
    }

    private void OnRecordCreated(AfterGeneralRecordCreatedEvent args)
    {
        var query = EntityManager.EntityQueryEnumerator<CriminalRecordsConsoleComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (_stationSystem.GetOwningStation(uid) == args.Station)
                UpdateUserInterface(uid, comp);
        }
    }

    private void OnRecordModified(RecordModifiedEvent args)
    {
        var query = EntityManager.EntityQueryEnumerator<CriminalRecordsConsoleComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (_stationSystem.GetOwningStation(uid) == args.Station)
                UpdateUserInterface(uid, comp);
        }
    }

    private void UpdateUserInterface<T>(EntityUid uid, CriminalRecordsConsoleComponent component, T ev)
    {
        UpdateUserInterface(uid, component);
    }

    private void OnKeySelected(EntityUid uid, CriminalRecordsConsoleComponent component,
        SelectGeneralStationRecord msg)
    {
        Logger.DebugS("TEST","REVEIVED KEY FROM CLIENT!");
        component.ActiveKey = msg.SelectedKey;
        UpdateUserInterface(uid, component);
    }

    private void OnCriminalStatusUpdate(EntityUid uid, CriminalRecordsConsoleComponent component, UpdateCriminalRecordStatus args)
    {
        if (!component.IsSecurity)
            return;

        if (!component.ActiveKey.HasValue)
            return;

        var currentTime = _gameTicker.RoundDuration();
        if (component.LastEditTime != null && component.LastEditTime + component.EditCooldown - CooldownLagTolerance > currentTime)
        {
            _popup.PopupEntity(Loc.GetString("criminal-status-cooldown-popup"), uid, args.Session);
            return;
        }

        var messageCut = args.Message;
        if (messageCut.Length > component.MaxMessageLength)
            messageCut = messageCut.Substring(0, component.MaxMessageLength);

        if (!_criminalRecord.AddCriminalRecordStatus(component.ActiveKey.Value, messageCut, args.StatusTypeId))
            return;

        component.LastEditTime = currentTime;
    }

    private void OnCriminalStatusDelete(EntityUid uid, CriminalRecordsConsoleComponent component, DeleteCriminalRecordStatus args)
    {
        if (!component.IsSecurity)
            return;

        if (!component.ActiveKey.HasValue)
            return;

        Logger.DebugS("TEST","DELETING!");

        if (!_criminalRecord.RemoveCriminalRecordStatus(component.ActiveKey.Value, args.Time))
            return;
    }

    private void UpdateUserInterface(EntityUid uid,
        CriminalRecordsConsoleComponent? console = null)
    {
        if (!Resolve(uid, ref console))
        {
            return;
        }

        var owningStation = _stationSystem.GetOwningStation(uid);

        if (!TryComp<StationRecordsComponent>(owningStation, out var stationRecordsComponent))
        {
            CriminalRecordConsoleState state = new(null, null, null);
            SetStateForInterface(uid, state);
            return;
        }

        var consoleRecords =
            _stationRecords.GetRecordsOfType<GeneralStationRecord>(owningStation.Value, stationRecordsComponent);

        var listing = new Dictionary<(NetEntity, uint), CriminalRecordShort>();

        var testRecordsAdded = true;
        foreach (var (key, record) in consoleRecords)
        {
            var shortRecord = new CriminalRecordShort(record, console.IsSecurity);
            var deconstructed_key = _stationRecords.Convert(key);
            listing.Add(deconstructed_key, shortRecord);

            //Add test trash records
            if (!testRecordsAdded)
            {
                testRecordsAdded = true;
                var prototypeMan = IoCManager.Resolve<IPrototypeManager>();
                var jobs = prototypeMan.EnumeratePrototypes<JobPrototype>().ToList();
                var rand = new Random();
                for (var i = 0; i < 100; i++)
                {
                    var altkey = (deconstructed_key.Item1, (uint) (deconstructed_key.Item2 + 100 + i));
                    var altRecord = new CriminalRecordShort(record);
                    var jobIndex = rand.Next(jobs.Count);
                    altRecord.JobPrototype = jobs[jobIndex].ID;
                    altRecord.DNA = Guid.NewGuid().ToString();
                    altRecord.Fingerprints = Guid.NewGuid().ToString();
                    listing.Add(altkey, altRecord);
                }
            }
        }

        if (listing.Count == 0)
        {
            console.ActiveKey = null;
        }

        GeneralStationRecord? selectedRecord = null;
        if (console.ActiveKey != null)
        {
            Logger.DebugS("TEST","HAVE KEY!");
            if (_stationRecords.TryGetRecord(
                owningStation.Value,
                _stationRecords.Convert(console.ActiveKey.Value),
                out selectedRecord,
                stationRecordsComponent))
            {
                Logger.DebugS("TEST","SUCCESS!");
            }
        }

        CriminalRecordConsoleState newState = new(console.ActiveKey, selectedRecord, listing);
        SetStateForInterface(uid, newState);
    }

    private void SetStateForInterface(EntityUid uid, CriminalRecordConsoleState newState)
    {
        if (newState.SelectedRecord != null)
            Logger.DebugS("TEST", "FINAL SERVER CHECK ========== SUCCESS!");
        else
            Logger.DebugS("TEST", "FINAL SERVER CHECK ========== FAIL!");

        _userInterface.TrySetUiState(uid, CriminalRecordsUiKey.Key, newState);
    }
}
