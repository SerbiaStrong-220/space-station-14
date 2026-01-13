using Content.Server.Station.Systems;
using Content.Server.StationRecords.Systems;
using Content.Shared.CrewManifest;
using Content.Shared.PDA;
using Content.Shared.StationRecords;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Records;

public sealed class LinkStationRecordsSystem : EntitySystem
{
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly StationRecordsSystem _stationRecords = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    public static readonly ProtoId<TagPrototype> LinkRecordsTag = "LinkRecordsPda";

    public override void Initialize()
    {
        SubscribeNetworkEvent<RequestLinkIdToRecord>(OnRequestLinkIdToRecord);
    }

    private void OnRequestLinkIdToRecord(RequestLinkIdToRecord ev)
    {
        var pda = GetEntity(ev.Pda);
        if (!TryComp<PdaComponent>(pda, out var pdaComp) || !_tag.HasTag(pda, LinkRecordsTag))
            return;

        var idCard = pdaComp.ContainedId;
        if (idCard == null)
            return;

        var station = _station.GetOwningStation(idCard);
        if (station == null)
            return;

        (NetEntity, uint)? rec = (GetNetEntity(station.Value), ev.Key);
        var recordKey = _stationRecords.Convert(rec);
        if (recordKey == null)
            return;

        if (!TryComp<StationRecordKeyStorageComponent>(idCard, out var storage))
            return;

        storage.Key = recordKey.Value;
        Dirty(idCard.Value, storage);
    }
}
