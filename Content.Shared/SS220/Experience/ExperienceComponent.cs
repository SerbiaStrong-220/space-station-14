// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.FixedPoint;
using Content.Shared.SS220.Experience.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Experience;

// # What happens and when
// 1. We start studying skill -> we add starting info into Skills and sets StudyingProgress in 0
//      *setting StudyingProgress checks if next (for init next is actually a starting) can be start studying
//       effectively by not setting StudyingProgress we ban skill from studying
// 2. When we hit StudyingProgress equal maximum (1) we progress in one sublevel (0,0,1) -> (0,1,0)
// 3. Hitting StudyingProgress maximum and sublevel maximum (s_m) we progress one level in tree
//                                                      which also calls Skill to be acquired (0, s_m, 1) -> (1, 0, 0)
//      * yeah again we have field named CanEndStudying which can block that progress
// 4. Repeat to have fun!

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState(true, fieldDeltas: true)]
[Access(typeof(ExperienceSystem))]
public sealed partial class ExperienceComponent : Component
{
    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public SortedDictionary<ProtoId<SkillTreePrototype>, SkillTreeExperienceInfo> Skills = new();

    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public SortedDictionary<ProtoId<SkillTreePrototype>, FixedPoint4> StudyingProgress = new();

    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public HashSet<ProtoId<KnowledgePrototype>> Knowledge = new();

    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public HashSet<ProtoId<KnowledgePrototype>> ConstantKnowledge = new();
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
    public int SkillSublevel;
}
