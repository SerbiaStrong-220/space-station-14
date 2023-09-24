// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.GameTicking;
using Content.Server.StationRecords.Systems;
using Content.Shared.Access.Components;
using Content.Shared.SS220.CriminalRecords;
using Content.Shared.StationRecords;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.CriminalRecords;
public sealed class CriminalRecordSystem : EntitySystem
{
    [Dependency] private readonly StationRecordsSystem _stationRecords = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;

    private readonly ISawmill _sawmill = Logger.GetSawmill("CriminalRecords");

    public override void Initialize()
    {
        base.Initialize();
    }

    public CriminalRecordCatalog EnsureRecordCatalog(GeneralStationRecord record)
    {
        if (record.CriminalRecords != null)
            return record.CriminalRecords;

        var catalog = new CriminalRecordCatalog();
        record.CriminalRecords = catalog;
        return catalog;
    }

    // for record removal
    public void UpdateLastRecordTime(CriminalRecordCatalog catalog)
    {
        var biggest = -1;
        foreach (var time in catalog.Records.Keys)
        {
            if (time > biggest)
                biggest = time;
        }

        catalog.LastRecordTime = biggest == -1 ? null : biggest;
    }

    public void UpdateIdCards((NetEntity, uint) key, GeneralStationRecord generalRecord)
    {
        CriminalRecord? criminalRecord = null;
        if (generalRecord.CriminalRecords != null)
        {
            if (generalRecord.CriminalRecords.LastRecordTime is int lastRecordTime)
            {
                generalRecord.CriminalRecords.Records.TryGetValue(lastRecordTime, out criminalRecord);
            }
        }

        var stationUid = GetEntity(key.Item1);
        var query = EntityQueryEnumerator<IdCardComponent, StationRecordKeyStorageComponent>();

        while (query.MoveNext(out var uid, out var idCard, out var keyStorage))
        {
            if (!keyStorage.Key.HasValue)
                continue;

            if (keyStorage.Key.Value.Id != key.Item2 || keyStorage.Key.Value.OriginStation != stationUid)
            {
                continue;
            }

            idCard.CurrentSecurityRecord = criminalRecord;
            EntityManager.Dirty(uid, idCard);
        }
    }

    public bool RemoveCriminalRecordStatus((NetEntity, uint) key, int time)
    {
        var station = GetEntity(key.Item1);

        if (!_stationRecords.TryGetRecord(
            station,
            _stationRecords.Convert(key),
            out GeneralStationRecord? selectedRecord))
        {
            _sawmill.Warning("Tried to add a criminal record but can't get a general record.");
            return false;
        }

        // If it is the same status with the same message - drop it to prevent spam
        var catalog = EnsureRecordCatalog(selectedRecord);
        if (!catalog.Records.Remove(time))
            return false;

        UpdateLastRecordTime(catalog);
        _stationRecords.Synchronize(station);
        UpdateIdCards(key, selectedRecord);

        return true;
    }

    public bool AddCriminalRecordStatus((NetEntity, uint) key, string message, string? statusPrototypeId)
    {
        var station = GetEntity(key.Item1);

        if (!_stationRecords.TryGetRecord(
            station,
            _stationRecords.Convert(key),
            out GeneralStationRecord? selectedRecord))
        {
            _sawmill.Warning("Tried to add a criminal record but can't get a general record.");
            return false;
        }

        ProtoId<CriminalStatusPrototype>? validatedRecordType = null;
        if (statusPrototypeId != null)
        {
            if (_prototype.HasIndex<CriminalStatusPrototype>(statusPrototypeId))
                validatedRecordType = statusPrototypeId;
        }

        // If it is the same status with the same message - drop it to prevent spam
        var catalog = EnsureRecordCatalog(selectedRecord);
        if (catalog.LastRecordTime.HasValue)
        {
            if (catalog.Records.TryGetValue(catalog.LastRecordTime.Value, out var lastRecord))
            {
                if (lastRecord.RecordType?.Id == statusPrototypeId && message == lastRecord.Message)
                    return false;
            }
        }

        var criminalRecord = new CriminalRecord()
        {
            Message = message,
            RecordType = validatedRecordType
        };

        var currentRoundTime = (int) _gameTicker.RoundDuration().TotalSeconds;
        if (!catalog.Records.TryAdd(currentRoundTime, criminalRecord))
            return false;

        catalog.LastRecordTime = currentRoundTime;
        _stationRecords.Synchronize(station);
        UpdateIdCards(key, selectedRecord);
        _sawmill.Debug("Added new criminal record, synchonizing");
        return true;
    }
}
