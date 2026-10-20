// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Content.Server.SS220.Administration.Connections;
using Content.Server.Database;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.SS220.Database
{
    public static class ServerDbBaseExtensions
    {
        public static async Task<List<ConnectionLogRecord>> GetConnectionLogsAsync(
            this ServerDbBase db,
            string ckeyRegex,
            DateTime? before,
            int? beforeId,
            int limit,
            CancellationToken cancel = default)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);
            var ckeyMatcher = new Regex(
                ckeyRegex,
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
                TimeSpan.FromMilliseconds(250));

            return await db.RunWithDbAsync(async context =>
            {
                var query = context.ConnectionLog
                    .AsNoTracking()
                    .AsQueryable();

                var result = new List<ConnectionLogRecord>(limit);
                var cursorTime = before;
                var cursorId = beforeId;
                const int batchSize = 500;

                while (result.Count < limit)
                {
                    var batchQuery = query;

                    // A stable cursor avoids duplicate rows when the player reconnects between pages.
                    if (cursorTime is { } time && cursorId is { } id)
                        batchQuery = batchQuery.Where(log => log.Time < time || log.Time == time && log.Id < id);

                    var logs = await batchQuery
                        .OrderByDescending(log => log.Time)
                        .ThenByDescending(log => log.Id)
                        .Take(batchSize)
                        .Select(log => new
                        {
                            log.Id,
                            log.UserName,
                            log.Time,
                            log.Address,
                            log.Denied,
                            ServerName = log.Server == null ? null : log.Server.Name,
                            log.Trust,
                        })
                        .ToListAsync(cancel);

                    if (logs.Count == 0)
                        break;

                    foreach (var log in logs)
                    {
                        if (!ckeyMatcher.IsMatch(log.UserName))
                            continue;

                        result.Add(new ConnectionLogRecord(
                            log.Id,
                            log.UserName,
                            db.NormalizeDatabaseTimeForSs220(log.Time),
                            log.Address.ToString(),
                            log.Denied,
                            log.ServerName,
                            log.Trust));

                        if (result.Count >= limit)
                            break;
                    }

                    var last = logs[^1];
                    cursorTime = last.Time;
                    cursorId = last.Id;

                    if (logs.Count < batchSize)
                        break;
                }

                return result;
            }, cancel);
        }
    }
}
