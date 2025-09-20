// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.FixedPoint;
using Content.Shared.SS220.Experience.Systems;
using Robust.Shared.Containers;
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
//  [xx|][oo] <= this means that 1 skill cant be studied  SkillTreeExperienceInfo is (0, 2, false)
//  [xx]| <= this will happen if we ended tree SkillTreeExperienceInfo is (0, 0, true)
//  [xx][|00] <= this !can! mean that we earned 1 skill but 2 banned from studying SkillTreeExperienceInfo is (1, 0, false)

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState(true, true)]
[Access(typeof(ExperienceSystem))]
public sealed partial class ExperienceComponent : Component
{
    public static readonly string ContainerId = "experience-entity-container";

    /// <summary>
    /// Container which entity with skill components.
    /// </summary>
    [ViewVariables] public ContainerSlot ExperienceContainer = default!;

    public static readonly string OverrideContainerId = "override-experience-entity-container";

    /// <summary>
    /// Container which entity with skill components. This overrides base one.
    /// </summary>
    [ViewVariables] public ContainerSlot OverrideExperienceContainer = default!;

    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<ProtoId<SkillTreePrototype>, FixedPoint4> StudyingProgress = new();

    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<ProtoId<SkillTreePrototype>, SkillTreeExperienceInfo> Skills = new();

    /// <summary>
    /// This is used to override <see cref="ExperienceComponent.Skills"/> in checks
    /// </summary>
    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<ProtoId<SkillTreePrototype>, SkillTreeExperienceInfo> OverrideSkills = new();

    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public HashSet<ProtoId<KnowledgePrototype>> Knowledge = new();

    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public HashSet<ProtoId<KnowledgePrototype>> ConstantKnowledge = new();

    /// <summary>
    /// This mask handles reiniting of experience to correctly process on spawn inits
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public InitGainedExperienceType InitMask = InitGainedExperienceType.NotInitialized;
}

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class SkillTreeExperienceInfo
{
    /// <summary>
    /// Defines current skill level
    /// </summary>
    [DataField]
    public int SkillLevel;

    /// <summary>
    /// Defines sublevel level
    /// </summary>
    [DataField]
    public int SkillSublevel;

    /// <summary>
    /// Defines if current SkillLevel is studied
    /// help differ 2 situation [xx]|[oo] <--> [xx][|oo]
    /// </summary>
    public bool SkillStudied = false;

    public override string ToString()
    {
        return $"level: {SkillLevel}. Sublevel: {SkillSublevel}. Studied: {SkillStudied}";
    }
}
