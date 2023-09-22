using Content.Server.Station.Systems;
using Content.Server.StationRecords;
using Content.Server.StationRecords.Systems;
using Content.Shared.Roles;
using Content.Shared.SS220.CriminalRecords;
using Content.Shared.StationRecords;
using FastAccessors;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.Server.SS220.CriminalRecords;

public sealed class GeneralStationRecordConsoleSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly StationRecordsSystem _stationRecordsSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<CriminalRecordsConsoleComponent, BoundUIOpenedEvent>(UpdateUserInterface);
        SubscribeLocalEvent<CriminalRecordsConsoleComponent, SelectGeneralStationRecord>(OnKeySelected);
        //SubscribeLocalEvent<GeneralStationRecordConsoleComponent, GeneralStationRecordsFilterMsg>(OnFiltersChanged);
        SubscribeLocalEvent<CriminalRecordsConsoleComponent, RecordModifiedEvent>(UpdateUserInterface);
        SubscribeLocalEvent<CriminalRecordsConsoleComponent, AfterGeneralRecordCreatedEvent>(GigaTest);
    }

    private void GigaTest(EntityUid uid, CriminalRecordsConsoleComponent component, AfterGeneralRecordCreatedEvent ev)
    {
        Logger.DebugS("TEST","NEW RECORD CREATED - UPDATING");
        UpdateUserInterface(uid, component, ev);
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
            _stationRecordsSystem.GetRecordsOfType<GeneralStationRecord>(owningStation.Value, stationRecordsComponent);

        var listing = new Dictionary<(NetEntity, uint), CriminalRecordShort>();

        var testRecordsAdded = true;
        foreach (var (key, record) in consoleRecords)
        {
            var shortRecord = new CriminalRecordShort(record);
            var deconstructed_key = _stationRecordsSystem.Convert(key);
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
            if(_stationRecordsSystem.TryGetRecord(
                owningStation.Value,
                _stationRecordsSystem.Convert(console.ActiveKey.Value),
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
            Logger.DebugS("TEST","FINAL SERVER CHECK ========== SUCCESS!");
        else
            Logger.DebugS("TEST","FINAL SERVER CHECK ========== FAIL!");

        _userInterface.TrySetUiState(uid, CriminalRecordsUiKey.Key, newState);
    }

    private bool IsSkippedRecord(GeneralStationRecordsFilter filter,
        GeneralStationRecord someRecord)
    {
        bool isFilter = filter.Value.Length > 0;
        string filterLowerCaseValue = "";

        if (!isFilter)
            return false;

        filterLowerCaseValue = filter.Value.ToLower();

        return filter.Type switch
        {
            GeneralStationRecordFilterType.Name =>
                !someRecord.Name.ToLower().Contains(filterLowerCaseValue),
            GeneralStationRecordFilterType.Prints => someRecord.Fingerprint != null
                && IsFilterWithSomeCodeValue(someRecord.Fingerprint, filterLowerCaseValue),
            GeneralStationRecordFilterType.DNA => someRecord.DNA != null
                && IsFilterWithSomeCodeValue(someRecord.DNA, filterLowerCaseValue),
        };
    }

    private bool IsFilterWithSomeCodeValue(string value, string filter)
    {
        return !value.ToLower().StartsWith(filter);
    }
}
