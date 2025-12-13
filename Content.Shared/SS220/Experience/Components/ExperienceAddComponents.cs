// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Experience;

/// <summary>
/// This is used as base component to inherit for components which adds skills
/// </summary>
public abstract partial class BaseExperienceAddComponent : Component
{
    [DataField]
    public ProtoId<ExperienceDefinitionPrototype>? SkillAddId;

    [DataField]
    public Dictionary<ProtoId<SkillTreePrototype>, SkillTreeExperienceInfo> Skills = new();

    [DataField]
    public HashSet<ProtoId<KnowledgePrototype>> Knowledges = new();

    [DataField]
    public int AddSublevelPoints;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class RoleExperienceAddComponent : BaseExperienceAddComponent { }

[RegisterComponent, NetworkedComponent]
public sealed partial class AdminForcedExperienceAddComponent : BaseExperienceAddComponent { }
