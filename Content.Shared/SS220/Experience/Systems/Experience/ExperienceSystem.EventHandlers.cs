// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.CultYogg.Cultists;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Experience.Systems;

public sealed partial class ExperienceSystem : EntitySystem
{
    private readonly ProtoId<KnowledgePrototype> _cultYoggKnowledge = "CultYoggKnowledge";

    private void InitializeEventHandlers()
    {
        SubscribeLocalEvent<ExperienceComponent, GotCultifiedEvent>((Entity<ExperienceComponent> entity, ref GotCultifiedEvent args)
            => TryAddKnowledge(entity!, _cultYoggKnowledge));

        SubscribeLocalEvent<ExperienceComponent, LiberationFromCultEvent>((Entity<ExperienceComponent> entity, ref LiberationFromCultEvent args)
            => TryRemoveKnowledge(entity!, _cultYoggKnowledge));

    }
}
