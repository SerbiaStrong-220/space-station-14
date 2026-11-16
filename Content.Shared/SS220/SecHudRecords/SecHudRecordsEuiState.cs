using Content.Shared.Eui;
using Content.Shared.SS220.CriminalRecords;
using Content.Shared.StationRecords;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.SecHudRecords;

[Serializable, NetSerializable]
public sealed class SecHudRecordsEuiState : EuiStateBase
{
    public NetEntity TargetNetEntity;
    public List<(ProtoId<CriminalStatusPrototype>?, string)> FullCatalog = new();
    public GeneralStationRecord? Record;
}
