// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Examine;
using Content.Shared.SS220.Experience;
using Content.Shared.SS220.Experience.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.ChameleonStamp;

public sealed partial class ChameleonForgedStampSystem : EntitySystem
{
    [Dependency] private ExperienceSystem _experience = default!;

    private static readonly ProtoId<SkillPrototype> BureaucratSkill = "BureaucracyProfessional";
    private static readonly ProtoId<KnowledgePrototype> SyndicateAgentKnowledge = "SyndicateAgentKnowledge";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChameleonForgedStampComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(Entity<ChameleonForgedStampComponent> ent, ref ExaminedEvent args)
    {
        var examiner = args.Examiner;

        if (HasComp<BypassKnowledgeCheckComponent>(examiner))
        {
            PushHint(ref args);
            return;
        }

        if (_experience.HaveKnowledge(examiner, SyndicateAgentKnowledge))
        {
            PushHint(ref args);
            return;
        }

        if (_experience.HaveSkill(examiner, BureaucratSkill))
            PushHint(ref args);
    }

    private void PushHint(ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("chameleon-stamp-forged-hint"), 1);
    }
}
