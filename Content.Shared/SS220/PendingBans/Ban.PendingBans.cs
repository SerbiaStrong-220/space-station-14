using Content.Shared.Database.SS220.PendingBans;

namespace Content.Shared.Administration;

public sealed partial record Ban
{
    public PendingBanTrigger PendingTrigger { get; init; }
    public uint DelayMinutes { get; init; }
}
