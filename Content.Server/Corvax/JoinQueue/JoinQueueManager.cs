using System.Linq;
using Content.Server.Connection;
using Content.Server.Corvax.DiscordAuth;
using Content.Server.SS220.Discord;
using Content.Shared.CCVar;
using Content.Shared.Corvax.CCCVars;
using Content.Shared.Corvax.JoinQueue;
using Content.Shared.Destructible;
using Prometheus;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.Corvax.JoinQueue;

/// <summary>
///     Manages new player connections when the server is full and queues them up, granting access when a slot becomes free
/// </summary>
public sealed class JoinQueueManager
{
    private static readonly Gauge QueueCount = Metrics.CreateGauge(
        "join_queue_count",
        "Amount of players in queue.");

    private static readonly Counter QueueBypassCount = Metrics.CreateCounter(
        "join_queue_bypass_count",
        "Amount of players who bypassed queue by privileges.");

    private static readonly Histogram QueueTimings = Metrics.CreateHistogram(
        "join_queue_timings",
        "Timings of players in queue",
        new HistogramConfiguration()
        {
            LabelNames = new[] {"type"},
            Buckets = Histogram.ExponentialBuckets(1, 2, 14),
        });

    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IConnectionManager _connectionManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IServerNetManager _netManager = default!;
    [Dependency] private readonly DiscordAuthManager _discordAuthManager = default!;
    [Dependency] private readonly DiscordPlayerManager _discordPlayerManager = default!;

    /// <summary>
    ///     Queue of active player sessions
    /// </summary>
    private readonly List<ICommonSession> _queue = new(); // Real Queue class can't delete disconnected users

    private bool _isEnabled = false;

    public int PlayerInQueueCount => _queue.Count;
    public int ActualPlayersCount => _playerManager.PlayerCount - PlayerInQueueCount; // Now it's only real value with actual players count that in game

    public void Initialize()
    {
        _netManager.RegisterNetMessage<MsgQueueUpdate>();

        _cfg.OnValueChanged(CCCVars.QueueEnabled, OnQueueCVarChanged, true);
        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
        _discordAuthManager.PlayerVerified += OnPlayerVerified;

        _discordPlayerManager.PlayerVerified += OnPlayerVerified;
    }

    private void OnQueueCVarChanged(bool value)
    {
        _isEnabled = value;

        if (!value)
        {
            foreach (var session in _queue)
            {
                session.Channel.Disconnect("Queue was disabled");
            }
        }
    }

    private async void OnPlayerVerified(object? sender, ICommonSession session)
    {
        if (!_isEnabled)
        {
            SendToGame(session);
            return;
        }

        var isPrivileged = await _connectionManager.HavePrivilegedJoin(session.UserId);
        var sponsorBypass = await _discordPlayerManager.HasPriorityJoinTierAsync(session.UserId) && _discordPlayerManager.HaveFreeSponsorSlot(); // SS220 Sponsor
        var currentOnline = _playerManager.PlayerCount - 1; // Do not count current session in general online, because we are still deciding her fate
        var haveFreeSlot = currentOnline < _cfg.GetCVar(CCVars.SoftMaxPlayers);
        if (isPrivileged || haveFreeSlot || sponsorBypass) // SS220 Sponsor
        {
            SendToGame(session);

            if (isPrivileged && !haveFreeSlot)
                QueueBypassCount.Inc();

            return;
        }

        _queue.Add(session);
        ProcessQueue(false, session.ConnectedTime);
    }

    private async void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (e.NewStatus == SessionStatus.Disconnected)
        {
            var wasInQueue = _queue.Remove(e.Session);

            if (!wasInQueue && e.OldStatus != SessionStatus.InGame) // Process queue only if player disconnected from InGame or from queue
                return;

            ProcessQueue(true, e.Session.ConnectedTime);

            if (wasInQueue)
                QueueTimings.WithLabels("Unwaited").Observe((DateTime.UtcNow - e.Session.ConnectedTime).TotalSeconds);
        }
    }

    /// <summary>
    ///     If possible, takes the first player in the queue and sends him into the game
    /// </summary>
    /// <param name="isDisconnect">Is method called on disconnect event</param>
    /// <param name="connectedTime">Session connected time for histogram metrics</param>
    private void ProcessQueue(bool isDisconnect, DateTime connectedTime)
    {
        var players = ActualPlayersCount;
        if (isDisconnect)
            players--; // Decrease currently disconnected session but that has not yet been deleted

        // SS220 sponsors begin
        //var haveFreeSlot = players < _cfg.GetCVar(CCVars.SoftMaxPlayers);
        //var haveFreeSponsorSlot = _connectionManager.HaveFreeSponsorSlot(); // SS220 sponsors
        //var queueContains = _queue.Count > 0;
        //if (haveFreeSlot && queueContains)
        //{
        //    var session = _queue.First();
        //    _queue.Remove(session);

        //    SendToGame(session);

        //    QueueTimings.WithLabels("Waited").Observe((DateTime.UtcNow - connectedTime).TotalSeconds);
        //}

        var queueContains = _queue.Count > 0;
        if (queueContains)
        {
            SortQueueBySponsors();
            var session = _queue.First();

            var canDequeue = CanDequeue(players, session);
            if (canDequeue)
            {
                _queue.Remove(session);

                SendToGame(session);

                QueueTimings.WithLabels("Waited").Observe((DateTime.UtcNow - connectedTime).TotalSeconds);
            }
        }
        // SS220 sponsors end

        SendUpdateMessages();
        QueueCount.Set(_queue.Count);
    }

    /// <summary>
    ///     Sends messages to all players in the queue with the current state of the queue
    /// </summary>
    private void SendUpdateMessages()
    {
        for (var i = 0; i < _queue.Count; i++)
        {
            _queue[i].Channel.SendMessage(new MsgQueueUpdate
            {
                Total = _queue.Count,
                Position = i + 1,
            });
        }
    }

    /// <summary>
    ///     Letting player's session into game, change player state
    /// </summary>
    /// <param name="s">Player session that will be sent to game</param>
    private void SendToGame(ICommonSession s)
    {
        Timer.Spawn(0, () => _playerManager.JoinGame(s));
    }

    // SS220 sponsors begin
    private void SortQueueBySponsors()
    {
        _queue.Sort((ICommonSession s1, ICommonSession s2) =>
        {
            var s1HasPriorityJoin = _discordPlayerManager.HasPriorityJoinTier(s1.UserId);
            var s2HasPriorityJoin = _discordPlayerManager.HasPriorityJoinTier(s2.UserId);
            if (s1HasPriorityJoin && !s2HasPriorityJoin) return -1;
            else if (!s1HasPriorityJoin && s2HasPriorityJoin) return 1;
            else return 0;
        });
    }

    private bool CanDequeue(int actualPlayers, ICommonSession session)
    {
        var sponsorBypass = _discordPlayerManager.HasPriorityJoinTier(session.UserId) && _discordPlayerManager.HaveFreeSponsorSlot();
        var haveFreeSlot = actualPlayers < _cfg.GetCVar(CCVars.SoftMaxPlayers);

        return haveFreeSlot || sponsorBypass;
    }
    // SS220 sponsors end
}
