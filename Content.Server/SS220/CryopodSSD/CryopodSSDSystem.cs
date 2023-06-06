using System.Linq;
using System.Globalization;
using Content.Server.Chat.Systems;
using Content.Server.Climbing;
using Content.Server.Forensics;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Server.Mind.Components;
using Content.Server.Objectives;
using Content.Server.Objectives.Conditions;
using Content.Server.Objectives.Interfaces;
using Content.Server.Station.Systems;
using Content.Server.StationRecords.Systems;
using Content.Shared.Inventory;
using Content.Shared.SS220.CryopodSSD;
using Content.Shared.StationRecords;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.SS220.CryopodSSD;


/// <summary>
/// SS220
/// Implemented leaving from game via climbing in cryopod
/// <seealso cref="CryopodSSDComponent"/>
/// </summary>
public sealed class CryopodSSDSystem : EntitySystem
{
    [Dependency] private readonly StationRecordsSystem _stationRecordsSystem = default!;
    [Dependency] private readonly IObjectivesManager _objectivesManager = default!;
    [Dependency] private readonly MindTrackerSystem _mindTrackerSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    
    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = Logger.GetSawmill("cryopodSSD");

        SubscribeLocalEvent<CryopodSSDComponent, ClimbedOnEvent>(OnClimbedOn);
        SubscribeLocalEvent<CryopodSSDComponent, BoundUIOpenedEvent>(UpdateUserInterface);
    }

    private void UpdateUserInterface<T>(EntityUid uid, CryopodSSDComponent component, T ev)
    {
        UpdateUserInterface(uid, component);
    }

    private void UpdateUserInterface(EntityUid uid, CryopodSSDComponent? component)
    {
        if (!Resolve(uid, ref component))
        {
            return;
        }

        var state = new CryopodSSDState(component.StoredEntities);
        SetStateForInterface(uid, state);
    }

    private void SetStateForInterface(EntityUid uid, CryopodSSDState state)
    {
        var ui = _userInterface.GetUiOrNull(uid, CryopodSSDKey.Key);
        if (ui is not null)
        {
            _userInterface.SetUiState(ui, state);
        }
    }

    private void OnClimbedOn(EntityUid uid, CryopodSSDComponent component, ClimbedOnEvent args)
    {
        if (!TryComp(args.Climber, out MindComponent? mind) || !mind.HasMind)
        {
            _sawmill.Error($"{ToPrettyString(args.Instigator)} tries to put in cryo non-playable entity {ToPrettyString(args.Climber)}");
            return;
        }

        var station = _stationSystem.GetOwningStation(uid);

        if (station is not null)
        {
            DeleteEntityRecord(args.Climber, station.Value, out var job);
            
            _chatSystem.DispatchStationAnnouncement(station.Value, 
                Loc.GetString(
                    "cryopodSSD-entered-cryo",
                    ("character", MetaData(args.Climber).EntityName),
                    ("job", CultureInfo.CurrentCulture.TextInfo.ToTitleCase(job))),
                Loc.GetString("cryopodSSD-sender"));
            
            component.StoredEntities.Add($"{MetaData(args.Climber).EntityName} - [{job}] - {_gameTiming.RealTime}");
        }
        
        

        UndressEntity(args.Climber);
        
        _sawmill.Info($"{ToPrettyString(args.Instigator)} put {ToPrettyString(args.Climber)} in cryo");
        
        _entityManager.QueueDeleteEntity(args.Climber);
        
        ReplaceKillEntityObjectives(args.Climber);
    }

    private void ReplaceKillEntityObjectives(EntityUid uid)
    {
        var objectiveToReplace = new List<Objective>();
        foreach (var mind in _mindTrackerSystem.AllMinds)
        {
            if (mind.OwnedEntity is null)
            {
                continue;
            }
            
            objectiveToReplace.Clear();
            
            foreach (var objective in mind.AllObjectives)
            {
                if (objective.Conditions.Any(condition => (condition as KillPersonCondition)?.IsTarget(uid) ?? false))
                {
                    objectiveToReplace.Add(objective);
                }
            }

            foreach (var objective in objectiveToReplace)
            {
                mind.TryRemoveObjective(objective);
                var newObjective = _objectivesManager.GetRandomObjective(mind, "TraitorObjectiveGroups");
                if (newObjective is null || !mind.TryAddObjective(newObjective))
                {
                    _sawmill.Error($"{ToPrettyString(mind.OwnedEntity.Value)}'s target get in cryo, so he lost his objective and didn't get a new one");
                    continue;
                }
                    
                _sawmill.Error($"{ToPrettyString(mind.OwnedEntity.Value)}'s target get in cryo, so he get a new one");
            }
        }

    }

    private void UndressEntity(EntityUid uid)
    {
        if (!TryComp<InventoryComponent>(uid, out var inventoryComponent))
        {
            return;
        }

        if (!_prototypeManager.TryIndex(inventoryComponent.TemplateId,
                out InventoryTemplatePrototype? inventoryTemplate))
        {
            return;
        }

        foreach (var slot in inventoryTemplate.Slots)
        {
            _inventorySystem.TryUnequip(uid, slot.Name);
        }
    }

    private void DeleteEntityRecord(EntityUid uid, EntityUid station, out string job)
    {
        job = string.Empty;
        var stationRecord = FindEntityStationRecordKey(station, uid);

        if (stationRecord is null)
        {
            return;
        }

        job = stationRecord.Value.Item2.JobTitle;

        _stationRecordsSystem.RemoveRecord(station, stationRecord.Value.Item1);
    }
    
    private (StationRecordKey, GeneralStationRecord)? FindEntityStationRecordKey(EntityUid station, EntityUid uid)
    {
        if (TryComp<DnaComponent>(uid, out var dnaComponent))
        {
            var stationRecords = _stationRecordsSystem.GetRecordsOfType<GeneralStationRecord>(station);
            var result = stationRecords.FirstOrNull(records => records.Item2.DNA == dnaComponent.DNA);
            if (result is not null)
            {
                return result.Value;
            }
        }

        return null;
    }
}