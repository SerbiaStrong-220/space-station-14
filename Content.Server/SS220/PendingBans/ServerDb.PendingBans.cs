using System.Threading.Tasks;
using Content.Shared.Database.SS220.PendingBans;

namespace Content.Server.Database;

public partial interface IServerDbManager
{
    Task<List<ActivatedPendingBan>> ActivatePendingBansAsync(
        string serverId, DateTime now, PendingBanTrigger? roundTrigger = null, bool force = false);
}

public sealed partial class ServerDbManager
{
    public Task<List<ActivatedPendingBan>> ActivatePendingBansAsync(
        string serverId, DateTime now, PendingBanTrigger? roundTrigger = null, bool force = false)
    {
        return RunDbCommand(() => _db.ActivatePendingBansAsync(serverId, now, roundTrigger, force));
    }
}

public abstract partial class ServerDbBase
{
    public async Task<List<ActivatedPendingBan>> ActivatePendingBansAsync(
        string serverId, DateTime now, PendingBanTrigger? roundTrigger, bool force)
    {
        await using var db = await GetDb();
        return await PendingBanActivation.ActivateAsync(db.DbContext, serverId, now, roundTrigger, force);
    }
}
