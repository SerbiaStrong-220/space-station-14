// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Text;
using Content.Shared.Mind;
using Content.Shared.Roles.Jobs;
using Content.Shared.Objectives.Components;
using Content.Server.SS220.GameTicking.Rules;
using Content.Shared.SS220.CultYogg.Sacraficials;
using Content.Server.SS220.Objectives.Components;
using Robust.Shared.Serialization;

namespace Content.Server.SS220.Objectives.Systems;

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
        SubscribeLocalEvent<CultYoggSummonConditionComponent, CultYoggReinitObjEvent>(OnReInit);
        SubscribeLocalEvent<CultYoggSummonConditionComponent, CultYoggUpdateSacrObjEvent>(OnSacrUpdate);
    }
    //check if gamerule was rewritten
    private void OnInit(Entity<CultYoggSummonConditionComponent> ent, ref ComponentInit args)
    {
        TaskNumberUpdate(ent);
    }

    private void OnAfterAssign(Entity<CultYoggSummonConditionComponent> ent, ref ObjectiveAfterAssignEvent args)
    {
        SacraficialsUpdate(ent);
    }
    private void OnSacrUpdate(Entity<CultYoggSummonConditionComponent> ent, ref CultYoggUpdateSacrObjEvent args)
    {
        SacraficialsUpdate(ent);
    }
    private void OnReInit(Entity<CultYoggSummonConditionComponent> ent, ref CultYoggReinitObjEvent args)
    {
        SacraficialsUpdate(ent);
    }
    private void OnGetProgress(Entity<CultYoggSummonConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = 0;

        var ruleComp = _cultRule.GetCultGameRule();

        if (ruleComp is null)
            return;

        args.Progress = ruleComp.AmountOfSacrifices / ent.Comp.reqSacrAmount;
    }

    private void TaskNumberUpdate(Entity<CultYoggSummonConditionComponent> ent)
    {
        var ruleComp = _cultRule.GetCultGameRule();

        if (ruleComp is null)
            return;

        ent.Comp.reqSacrAmount = ruleComp.AmountOfSacrificesToGodSummon;
    }
    private void SacraficialsUpdate(Entity<CultYoggSummonConditionComponent> ent)
    {
        var title = new StringBuilder();
        title.AppendLine(Loc.GetString("objective-cult-yogg-sacrafice-start"));

        var query = EntityQueryEnumerator<CultYoggSacrificialMindComponent>();
        while (query.MoveNext(out var uid, out var _))
        {
            var targetName = "Unknown";
            if (TryComp<MindComponent>(uid, out var mind) && mind.CharacterName != null)
            {
                targetName = mind.CharacterName;
            }

            var jobName = _job.MindTryGetJobName(uid);

            title.AppendLine(Loc.GetString("objective-condition-cult-yogg-sacrafice-person", ("targetName", targetName), ("job", jobName)));
        }

        _metaData.SetEntityName(ent, title.ToString());
    }
}
[ByRefEvent, Serializable]
public sealed class CultYoggReinitObjEvent : EntityEventArgs
{
}

[ByRefEvent, Serializable]
public sealed class CultYoggUpdateSacrObjEvent : EntityEventArgs
{
}
