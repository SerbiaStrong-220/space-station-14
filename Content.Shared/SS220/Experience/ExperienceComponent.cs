// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Experience.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Experience;

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ExperienceSystem))]
public sealed partial class ExperienceComponent : Component
{
    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public SortedDictionary<ProtoId<SkillTreePrototype>, SkillTreeExperienceInfo> Skills = new();

    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public List<ProtoId<KnowledgePrototype>> Knowledge = new();

    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public SortedDictionary<ProtoId<SkillTreePrototype>, float> LearningProgress = new();
}

[NetSerializable]
public struct SkillTreeExperienceInfo
{
    /// <summary>
    /// Defines current skill level
    /// </summary>
    public int SkillLevel;

    /// <summary>
    /// Defines sublevel level
    /// </summary>
    public int ExperienceLevel;
}
