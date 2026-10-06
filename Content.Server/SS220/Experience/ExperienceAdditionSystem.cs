// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.CultYogg.Cultists;
using Content.Shared.SS220.Experience;
using Content.Shared.SS220.Experience.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Experience;

public sealed partial class ExperienceAdditionSystem : EntitySystem
{
    [Dependency] private ExperienceSystem _experience = default!;

    private static readonly ProtoId<KnowledgePrototype> CultYoggKnowledge = "CultYoggKnowledge";

    public override void Initialize()
    {
        SubscribeLocalEvent<ExperienceComponent, GotCultifiedEvent>(OnCultified);
        SubscribeLocalEvent<ExperienceComponent, LiberationFromCultEvent>(OnLiberationFromCult);
    }

    // !! DO NOT DO THE SAME !!
    // It requires own system with dict like <obj, int> to resolve different sources of points
    // just don't have time to implement & test it =(
    // so you can actually make `god-add-experience` comp with own system to kill 10+ components and also do somewhat with role add knowledges or points
    // zero idea should it be added with mindrole feels kinda other thing for this system
    private void OnCultified(Entity<ExperienceComponent> entity, ref GotCultifiedEvent args)
    {
        _experience.TryAddKnowledge(entity!, CultYoggKnowledge);
        var addPointsComp = EnsureComp<AntagFreeSublevelPointsAddComponent>(entity);

        if (addPointsComp.AddFreeSublevelPoints < 5)
            addPointsComp.AddFreeSublevelPoints = 5;
    }

    private void OnLiberationFromCult(Entity<ExperienceComponent> entity, ref LiberationFromCultEvent args)
    {
        _experience.TryRemoveKnowledge(entity!, CultYoggKnowledge);
        var addPointsComp = EnsureComp<AntagFreeSublevelPointsAddComponent>(entity);

        if (addPointsComp.AddFreeSublevelPoints == 5)
            RemComp<AntagFreeSublevelPointsAddComponent>(entity);
    }
}

