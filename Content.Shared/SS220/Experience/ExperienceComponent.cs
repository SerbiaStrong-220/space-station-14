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
//         but if somehow it is passed, so you can progress
// 2. When we hit StudyingProgress equal maximum (1) we progress in one sublevel (0,0,1) -> (0,1,0)
// 3. Hitting StudyingProgress maximum and sublevel maximum (s_m) we progress one level in tree
//                                                      which also calls Skill to be acquired (0, s_m, 1) -> (1, 0, 0)
//      * yeah again we have field named CanEndStudying which can block that progress
// 4. Repeat to have fun!
//
// In addition to have some image in mind lets use:
// [xxxx][xx|o][ooo][oo]
//  its describe SkillTreeExperienceInfo = (1, 2)
//  tree have 4 skills in it with maxSublevels 4 3 3 and 2.
//  | shows StudyingProgress
// it is possible to have images:
//  [xx|][oo] <= this means that 1 skill cant be studied  SkillTreeExperienceInfo is (0, 2)
//  [xx]| <= this will happen if we ended tree SkillTreeExperienceInfo is (1, null)
//  [xx][|00] <= this !can! mean that we earned 1 skill but 2 banned from studying SkillTreeExperienceInfo is (1, 0)

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

    /// <summary>
    /// Defines if current SkillLevel is studied
    /// help differ 2 situation [xx]|[oo] <--> [xx][|oo]
    /// </summary>
    public bool SkillStudied;

    public override readonly string ToString()
    {
        return $"level is {SkillLevel}, sublevel is {SkillSublevel}, skill studied is {SkillStudied}";
    }
}
