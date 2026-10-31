// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Linq;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Server.StationRecords.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.Roles;
using Content.Shared.SS220.Hop;
using Content.Shared.StationRecords;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Hop;

public sealed class HopConsoleSystem : EntitySystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly StationJobsSystem _jobs = default!;
    [Dependency] private readonly StationRecordsSystem _records = default!;
    [Dependency] private readonly AccessReaderSystem _access = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly ISharedAdminLogManager _logs = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    private float _refreshElapsed;

    public override void Initialize()
    {
        Subs.BuiEvents<HopConsoleComponent>(HopConsoleUiKey.Key, subs =>
        {
            subs.Event<HopConsoleSaveJobMessage>(OnSaveJob);
            subs.Event<HopConsoleAddRecordMessage>(OnAddRecord);
            subs.Event<HopConsoleRemoveRecordMessage>(OnRemoveRecord);
            subs.Event<HopConsoleRefreshMessage>(OnRefresh);
            subs.Event<BoundUIOpenedEvent>(OnOpened);
        });
        SubscribeLocalEvent<BureaucraticJobsChangedEvent>(OnBureaucraticJobsChanged);
    }

    private void OnBureaucraticJobsChanged(BureaucraticJobsChangedEvent args)
    {
        if (!TryComp<StationJobsComponent>(args.Station, out var jobs))
            return;
        var restrictions = EnsureComp<HopStationRestrictionsComponent>(args.Station);
        restrictions.LockedJobs.UnionWith(args.ChangedJobs);
        var candidates = restrictions.LockedJobs
            .Where(id => CrewJob(jobs, id) && jobs.JobList.ContainsKey(id))
            .OrderByDescending(id => jobs.JobList[id] ?? int.MaxValue)
            .ThenBy(id => id.ToString(), StringComparer.Ordinal).ToList();
        if (candidates.Count == 0)
            return;
        var priority = args.PreferredJob is { } preferred && candidates.Contains(preferred)
            ? preferred
            : candidates[0];
        restrictions.ForcedPriority = priority;
        jobs.PriorityJobs.Clear();
        jobs.PriorityJobs.Add(priority);
        _jobs.RefreshJobsAvailable();
    }

    private int OccupiedSlots(StationJobsComponent jobs, string jobId) =>
        jobs.PlayerJobs.Values.Sum(roles => roles.Count(role => role == jobId));

    public override void Update(float frameTime)
    {
        _refreshElapsed += frameTime;
        if (_refreshElapsed < 2f)
            return;
        _refreshElapsed = 0;
        var query = EntityQueryEnumerator<HopConsoleComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            foreach (var actor in _ui.GetActors(uid, HopConsoleUiKey.Key).ToArray())
            {
                if (!Authorized(uid, actor))
                    _ui.CloseUi(uid, HopConsoleUiKey.Key, actor);
            }
            if (_ui.IsUiOpen(uid, HopConsoleUiKey.Key))
                UpdateUi(uid);
        }
    }

    private bool Authorized(EntityUid console, EntityUid actor)
    {
        return TryComp<AccessReaderComponent>(console, out var reader) && _access.IsAllowed(actor, console, reader);
    }

    private void OnOpened(Entity<HopConsoleComponent> console, ref BoundUIOpenedEvent args)
    {
        if (!Authorized(console, args.Actor))
        {
            _ui.CloseUi(console.Owner, HopConsoleUiKey.Key, args.Actor);
            return;
        }
        UpdateUi(console);
    }

    private void OnRefresh(Entity<HopConsoleComponent> console, ref HopConsoleRefreshMessage args)
    {
        if (Authorized(console, args.Actor))
            UpdateUi(console);
        else
            _ui.CloseUi(console.Owner, HopConsoleUiKey.Key, args.Actor);
    }

    private bool TryStation(EntityUid console, out EntityUid station)
    {
        station = _station.GetOwningStation(console) ?? EntityUid.Invalid;
        return HasComp<StationJobsComponent>(station) && HasComp<StationRecordsComponent>(station);
    }

    private bool CrewJob(StationJobsComponent jobs, string id)
    {
        jobs.SetupAvailableJobs.TryGetValue(id, out var setup);
        var hasSlots = jobs.JobList.TryGetValue(id, out var slots);
        return _prototypes.TryIndex<JobPrototype>(id, out var job) &&
            HopJobPolicy.IsCrewJob(job.SetPreference, setup, hasSlots, slots);
    }

    /// <summary>Uses the actual station configuration, including explicitly configured unlimited passenger slots.</summary>
    public HopConsoleState BuildState(EntityUid console)
    {
        if (!TryStation(console, out var station))
            return new HopConsoleState(string.Empty, new(), new(), available: false);

        var jobs = Comp<StationJobsComponent>(station);
        TryComp<HopStationRestrictionsComponent>(station, out var restrictions);
        var entries = new List<HopJobEntry>();
        foreach (var id in jobs.SetupAvailableJobs.Keys.Union(jobs.JobList.Keys))
        {
            if (!CrewJob(jobs, id))
                continue;

            jobs.SetupAvailableJobs.TryGetValue(id, out var setup);
            var hasSlots = jobs.JobList.TryGetValue(id, out var slots);
            entries.Add(new HopJobEntry(id, _prototypes.Index(id).LocalizedName,
                HopJobPolicy.AvailableSlots(hasSlots, slots), jobs.PriorityJobs.Contains(id),
                HopJobPolicy.ConfiguredSlots(setup, 0), HopJobPolicy.ConfiguredSlots(setup, 1),
                OccupiedSlots(jobs, id), restrictions?.LockedJobs.Contains(id) == true,
                restrictions?.ForcedPriority != null));
        }
        entries.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase));
        var records = _records.GetRecordsOfType<GeneralStationRecord>(station)
            .Select(x => new HopRecordEntry(x.Item1, x.Item2.Name, x.Item2.JobTitle, x.Item2.JobPrototype))
            .OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
        return new HopConsoleState(Name(station), entries, records, debugJobs: jobs.SpawnAllJobs);
    }

    private void UpdateUi(EntityUid console) => _ui.SetUiState(console, HopConsoleUiKey.Key, BuildState(console));

    private void Reply(EntityUid console, EntityUid actor, HopConsoleOperation operation, bool success, string text)
    {
        var state = BuildState(console);
        _ui.SetUiState(console, HopConsoleUiKey.Key, state);
        _ui.ServerSendUiMessage(console, HopConsoleUiKey.Key,
            new HopConsoleResultMessage(operation, success, text, state), actor);
    }

    private bool Validate(EntityUid console, EntityUid actor, HopConsoleOperation operation, out EntityUid station)
    {
        station = EntityUid.Invalid;
        if (!Authorized(console, actor))
        {
            _ui.CloseUi(console, HopConsoleUiKey.Key, actor);
            return false;
        }
        if (TryStation(console, out station))
            return true;
        Reply(console, actor, operation, false, "hop-console-no-station");
        return false;
    }

    private void OnSaveJob(Entity<HopConsoleComponent> console, ref HopConsoleSaveJobMessage args)
    {
        const HopConsoleOperation op = HopConsoleOperation.SaveJob;
        if (!Validate(console, args.Actor, op, out var station))
            return;
        var jobs = Comp<StationJobsComponent>(station);
        if (!CrewJob(jobs, args.JobId) || !HopJobPolicy.ValidSlots(args.Slots))
        {
            Reply(console, args.Actor, op, false, "hop-console-error-input");
            return;
        }
        var hasSlots = jobs.JobList.TryGetValue(args.JobId, out var slots);
        var oldSlots = HopJobPolicy.AvailableSlots(hasSlots, slots);
        var oldPriority = jobs.PriorityJobs.Contains(args.JobId);
        if (oldSlots != args.ExpectedSlots || oldPriority != args.ExpectedPriority)
        {
            Reply(console, args.Actor, op, false, "hop-console-error-conflict");
            return;
        }
        TryComp<HopStationRestrictionsComponent>(station, out var restrictions);
        jobs.SetupAvailableJobs.TryGetValue(args.JobId, out var setup);
        var maximum = HopJobPolicy.MaximumAvailable(HopJobPolicy.ConfiguredSlots(setup, 1), OccupiedSlots(jobs, args.JobId));
        if (!HopJobPolicy.CanSetSlots(args.JobId, args.Slots, oldSlots, maximum,
                restrictions?.LockedJobs.Contains(args.JobId) == true))
        {
            Reply(console, args.Actor, op, false, restrictions?.LockedJobs.Contains(args.JobId) == true
                ? "hop-console-error-event" : "hop-console-error-cap");
            return;
        }
        if (restrictions?.ForcedPriority is { } forced && args.Priority != (forced == args.JobId))
        {
            Reply(console, args.Actor, op, false, "hop-console-error-priority-event");
            return;
        }
        if (args.Slots != oldSlots)
        {
            if (args.Slots is { } amount)
                _jobs.TrySetJobSlot(station, args.JobId, amount, createSlot: true);
            else
                _jobs.MakeJobUnlimited(station, args.JobId);
        }
        if (args.Priority)
        {
            jobs.PriorityJobs.Clear();
            jobs.PriorityJobs.Add(args.JobId);
        }
        else
            jobs.PriorityJobs.Remove(args.JobId);
        _jobs.RefreshJobsAvailable();
        _logs.Add(LogType.Action,
            $"{ToPrettyString(args.Actor)} changed {args.JobId} on {ToPrettyString(station)}: available slots {oldSlots?.ToString() ?? "unlimited"} -> {args.Slots?.ToString() ?? "unlimited"}, priority {oldPriority} -> {args.Priority}.");
        Reply(console, args.Actor, op, true, "hop-console-saved");
    }

    private void OnAddRecord(Entity<HopConsoleComponent> console, ref HopConsoleAddRecordMessage args)
    {
        const HopConsoleOperation op = HopConsoleOperation.AddRecord;
        if (!Validate(console, args.Actor, op, out var station))
            return;
        var name = args.Name.Trim();
        var title = args.JobTitle.Trim();
        if (string.IsNullOrWhiteSpace(name) || name.Length > _config.GetCVar(CCVars.MaxNameLength) ||
            title.Length > _config.GetCVar(CCVars.MaxIdJobLength) ||
            !CrewJob(Comp<StationJobsComponent>(station), args.JobId))
        {
            Reply(console, args.Actor, op, false, "hop-console-error-input");
            return;
        }
        if (_records.GetRecordsOfType<GeneralStationRecord>(station)
            .Any(x => string.Equals(x.Item2.Name, name, StringComparison.CurrentCultureIgnoreCase)))
        {
            Reply(console, args.Actor, op, false, "hop-console-error-duplicate");
            return;
        }
        var job = _prototypes.Index<JobPrototype>(args.JobId);
        var record = new GeneralStationRecord
        {
            Name = name,
            JobTitle = title.Length == 0 ? job.LocalizedName : title,
            JobIcon = job.Icon,
            JobPrototype = job.ID,
            DisplayPriority = job.RealDisplayWeight,
            CriminalRecords = new()
        };
        var key = _records.AddRecordEntry(station, record);
        if (!key.IsValid())
        {
            Reply(console, args.Actor, op, false, "hop-console-error-input");
            return;
        }
        _records.Synchronize(key);
        _logs.Add(LogType.Action, $"{ToPrettyString(args.Actor)} added crew record {key.Id}: {name} ({job.ID}) on {ToPrettyString(station)}.");
        Reply(console, args.Actor, op, true, "hop-console-record-added");
    }

    private void OnRemoveRecord(Entity<HopConsoleComponent> console, ref HopConsoleRemoveRecordMessage args)
    {
        const HopConsoleOperation op = HopConsoleOperation.RemoveRecord;
        if (!Validate(console, args.Actor, op, out var station))
            return;
        var key = new StationRecordKey(args.Key, station);
        if (!_records.TryGetRecord<GeneralStationRecord>(key, out var record) || !_records.RemoveRecord(key))
        {
            Reply(console, args.Actor, op, false, "hop-console-error-missing");
            return;
        }
        _logs.Add(LogType.Action, $"{ToPrettyString(args.Actor)} removed crew record {key.Id}: {record.Name} on {ToPrettyString(station)}.");
        Reply(console, args.Actor, op, true, "hop-console-record-removed");
    }
}
