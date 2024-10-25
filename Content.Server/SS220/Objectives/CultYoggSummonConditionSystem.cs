// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Mind;
using Content.Shared.Roles.Jobs;
using Content.Shared.Objectives.Components;
using Content.Server.SS220.GameTicking.Rules;
using Content.Shared.SS220.CultYogg.Sacraficials;

namespace Content.Server.SS220.Objectives;

/// <summary>
/// Handle amount of sacrafices
/// </summary>
public sealed class CultYoggSummonConditionSystem : EntitySystem
{
    [Dependency] private readonly CultYoggRuleSystem _cultRule = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedJobSystem _job = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultYoggSummonConditionComponent, ComponentInit>(OnInit);

        SubscribeLocalEvent<CultYoggSummonConditionComponent, ObjectiveAfterAssignEvent>(OnAfterAssign);

        SubscribeLocalEvent<CultYoggSummonConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }
    //check if gamerule was rewritten
    private void OnInit(Entity<CultYoggSummonConditionComponent> ent, ref ComponentInit args)
    {
        _cultRule.GetCultGameRule(out var ruleComp);

        if (ruleComp is null)
            return;

        ent.Comp.reqSacrAmount = ruleComp.ReqAmountOfSacrifices;
    }

    private void OnAfterAssign(Entity<CultYoggSummonConditionComponent> ent, ref ObjectiveAfterAssignEvent args) //ToDo error with progress
    {
        string title = Loc.GetString("objective-cult-yogg-sacrafice-start");

        var query = EntityQueryEnumerator<CultYoggSacrificialMindComponent>();
        while (query.MoveNext(out var uid, out var _))
        {
            var targetName = "Unknown";
            if (TryComp<MindComponent>(uid, out var mind) && mind.CharacterName != null)
            {
                targetName = mind.CharacterName;
            }

            var jobName = _job.MindTryGetJobName(uid);

            title += "\n" + Loc.GetString("objective-condition-cult-yogg-sacrafice-person", ("targetName", targetName), ("job", jobName));//ToDo собирать строку через string builder
        }

        _metaData.SetEntityName(ent, title, args.Meta);
    }
    private void OnGetProgress(Entity<CultYoggSummonConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        _cultRule.GetCultGameRule(out var ruleComp);

        if (ruleComp is null)
            return;

        args.Progress = ruleComp.AmountOfSacrifices / ent.Comp.reqSacrAmount;
    }
}
