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

        Dictionary<string, float> fractionsWinPoints = new();
        var capturePoints = EntityQueryEnumerator<EventCapturePointComponent>();
        while (capturePoints.MoveNext(out _, out var capturePointComponent))
        {
            foreach (var (fraction, retentionTime) in capturePointComponent.PointRetentionTime)
            {
                var winPoints = (float)(retentionTime / capturePointComponent.RetentionTimeForWP) * capturePointComponent.WinPoints;
                if (!fractionsWinPoints.TryAdd(fraction, winPoints))
                    fractionsWinPoints[fraction] += winPoints;
            }
        }

        //Sort by value
        fractionsWinPoints = fractionsWinPoints.OrderBy(pair => pair.Value).ToDictionary(pair => pair.Key, pair => pair.Value);

        foreach (var (fraction, winPoints) in fractionsWinPoints)
        {
            args.AddLine(Loc.GetString("fract-war-round-end-fraction-points", ("fraction", Loc.GetString(fraction)), ("points", (int)winPoints)));
        }

        args.AddLine(Loc.GetString("fract-war-round-end-winner", ("fraction", Loc.GetString(fractionsWinPoints.First().Key))));
    }
}
