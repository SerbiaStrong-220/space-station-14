// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Experience;

/// <summary>
/// Event raised on adding <see cref="SkillTreePrototype"/> to <see cref="ExperienceComponent.Skills"/>
/// </summary>
/// <param name="SkillTree"> Id of added <see cref="SkillTreePrototype"/> </param>
/// <param name="Info"> This struct contains additions to start level, all higher than max level will be correctly added </param>
[ByRefEvent]
public record struct SkillTreeAddedEvent(ProtoId<SkillTreePrototype> SkillTree, SkillTreeExperienceInfo Info)
{
    public ProtoId<SkillTreePrototype> SkillTree { get; init; } = SkillTree;
    public SkillTreeExperienceInfo Info = Info;
}

[ByRefEvent]
public record struct KnowledgeInitial(HashSet<ProtoId<KnowledgePrototype>> Knowledges);

/// <summary>
/// Event raised on gaining skill
/// </summary>
[ByRefEvent]
public record struct SkillLevelGainedEvent(ProtoId<SkillTreePrototype> SkillTree, ProtoId<SkillPrototype> GainedSkill);

[ByRefEvent]
public record struct KnowledgeGainedEvent(ProtoId<KnowledgePrototype> Knowledge);

[ByRefEvent]
public record struct KnowledgeLostEvent(ProtoId<KnowledgePrototype> Knowledge);

[ByRefEvent]
public record struct SkillEntityOverrideCheckEvent<T>(bool Subscribed = false) where T : notnull;
