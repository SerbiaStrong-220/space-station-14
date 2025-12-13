// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Experience;

[Prototype]
public sealed class ExperienceDefinitionPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public Dictionary<ProtoId<SkillTreePrototype>, SkillTreeExperienceInfo> Skills = new();

    [DataField]
    public HashSet<ProtoId<KnowledgePrototype>> Knowledges = new();

    [DataField]
    public int AddSublevelPoints;
}
