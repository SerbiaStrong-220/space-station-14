// © SS220, MIT, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.txt

using Content.Shared.GameTicking.Components;
using Content.Server.GameTicking.Rules;
using Content.Server.SS220.GameTicking.Rules.Components;
using Content.Server.Voting.Managers;
using Content.Server.Voting;
using Content.Server.Administration.Logs;
using Content.Shared.Database;
using Content.Server.RoundEnd;
using Robust.Shared.Timing;

namespace Content.Server.SS220.GameTicking.Rules;

public sealed class EmergencyShuttleAutoVoteRuleSystem : GameRuleSystem<EmergencyShuttleAutoVoteRuleComponent>
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly RoundEndSystem _roundEnd = default!;
    [Dependency] private readonly IVoteManager _voteManager = default!;

    protected override void Started(EntityUid uid, EmergencyShuttleAutoVoteRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        MakeEmergencyShuttleVote(component);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var autoRecall in EntityQuery<EmergencyShuttleAutoVoteRuleComponent>())
        {
            if (_gameTiming.CurTime > autoRecall.ForceEvacTime)
                MakeEmergencyShuttleVote(autoRecall);

            if (_gameTiming.CurTime < autoRecall.VoteStartTime)
                return;

            if (_gameTiming.CurTime < autoRecall.LastEvacVoteTime + autoRecall.IntervalBetweenVotes)
                return;

            MakeEmergencyShuttleVote(autoRecall);
        }
    }

    private void MakeEmergencyShuttleVote(EmergencyShuttleAutoVoteRuleComponent component)
    {
        component.LastEvacVoteTime = _gameTiming.CurTime;

        var voteOptions = new VoteOptions()
        {
            Title = Loc.GetString("ui-vote-restart-title"),
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
                callEvac = true;
            else
                callEvac = (bool)args.Winner;

            _adminLog.Add(LogType.Vote, LogImpact.Medium, $"Auto call emergency shuttle vote finished, result is {callEvac}");

            if (!callEvac)
                return;

            _roundEnd.RequestRoundEnd(null, false, "round-end-system-shuttle-auto-called-announcement");

            var ev = new EmergencyShuttleCalledByVote();
            RaiseLocalEvent(ref ev);
        };
    }
}

[ByRefEvent]
public record struct EmergencyShuttleCalledByVote(bool Called = true);
