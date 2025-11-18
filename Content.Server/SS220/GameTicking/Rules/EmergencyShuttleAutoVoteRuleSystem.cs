// © SS220, MIT, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.txt

using Content.Server.Administration.Logs;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.RoundEnd;
using Content.Server.SS220.GameTicking.Rules.Components;
using Content.Server.Voting;
using Content.Server.Voting.Managers;
using Content.Shared.Database;
using Content.Shared.GameTicking.Components;
using Prometheus;
using Robust.Shared.Timing;

namespace Content.Server.SS220.GameTicking.Rules;

public sealed class EmergencyShuttleAutoVoteRuleSystem : GameRuleSystem<EmergencyShuttleAutoVoteRuleComponent>
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly IVoteManager _voteManager = default!;

    private TimeSpan RoundTime => _gameTiming.CurTime - _gameTicker.RoundStartTimeSpan;

    private static readonly Histogram EvacCallTime = Metrics.CreateHistogram(
        "vote_evac_call_time", "Round time when emergency shuttle was called by vote or force called by its round duration settings");

    private static readonly Histogram VoteTimeResult = Metrics.CreateHistogram(
        "vote_evac_result",
        "Gives information of vote result against round time in HOURS",
        new HistogramConfiguration()
        {
            LabelNames = new[] { "result" },
            Buckets = Histogram.LinearBuckets(1, 0.2, 10)
        }
        );

    protected override void ActiveTick(EntityUid uid, EmergencyShuttleAutoVoteRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        if (RoundTime > component.ForceEvacTime)
        {
            CallUnRecallableEmergencyShuttle();
            _gameTicker.EndGameRule(uid, gameRule);
        }

        if (RoundTime < component.VoteStartTime)
            return;

        if (RoundTime < component.LastEvacVoteTime + component.IntervalBetweenVotes)
            return;

        MakeEmergencyShuttleVote(component);
    }

    private void MakeEmergencyShuttleVote(EmergencyShuttleAutoVoteRuleComponent component)
    {
        component.LastEvacVoteTime = RoundTime;

        var voteOptions = new VoteOptions()
        {
            Title = Loc.GetString("ui-vote-auto-emergency-shuttle-title"),
            Options =
            {
                (Loc.GetString("ui-vote-auto-emergency-shuttle-yes"), true),
                (Loc.GetString("ui-vote-auto-emergency-shuttle-no"), false),
            }
        };

        voteOptions.SetInitiatorOrServer(null);

        var vote = _voteManager.CreateVote(voteOptions);

        vote.OnFinished += (_, args) =>
        {
            bool callEvac;
            if (args.Winner == null)
                callEvac = false;
            else
                callEvac = (bool)args.Winner;

            _adminLog.Add(LogType.Vote, LogImpact.Medium, $"Auto call emergency shuttle vote finished, result is {callEvac}");

            VoteTimeResult.WithLabels(callEvac.ToString()).Observe(RoundTime.TotalHours);

            if (!callEvac)
                return;

            CallUnRecallableEmergencyShuttle();
        };
    }

    private void CallUnRecallableEmergencyShuttle()
    {
        _roundEnd.RequestRoundEnd(null, false, "round-end-system-shuttle-auto-called-announcement");

        EvacCallTime.Observe(RoundTime.TotalHours);

        var ev = new EmergencyShuttleCalledByVote();
        RaiseLocalEvent(ref ev);
    }
}

[ByRefEvent]
public record struct EmergencyShuttleCalledByVote()
{
    public bool Block = true;
}
