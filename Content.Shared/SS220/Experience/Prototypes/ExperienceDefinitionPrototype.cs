// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Experience;

[Prototype]
public sealed partial class ExperienceDefinitionPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    [AlwaysPushInheritance]
    public Dictionary<ProtoId<SkillTreePrototype>, SkillTreeInfo> Skills { get; private set; } = new();

    [DataField]
    [AlwaysPushInheritance]
    public HashSet<ProtoId<KnowledgePrototype>> Knowledges { get; private set; } = new();

    [DataField]
    [AlwaysPushInheritance]
    public int AddSublevelPoints { get; private set; }
}
