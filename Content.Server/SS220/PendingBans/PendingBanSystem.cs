using System.Threading.Tasks;
using Content.Server.Administration.Managers;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Shared.CCVar;
using Content.Shared.Database.SS220.PendingBans;
using Robust.Shared.Asynchronous;
using Robust.Shared.Configuration;

namespace Content.Server.SS220.PendingBans;

/// <summary>
/// Serializes scheduling and activation, including startup recovery and shutdown draining.
/// Frame updates deliberately run even when the game is paused or the server is empty.
/// </summary>
public sealed class PendingBanSystem : EntitySystem
{
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly IBanManager _bans = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly ITaskManager _tasks = default!;

    private Task _work = Task.CompletedTask;
    private string _serverId = string.Empty;
    private DateTime _nextCheck;
    private bool _stopping;
    private bool _ready;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnRunLevelChanged);
    }

    public void Recover()
    {
        // Keep ownership stable even if the CVar is changed during this process's lifetime.
        _serverId = _cfg.GetCVar(CCVars.ServerId);
        // Any pending bans left by a previous process must take effect before accepting players.
        var recovery = Queue(() => ActivateAsync(force: true));
        _tasks.BlockWaitOnTask(recovery);
        recovery.GetAwaiter().GetResult();
        _ready = true;
    }

    public Task ScheduleAsync(CreateBanInfo info, PendingBanTrigger trigger, uint delayMinutes, bool erase, bool postBanInfo)
    {
        if (!_ready || _stopping)
            throw new InvalidOperationException("Server is starting or stopping.");
        if (trigger == PendingBanTrigger.Immediate || !Enum.IsDefined(trigger) ||
            trigger == PendingBanTrigger.Delay && (delayMinutes == 0 || delayMinutes > 525600))
            throw new ArgumentException("Invalid pending ban schedule.");

        var data = new PendingBanData(
            _serverId,
            trigger == PendingBanTrigger.Delay ? DateTime.UtcNow.AddMinutes(delayMinutes) : null,
            trigger,
            erase,
            postBanInfo);
        return Queue(() => _bans.ScheduleBanAsync(info, data));
    }

    public void UpdatePendingBans()
    {
        if (!_ready || _stopping || !_work.IsCompleted || DateTime.UtcNow < _nextCheck)
            return;

        _nextCheck = DateTime.UtcNow.AddSeconds(1);
        _ = Queue(() => ActivateSafelyAsync());
    }

    private void OnRunLevelChanged(GameRunLevelChangedEvent ev)
    {
        if (!_ready || _stopping)
            return;

        var trigger = ev.New switch
        {
            GameRunLevel.PreRoundLobby => PendingBanTrigger.Lobby,
            GameRunLevel.InRound => PendingBanTrigger.RoundStart,
            GameRunLevel.PostRound => PendingBanTrigger.RoundEnd,
            _ => throw new ArgumentOutOfRangeException(),
        };
        _ = Queue(() => ActivateSafelyAsync(trigger));
    }

    public void FlushBeforeShutdown()
    {
        if (!_ready)
            return;

        _stopping = true;
        var flush = Queue(() => ActivateAsync(force: true));
        _tasks.BlockWaitOnTask(flush);
        // Do not silently report success if the database is unavailable.
        flush.GetAwaiter().GetResult();
    }

    private Task Queue(Func<Task> action)
    {
        return _work = RunAfterAsync(_work, action);
    }

    private static async Task RunAfterAsync(Task previous, Func<Task> action)
    {
        try
        {
            await previous;
        }
        catch
        {
            // A failed request must not prevent subsequent requests or shutdown recovery.
        }
        await action();
    }

    private async Task ActivateSafelyAsync(PendingBanTrigger? trigger = null)
    {
        try
        {
            await ActivateAsync(trigger);
        }
        catch (Exception e)
        {
            Log.Error($"Failed to activate pending bans; the database queue is retained: {e}");
        }
    }

    private async Task ActivateAsync(PendingBanTrigger? trigger = null, bool force = false)
    {
        var activated = await _db.ActivatePendingBansAsync(
            _serverId, DateTime.UtcNow, trigger, force);
        foreach (var ban in activated)
        {
            try
            {
                await _bans.ApplyPendingBanAsync(ban);
            }
            catch (Exception e)
            {
                Log.Error($"Pending ban #{ban.Id} is active in the database, but applying live effects failed: {e}");
            }
        }
    }
}
