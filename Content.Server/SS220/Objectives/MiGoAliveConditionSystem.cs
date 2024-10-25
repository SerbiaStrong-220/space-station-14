// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Objectives.Components;
using Content.Shared.Objectives.Components;
using Content.Shared.SS220.CultYogg.MiGo;
using Content.Server.SS220.GameTicking.Rules;

namespace Content.Server.Objectives.Systems;

/// <summary>
/// Handles acsending to MiGo task
/// </summary>
public sealed class MiGoAliveConditionSystem : EntitySystem
{
    [Dependency] private readonly CultYoggRuleSystem _cultRule = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MiGoAliveConditionComponent, ComponentInit>(OnInit);

        SubscribeLocalEvent<MiGoAliveConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    //check if gamerule was rewritten
    private void OnInit(Entity<MiGoAliveConditionComponent> ent, ref ComponentInit args)
    {
        _cultRule.GetCultGameRule(out var ruleComp);

        if (ruleComp is null)
            return;

        ent.Comp.reqMiGoAmount = ruleComp.ReqAmountOfMiGo;
    }

    private void OnGetProgress(Entity<MiGoAliveConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        float migoCount = 0;//float cause args.Progress is float
        var query = EntityQueryEnumerator<MiGoComponent>();
        while (query.MoveNext(out var uid, out var migo))
        {
            if (migo.MayBeReplaced) //theoratically includes dead and no mind state
                continue;

            migoCount++;
        }

        args.Progress = migoCount / ent.Comp.reqMiGoAmount;
    }
}
