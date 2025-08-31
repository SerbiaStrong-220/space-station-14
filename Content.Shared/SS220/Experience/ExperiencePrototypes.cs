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

    [DataField]
    [AlwaysPushInheritance]
    public ComponentRegistry Components { get; } = [];

    /// <summary>
    /// TODO
    /// </summary>
    [DataField]
    public bool OverrideNotNullValues = false;

    /// <summary>
    /// TODO
    /// </summary>
    [DataField]
    public bool OverrideNullValues = true;

    /// <summary>
    /// TODO
    /// </summary>
    [DataField]
    public bool ApplyIfAlreadyHave = true;
}

[DataDefinition]
public partial struct SkillLevelInfo
{
    /// <summary>
    /// Sublevel starts from 0 and progress until reaching this value
    /// </summary>
    [DataField]
    public int MaximumSublevel;

    [DataField]
    public LocId LevelUpPopup = "experience-skill-level-up-base-popup";

    [DataField]
    public LocId? SublevelUpPopup = null;

    /// <summary>
    /// Defines if this skill can be started studying
    /// </summary>
    [DataField]
    public bool CanStartStudying = true;

    /// <summary>
    /// Defines if this skill
    /// </summary>
    [DataField]
    public bool CanEndStudying = true;
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
