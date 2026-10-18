using System;
using System.ComponentModel.DataAnnotations.Schema;
using Content.Shared.Database.SS220.PendingBans;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database;

[Index(nameof(PendingServer))]
public sealed partial class Ban
{
    public string? PendingServer { get; set; }
    public DateTime? PendingUntil { get; set; }
    public PendingBanTrigger PendingTrigger { get; set; }
    public bool PendingErase { get; set; }
    public bool PendingPostBanInfo { get; set; }

    [NotMapped]
    public PendingBanData? Pending
    {
        get => PendingServer == null
            ? null
            : new PendingBanData(PendingServer, PendingUntil, PendingTrigger, PendingErase, PendingPostBanInfo);
        set
        {
            PendingServer = value?.ServerId;
            PendingUntil = value?.DueAt;
            PendingTrigger = value?.Trigger ?? PendingBanTrigger.Immediate;
            PendingErase = value?.Erase ?? false;
            PendingPostBanInfo = value?.PostBanInfo ?? false;
        }
    }
}
