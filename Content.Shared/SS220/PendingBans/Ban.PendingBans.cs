// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Database.SS220.PendingBans;

namespace Content.Shared.Administration;

public sealed partial record Ban
{
    public PendingBanTrigger PendingTrigger { get; init; }
    public uint DelayMinutes { get; init; }
}
