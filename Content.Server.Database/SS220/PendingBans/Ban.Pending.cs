// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.ComponentModel.DataAnnotations.Schema;
using Content.Shared.Database.SS220.PendingBans;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database;

public sealed partial class Ban
{
    // Always present for EF table splitting: inactive bans retain the original
    // non-nullable flag/trigger columns and a null pending_server.
    public PendingBanData PendingData { get; set; } = new(null, PendingBanTrigger.Immediate, null, false, false);

    [NotMapped]
    public PendingBanData? Pending
    {
        get => PendingData.ServerId == null ? null : PendingData;
        set => PendingData = value ?? new(null, PendingBanTrigger.Immediate, null, false, false);
    }

    internal static void ConfigurePendingBans(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Ban>().OwnsOne(b => b.PendingData, pending =>
        {
            pending.Ignore(p => p.Trigger);
            pending.Property(p => p.ServerId).HasColumnName("pending_server");
            pending.Property(p => p.TriggerType).HasColumnName("pending_trigger");
            pending.Property(p => p.DueAt).HasColumnName("pending_until");
            pending.Property(p => p.Erase).HasColumnName("pending_erase");
            pending.Property(p => p.PostBanInfo).HasColumnName("pending_post_ban_info");
            pending.HasIndex(p => p.ServerId).HasDatabaseName("IX_ban_pending_server");
        });
        modelBuilder.Entity<Ban>().Navigation(b => b.PendingData).IsRequired();
    }
}
