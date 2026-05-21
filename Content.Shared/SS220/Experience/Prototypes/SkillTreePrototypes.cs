// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Experience;

[Prototype]
public sealed partial class SkillTreePrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public List<ProtoId<SkillPrototype>> SkillTree { get; private set; } = new();

    [DataField(required: true)]
    public LocId SkillTreeName { get; private set; }

    [DataField(required: true)]
    public ProtoId<SkillTreeGroupPrototype> SkillGroupId { get; private set; }

    [DataField]
    public bool CanBeShownOnInit = true;

    [DataField]
    public bool StudyingProgressPossible = true;
}

/// <summary>
/// Just QoDevEx thing
/// </summary>
[Prototype]
public sealed partial class SkillTreeGroupPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId GroupName { get; private set; }
}
