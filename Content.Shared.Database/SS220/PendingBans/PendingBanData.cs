// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
namespace Content.Shared.Database.SS220.PendingBans;

[Serializable]
public enum PendingBanTrigger : byte
{
    Immediate,
    Delay,
    RoundEnd,
    RoundStart,
    Lobby,
}

public interface IPendingBanTrigger
{
    bool IsDue(DateTime time, PendingBanTrigger? roundTrigger);
}

/// <summary>
/// Only a delayed trigger stores an absolute deadline, which survives server restarts.
/// </summary>
public sealed record DelayPendingBanTrigger(DateTime DueAt) : IPendingBanTrigger
{
    public bool IsDue(DateTime time, PendingBanTrigger? roundTrigger)
    {
        return time >= DueAt;
    }
}

public sealed record RoundStatusPendingBanTrigger(PendingBanTrigger Status) : IPendingBanTrigger
{
    public bool IsDue(DateTime time, PendingBanTrigger? roundTrigger)
    {
        return Status == roundTrigger;
    }
}

/// <summary>
/// Persistent scheduling metadata. ServerId must uniquely identify a server sharing the database.
/// </summary>
public sealed record PendingBanData(
    string? ServerId,
    PendingBanTrigger TriggerType,
    DateTime? DueAt,
    bool Erase,
    bool PostBanInfo)
{
    public const uint MaxDelayMinutes = 365 * 24 * 60;

    public PendingBanData(string serverId, IPendingBanTrigger trigger, bool erase, bool postBanInfo)
        : this(serverId,
            trigger switch
            {
                DelayPendingBanTrigger => PendingBanTrigger.Delay,
                RoundStatusPendingBanTrigger round => round.Status,
                _ => throw new ArgumentException("Unknown pending ban trigger.", nameof(trigger)),
            },
            (trigger as DelayPendingBanTrigger)?.DueAt,
            erase,
            postBanInfo)
    {
    }

    // These two scalar fields retain the existing database columns; runtime conditions
    // remain separate types so a round-status trigger does not carry a deadline.
    public IPendingBanTrigger Trigger => TriggerType == PendingBanTrigger.Delay
        ? new DelayPendingBanTrigger(DueAt!.Value)
        : new RoundStatusPendingBanTrigger(TriggerType);
}

public sealed record ActivatedPendingBan(int Id, bool Erase, bool PostBanInfo);
