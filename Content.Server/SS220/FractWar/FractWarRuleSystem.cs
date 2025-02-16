using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.SS220.EventCapturePoint;
using Content.Shared.GameTicking.Components;
using System.Linq;

namespace Content.Server.SS220.FractWar;

public sealed partial class FractWarRuleSystem : GameRuleSystem<FractWarRuleComponent>
{
    [Dependency] private readonly EventCapturePointSystem _eventCapturePoint = default!;

    protected override void AppendRoundEndText(EntityUid uid, FractWarRuleComponent component, GameRuleComponent gameRule, ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(uid, component, gameRule, ref args);

        args.AddLine(Loc.GetString("fractwar-round-end-score-points"));
        args.AddLine("");

        _eventCapturePoint.RefreshWinPoints(component);
        var fractionsWinPoints = component.FractionsWinPoints;

        if (fractionsWinPoints.Count <= 0)
            return;

        var finalWinPoints = fractionsWinPoints.OrderByDescending(pair => pair.Value).ToDictionary(pair => pair.Key, pair => (int)pair.Value);

        List<string> winners = [];
        var largestWP = 0;
        foreach (var (fraction, winPoints) in finalWinPoints)
        {
            args.AddLine(Loc.GetString("fractwar-round-end-fraction-points", ("fraction", Loc.GetString(fraction)), ("points", winPoints)));

            if (winPoints > largestWP)
            {
                winners.Clear();
                winners.Add(fraction);
                largestWP = winPoints;
            }
            else if (winPoints == largestWP)
            {
                winners.Add(fraction);
            }
        }
        args.AddLine("");

        if (winners.Count > 1)
        {
            var winnersStr = "";
            var lastWinner = Loc.GetString(winners.Last());

            for (var i = 0; winners.Count > i + 1; i++)
            {
                var currentWinner = Loc.GetString(winners[i]);
                winnersStr += winners.Count != i + 2
                    ? currentWinner + ","
                    : currentWinner;
            }

            args.AddLine(Loc.GetString("fractwar-round-end-draw", ("fractions", winnersStr), ("lastFraction", lastWinner)));
        }
        else
        {
            args.AddLine(Loc.GetString("fractwar-round-end-winner", ("fraction", Loc.GetString(winners.First()))));
        }
    }

    public FractWarRuleComponent? GetActiveGameRule()
    {
        FractWarRuleComponent? comp = null;
        var query = QueryActiveRules();
        while (query.MoveNext(out _, out var fractComp, out _))
        {
            comp = fractComp;
            break;
        }

        return comp;
    }
}
