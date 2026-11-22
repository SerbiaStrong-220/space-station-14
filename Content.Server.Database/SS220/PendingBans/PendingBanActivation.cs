// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Content.Shared.Database.SS220.PendingBans;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database.SS220.PendingBans;

public static class PendingBanActivation
{
    public static async Task<List<ActivatedPendingBan>> ActivatePendingBansAsync(
        ServerDbContext db, string serverId, DateTime time, PendingBanTrigger? roundTrigger = null, bool force = false)
    {
        var pending = await db.Ban
            .Where(b => b.PendingData.ServerId == serverId && b.Unban == null)
            .ToListAsync();

        List<ActivatedPendingBan> activated = [];
        foreach (var ban in pending)
        {
            if (ban.Pending is not { } schedule || !force && !schedule.Trigger.IsDue(time, roundTrigger))
                continue;

            activated.Add(new ActivatedPendingBan(ban.Id, schedule.Erase, schedule.PostBanInfo));
            // The duration begins at activation, not when the administrator scheduled the ban.
            ban.ExpirationTime = ban.ExpirationTime is { } expires ? time + (expires - ban.BanTime) : null;
            ban.BanTime = time;
            ban.Pending = null;
        }

        // One transaction applies the batch. Retrying never inserts duplicate bans.
        if (activated.Count > 0)
            await db.SaveChangesAsync();

        return activated;
    }
}
