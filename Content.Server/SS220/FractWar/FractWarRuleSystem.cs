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

        _eventCapturePoint.RefreshWP(component);
        var fractionsWinPoints = component.FractionsWP;

        if (fractionsWinPoints.Count <= 0)
            return;

        fractionsWinPoints = fractionsWinPoints.OrderByDescending(pair => pair.Value).ToDictionary(pair => pair.Key, pair => pair.Value);

        args.AddLine("");
        foreach (var (fraction, winPoints) in fractionsWinPoints)
        {
            args.AddLine(Loc.GetString("fractwar-round-end-fraction-points", ("fraction", Loc.GetString(fraction)), ("points", (int)winPoints)));
        }

        //Sort by value
        args.AddLine("");
        args.AddLine(Loc.GetString("fractwar-round-end-winner", ("fraction", Loc.GetString(fractionsWinPoints.First().Key))));
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
