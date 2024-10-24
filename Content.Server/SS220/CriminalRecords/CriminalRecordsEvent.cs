// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.CriminalRecords;
using Content.Shared.StationRecords;

namespace Content.Server.SS220.CriminalRecords;

public abstract class CriminalStatusEvent(EntityUid? sender, StationRecordKey key, ref CriminalRecord currentCriminalRecord)
{
    public EntityUid? Sender { get; } = sender;
    public StationRecordKey Key { get; } = key;
    public CriminalRecord CurrentCriminalRecord { get; } = currentCriminalRecord;
}

/// <summary>
/// Just Look to its name
/// </summary>
public sealed class CriminalStatusAdded(EntityUid? sender, StationRecordKey key, ref CriminalRecord criminalRecord)
                                            : CriminalStatusEvent(sender, key, ref criminalRecord)
{ }

/// <summary>
/// Just Look to its name
/// </summary>
public sealed class CriminalStatusDeleted(EntityUid? sender, StationRecordKey key, ref CriminalRecord criminalRecord)
                                            : CriminalStatusEvent(sender, key, ref criminalRecord)
{ }
