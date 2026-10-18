using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Shared.Database.SS220.PendingBans;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database;

public static class PendingBanActivation
{
    public static async Task<List<ActivatedPendingBan>> ActivateAsync(
        ServerDbContext db, string serverId, DateTime now, PendingBanTrigger? roundTrigger = null, bool force = false)
    {
        var pending = await db.Ban
            .Where(b => b.PendingServer == serverId && b.Unban == null)
            .ToListAsync();

        List<ActivatedPendingBan> activated = [];
        foreach (var ban in pending)
        {
            if (ban.Pending is not { } schedule || !schedule.IsDue(now, roundTrigger, force))
                continue;

            activated.Add(new ActivatedPendingBan(ban.Id, schedule.Erase, schedule.PostBanInfo));
            // The duration begins at activation, not when the administrator scheduled the ban.
            ban.ExpirationTime = ban.ExpirationTime is { } expires ? now + (expires - ban.BanTime) : null;
            ban.BanTime = now;
            ban.Pending = null;
        }

        // One transaction applies the batch. Retrying never inserts duplicate bans.
        if (activated.Count > 0)
            await db.SaveChangesAsync();

        return activated;
    }
}
