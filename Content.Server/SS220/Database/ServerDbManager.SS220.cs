// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Threading;
using System.Threading.Tasks;
using Content.Server.Database;
using Content.Server.SS220.Administration.Connections;

namespace Content.Server.SS220.Database
{
    public static class ServerDbManagerExtensions
    {
        public static Task<List<ConnectionLogRecord>> GetConnectionLogsAsync(
            this IServerDbManager manager,
            string ckeyRegex,
            DateTime? before,
            int? beforeId,
            int limit,
            CancellationToken cancel = default)
        {
            if (manager is not ServerDbManager dbManager)
                throw new InvalidOperationException("Connection logs require the standard server database manager.");

            ServerDbManager.DbReadOpsMetric.Inc();
            return dbManager.RunDbCommandForSs220(
                () => dbManager.Database.GetConnectionLogsAsync(ckeyRegex, before, beforeId, limit, cancel));
        }
    }
}
