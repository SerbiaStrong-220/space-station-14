// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Experience;

/// <summary>
/// This is used as base component to inherit for components which adds skills
/// </summary>
[DataDefinition]
public abstract partial class SkillBaseAddComponent : Component
{
    [DataField(required: true)]
    public ProtoId<SkillAddPrototype> SkillAddId;

    [DataField]
    public Dictionary<ProtoId<SkillTreePrototype>, SkillTreeExperienceInfo> Skills = new();

    [DataField]
    public HashSet<ProtoId<KnowledgePrototype>> Knowledges = new();
}

[RegisterComponent, NetworkedComponent]
public sealed partial class SkillRoleAddComponent : SkillBaseAddComponent { }

[RegisterComponent, NetworkedComponent]
public sealed partial class SkillBackgroundAddComponent : SkillBaseAddComponent { }

[RegisterComponent, NetworkedComponent]
public sealed partial class SkillForcedAddComponent : SkillBaseAddComponent { }
