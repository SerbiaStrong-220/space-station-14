using System.Linq;
using System.Globalization;
using Content.Server.Chat.Systems;
using Content.Server.Climbing;
using Content.Server.Forensics;
using Content.Server.Power.Components;
using Content.Server.Station.Systems;
using Content.Server.StationRecords.Systems;
using Content.Shared.Inventory;
using Content.Shared.StationRecords;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.SS220.CryopodSSD;

public sealed class CryopodSSDSystem : EntitySystem
{
    [Dependency] private readonly StationRecordsSystem _stationRecordsSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;
    [Dependency] private readonly StationSystem _stationSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<CryopodSSDComponent, PowerChangedEvent>(OnPowerChanged);
        
        SubscribeLocalEvent<CryopodSSDComponent, ClimbedOnEvent>(OnClimbedOn);
    }

    private void OnClimbedOn(EntityUid uid, CryopodSSDComponent component, ClimbedOnEvent args)
    {
        
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
        }
        
        

        UndressEntity(args.Climber);
        
        _entityManager.QueueDeleteEntity(args.Climber);
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

        return;
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


    private void OnPowerChanged(EntityUid uid, CryopodSSDComponent component, ref PowerChangedEvent args)
    {
        
    }
}