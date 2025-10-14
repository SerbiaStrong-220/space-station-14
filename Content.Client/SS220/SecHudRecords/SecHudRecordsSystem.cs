using Content.Shared.SS220.SecHudRecords;
using Content.Shared.StationRecords;
using Robust.Shared.Player;

namespace Content.Client.SS220.SecHudRecords;

public sealed class SecHudRecordsSystem : SharedSecHudRecordsSystem
{
    protected override void VerbAct(NetEntity target, ICommonSession session, StationRecordKey key)
    {
        // DO NOTHING
    }
}
