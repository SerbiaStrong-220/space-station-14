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
    [AutoNetworkedField]
    public ProtoId<ExperienceDefinitionPrototype>? SkillAddId;

    [DataField]
    [AutoNetworkedField]
    public Dictionary<ProtoId<SkillTreePrototype>, SkillTreeExperienceInfo> Skills = new();

    [DataField]
    [AutoNetworkedField]
    public HashSet<ProtoId<KnowledgePrototype>> Knowledges = new();

    [DataField]
    [AutoNetworkedField]
    public int AddSublevelPoints;
}

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RoleExperienceAddComponent : BaseExperienceAddComponent { }

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AdminForcedExperienceAddComponent : BaseExperienceAddComponent { }
