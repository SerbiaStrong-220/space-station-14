// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.SS220.GameTicking.Rules;
using Content.Server.SS220.Objectives.Components;
using Content.Shared.Objectives.Components;
using System.Text;

namespace Content.Server.SS220.Objectives.Systems;

public sealed class CultYoggEnslaveConditionSystem : EntitySystem
{
    [Dependency] private readonly CultYoggRuleSystem _cultRule = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultYoggEnslaveConditionComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<CultYoggEnslaveConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnInit(Entity<CultYoggEnslaveConditionComponent> ent, ref ComponentInit args)
    {
        if (!_cultRule.TryGetCultGameRule(out var rule))
            return;

        foreach (var (_, stagedef) in rule.Value.Comp.Stages)//get highest stage number
        {
            if (stagedef.CultistsAmountRequired == null)
                continue;

            ent.Comp.ReqCultFactionAmount = stagedef.CultistsAmountRequired.Value;
        }

        var title = new StringBuilder();
        title.AppendLine(Loc.GetString("objective-condition-cult-yogg-enslave", ("amont", ent.Comp.ReqCultFactionAmount)));
        _metaData.SetEntityName(ent, title.ToString());
    }

    private void OnGetProgress(Entity<CultYoggEnslaveConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = 0;

        args.Progress = _cultRule.GetCultistsFraction() / (float)ent.Comp.ReqCultFactionAmount;
    }
}
