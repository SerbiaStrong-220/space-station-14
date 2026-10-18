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

/// <summary>
/// Persistent scheduling metadata. ServerId must uniquely identify a server sharing the database.
/// </summary>
public sealed record PendingBanData(
    string ServerId,
    DateTime? DueAt,
    PendingBanTrigger Trigger,
    bool Erase,
    bool PostBanInfo)
{
    public bool IsDue(DateTime now, PendingBanTrigger? roundTrigger, bool force)
    {
        return force || Trigger == PendingBanTrigger.Delay && DueAt <= now ||
            Trigger != PendingBanTrigger.Delay && Trigger == roundTrigger;
    }
}

public sealed record ActivatedPendingBan(int Id, bool Erase, bool PostBanInfo);
