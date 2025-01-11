using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Shared.GameTicking.Components;
using Content.Shared.SS220.EventCapturePoint;
using System.Linq;

namespace Content.Server.SS220.FractWar;

public sealed partial class FractWarRuleSystem : GameRuleSystem<FractWarRuleComponent>
{
    protected override void AppendRoundEndText(EntityUid uid, FractWarRuleComponent component, GameRuleComponent gameRule, ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(uid, component, gameRule, ref args);

        args.AddLine(Loc.GetString("fractwar-round-end-score-points"));

        Dictionary<string, float> fractionsWinPoints = new();
        var capturePoints = EntityQueryEnumerator<EventCapturePointComponent>();
        while (capturePoints.MoveNext(out _, out var capturePointComponent))
        {
            foreach (var (fraction, retentionTime) in capturePointComponent.PointRetentionTime)
            {
                var winPoints = (float)(retentionTime.TotalSeconds / capturePointComponent.RetentionTimeForWP.TotalSeconds) * capturePointComponent.WinPoints;
                if (!fractionsWinPoints.TryAdd(fraction, winPoints))
                    fractionsWinPoints[fraction] += winPoints;
            }
        }

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
}
