// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Threading;
using System.Threading.Tasks;
using Content.Server.SS220.Administration.Connections;

namespace Content.Server.Database
{
    public partial interface IServerDbManager
    {
        Task<List<ConnectionLogRecord>> GetConnectionLogsAsync(
            string ckeyRegex,
            DateTime? before,
            int? beforeId,
            int limit,
            CancellationToken cancel = default);
    }

    public sealed partial class ServerDbManager
    {
        public Task<List<ConnectionLogRecord>> GetConnectionLogsAsync(
            string ckeyRegex,
            DateTime? before,
            int? beforeId,
            int limit,
            CancellationToken cancel = default)
        {
            DbReadOpsMetric.Inc();
            return RunDbCommand(() => _db.GetConnectionLogsAsync(ckeyRegex, before, beforeId, limit, cancel));
        }
    }
}
