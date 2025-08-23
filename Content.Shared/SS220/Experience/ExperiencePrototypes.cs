// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Experience;

[Prototype]
public sealed class SkillPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public SkillLevelInfo LevelInfo;

    [DataField(required: true)]
    public SkillLevelDescription LevelDescription;
}

[DataDefinition]
public partial struct SkillLevelInfo
{
    [DataField]
    public int NumberOfSublevels;

    [DataField]
    public LocId LevelUpPopup = "experience-skill-level-up-base-popup";

    [DataField]
    public LocId? SublevelUpPopup = null;
}

[DataDefinition]
public partial struct SkillLevelDescription
{
    [DataField(required: true)]
    public LocId SkillName;

    [DataField(required: true)]
    public LocId SkillDescription;

    [DataField]
    public LocId? SkillHoverDescription = null;
}

[Prototype]
public sealed class SkillTreePrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public List<ProtoId<SkillPrototype>> SkillTree = new();
}

[Prototype]
public sealed class KnowledgePrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;
}
