// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.CultYogg.Cultists;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Experience;

// NOTE:
// Use it to remove experience related logic from other system
public sealed partial class ExperienceAdditionSystem : EntitySystem
{
    private static readonly ProtoId<KnowledgePrototype> CultYoggKnowledge = "CultYoggKnowledge";

    private void InitializeOtherSubscription()
    {
        SubscribeLocalEvent<ExperienceComponent, GotCultifiedEvent>(OnCultified);
        SubscribeLocalEvent<ExperienceComponent, LiberationFromCultEvent>(OnLiberationFromCult);
    }

    private void OnCultified(Entity<ExperienceComponent> entity, ref GotCultifiedEvent args)
    {
        UpsertSpecifier(entity, ExperienceAddSource.Antag, new ExperienceAddSpecifier(5, [CultYoggKnowledge], []));
    }

    private void OnLiberationFromCult(Entity<ExperienceComponent> entity, ref LiberationFromCultEvent args)
    {
        UpsertSpecifier(entity, ExperienceAddSource.Antag, null);
    }
}

