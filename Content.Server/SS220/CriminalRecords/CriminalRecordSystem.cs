// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using Content.Server.Administration.Logs;
using Content.Server.GameTicking;
using Content.Server.StationRecords.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Database;
using Content.Shared.SS220.CriminalRecords;
using Content.Shared.StationRecords;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.CriminalRecords;

public sealed class CriminalRecordSystem : SharedCriminalRecordSystem
{
    [Dependency] private readonly StationRecordsSystem _stationRecords = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IAdminLogManager _logManager = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;

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


    /// <returns>Null if no IDCard was updated or Uid of updated IDCard</returns>
    public EntityUid? UpdateIdCards(StationRecordKey key, GeneralStationRecord generalRecord)
    {
        CriminalRecord? criminalRecord = null;
        if (generalRecord.CriminalRecords?.LastRecordTime is { } lastRecordTime)
            generalRecord.CriminalRecords.Records.TryGetValue(lastRecordTime, out criminalRecord);

        var stationUid = key.OriginStation;
        var query = EntityQueryEnumerator<IdCardComponent, StationRecordKeyStorageComponent>();

        while (query.MoveNext(out var uid, out var idCard, out var keyStorage))
        {
            if (!keyStorage.Key.HasValue)
                continue;

            if (keyStorage.Key.Value.Id != key.Id || keyStorage.Key.Value.OriginStation != stationUid)
                continue;

            idCard.CurrentSecurityRecord = criminalRecord;
            Dirty(uid, idCard);
            return uid;
        }
        return null;
    }

    public bool RemoveCriminalRecordStatus(StationRecordKey key, int time, EntityUid? sender = null)
    {
        if (!_stationRecords.TryGetRecord(key, out GeneralStationRecord? selectedRecord))
        {
            Log.Warning("Tried to add a criminal record but can't get a general record.");
            return false;
        }

        // If it is the same status with the same message - drop it to prevent spam
        var catalog = EnsureRecordCatalog(selectedRecord);
        if (!catalog.Records.Remove(time))
            return false;

        UpdateLastRecordTime(catalog);
        _stationRecords.Synchronize(key.OriginStation);
        var cardId = UpdateIdCards(key, selectedRecord);
        if (cardId.HasValue && TryGetLastRecord(key, out _, out var currentCriminalRecord))
            RaiseLocalEvent<CriminalStatusEvent>(cardId.Value, new CriminalStatusDeleted(sender, key, ref currentCriminalRecord));

        if (sender != null)
        {
            _logManager.Add(
                LogType.SecutiyRecords,
                LogImpact.High,
                $"{ToPrettyString(sender):user} DELETED a criminal record for {selectedRecord.Name} with ID {time}"
            );
        }

        return true;
    }

    public bool TryGetLastRecord(
        StationRecordKey key,
        [NotNullWhen(true)] out GeneralStationRecord? stationRecord,
        [NotNullWhen(true)] out CriminalRecord? criminalRecord)
    {
        criminalRecord = null;

        if (!_stationRecords.TryGetRecord(key, out stationRecord))
            return false;

        if (stationRecord.CriminalRecords is not { } catalog)
            return false;

        criminalRecord = catalog.GetLastRecord();
        return criminalRecord != null;
    }

    public bool AddCriminalRecordStatus(StationRecordKey key, string message, string? statusPrototypeId, EntityUid? sender = null)
    {
        if (!_stationRecords.TryGetRecord(key, out GeneralStationRecord? selectedRecord))
        {
            Log.Warning("Tried to add a criminal record but can't get a general record.");
            return false;
        }

        var catalog = EnsureRecordCatalog(selectedRecord);

        ProtoId<CriminalStatusPrototype>? validatedRecordType = null;
        if (statusPrototypeId != null)
        {
            if (_prototype.HasIndex<CriminalStatusPrototype>(statusPrototypeId))
                validatedRecordType = statusPrototypeId;
        }

        // If it is the same status with the same message - drop it to prevent spam
        if (catalog.LastRecordTime.HasValue)
        {
            if (catalog.Records.TryGetValue(catalog.LastRecordTime.Value, out var lastRecord))
            {
                if (lastRecord.RecordType?.Id == statusPrototypeId && message == lastRecord.Message)
                    return false;
            }
        }

        var criminalRecord = new CriminalRecord
        {
            Message = message,
            RecordType = validatedRecordType
        };

        var currentRoundTime = (int)_gameTicker.RoundDuration().TotalSeconds;
        if (!catalog.Records.TryAdd(currentRoundTime, criminalRecord))
            return false;

        catalog.LastRecordTime = currentRoundTime;
        _stationRecords.Synchronize(key.OriginStation);
        var cardId = UpdateIdCards(key, selectedRecord);
        if (cardId.HasValue)
            RaiseLocalEvent<CriminalStatusEvent>(cardId.Value, new CriminalStatusAdded(sender, key, ref criminalRecord));

        Log.Debug("Added new criminal record, synchonizing");

        if (sender != null)
        {
            _logManager.Add(
                LogType.SecutiyRecords,
                statusPrototypeId == "execute" ? LogImpact.Extreme : LogImpact.High,
                $"{ToPrettyString(sender):user} sent a new criminal record for {selectedRecord.Name} with ID {currentRoundTime} with type '{statusPrototypeId ?? "none"}' with message: {message}"
            );
        }

        return true;
    }
}
