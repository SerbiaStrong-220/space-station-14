// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Experience;

[Prototype]
public sealed class SkillTreePrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public List<ProtoId<SkillPrototype>> SkillTree = new();

    [DataField(required: true)]
    public LocId SkillTreeName;

    [DataField(required: true)]
    public ProtoId<SkillTreeGroupPrototype> SkillGroupId;

    [DataField]
    public bool CanBeShownOnInit = true;
}

/// <summary>
/// Just QoDevEx thing
/// </summary>
[Prototype]
public sealed class SkillTreeGroupPrototype : IPrototype
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId GroupName;
}
