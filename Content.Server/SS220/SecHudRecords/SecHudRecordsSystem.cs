using Content.Server.EUI;
using Content.Server.SS220.CriminalRecords;
using Content.Server.SS220.SecHudRecords.EUI;
using Content.Server.StationRecords.Systems;
using Content.Shared.SS220.CriminalRecords;
using Content.Shared.SS220.SecHudRecords;
using Content.Shared.StationRecords;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.SecHudRecords;

public sealed class SecHudRecordsSystem : SharedSecHudRecordsSystem
{
    [Dependency] private readonly StationRecordsSystem _stationRecords = default!;
    [Dependency] private readonly CriminalRecordSystem _record = default!;
    [Dependency] private readonly EuiManager _eui = default!;

    protected override void VerbAct(NetEntity netTarget, ICommonSession session, StationRecordKey key)
    {
        _stationRecords.TryGetRecord<GeneralStationRecord>(key, out var generalRecord);

        List<(ProtoId<CriminalStatusPrototype>?, string)> fullCatalog = new();

        if (_record.GetRecordCatalog(key, out var catalog))
        {
            foreach (var record in catalog.Records)
            {
                fullCatalog.Add((record.Value.RecordType, record.Value.Message));
            }
        }

        _eui.OpenEui(new SecHudRecordsEui(netTarget, fullCatalog, generalRecord), session);
    }
}
