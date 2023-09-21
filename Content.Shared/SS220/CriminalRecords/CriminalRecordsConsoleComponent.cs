// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Roles;
using Content.Shared.StationRecords;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.CriminalRecords;

[RegisterComponent]
public sealed partial class CriminalRecordsConsoleComponent : Component
{
    public (NetEntity, uint)? ActiveKey { get; set; }
}

[Serializable, NetSerializable]
public enum CriminalRecordsUiKey
{
    Key,
}

[Serializable, NetSerializable, DataDefinition]
public sealed partial class CriminalRecordShort
{
    [DataField(required: true)]
    public string Name;

    [DataField]
    public ProtoId<JobPrototype>? JobPrototype;

    [DataField]
    public ProtoId<CriminalStatusPrototype>? CriminalStatusType;
    public string CriminalStatusNote = "";

    [DataField("dna")]
    public string DNA = "";

    [DataField]
    public string Fingerprints = "";

    public CriminalRecordShort(string name)
    {
        Name = name;
    }

    public CriminalRecordShort(GeneralStationRecord record)
    {
        Name = record.Name;
        JobPrototype = record.JobPrototype;
        DNA = record.DNA ?? "";
        Fingerprints = record.Fingerprint ?? "";
    }
}

[Serializable, NetSerializable]
public sealed class CriminalRecordConsoleState : BoundUserInterfaceState
{
    /// <summary>
    ///     Current selected key.
    /// </summary>
    public (NetEntity, uint)? SelectedKey { get; }
    public GeneralStationRecord? SelectedRecord { get; }
    public Dictionary<(NetEntity, uint), CriminalRecordShort>? RecordListing { get; }
    public CriminalRecordConsoleState(
        (NetEntity, uint)? key,
        GeneralStationRecord? record,
        Dictionary<(NetEntity, uint), CriminalRecordShort>? recordListing)
    {
        SelectedKey = key;
        SelectedRecord = record;
        RecordListing = recordListing;
    }

    public bool IsEmpty() => SelectedKey == null
        && SelectedRecord == null && RecordListing == null;
}
