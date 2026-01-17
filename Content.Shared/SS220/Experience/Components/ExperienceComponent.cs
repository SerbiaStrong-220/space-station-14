// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.FixedPoint;
using Content.Shared.SS220.Experience.Systems;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.Experience;

// # What happens and when
// 1. We start studying skill -> we add starting info into Skills and sets StudyingProgress in 0
//      *setting StudyingProgress checks if next (for init next is actually a starting) can be start studying
//         but if somehow it is passed, so you can progress
// 2. When we hit StudyingProgress equal maximum (1) we progress in one sublevel (0,0,1) -> (0,1,0)
// 3. Hitting StudyingProgress maximum and sublevel maximum (s_m) we progress one level in tree
//                                                      which also calls Skill to be acquired (0, s_m, 1) -> (1, 0, 0)
//      * yeah again we have field named CanEndStudying which can block that progress
//      * and field CanStartStudying to prevent starting studying! (why do I even need them... NO FUN ALLOWED)
// 4. Repeat to have fun!

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState(true, true)]
[Access(typeof(ExperienceSystem))]
public sealed partial class ExperienceComponent : Component
{
    public const string ContainerId = "experience-entity-container";
    public const string OverrideContainerId = "override-experience-entity-container";

    [ViewVariables(VVAccess.ReadOnly)]
    public bool SkillEntityInitialized = false;

    /// <summary>
    /// Container which entity with skill components.
    /// </summary>
    [ViewVariables]
    public ContainerSlot ExperienceContainer = default!;

    /// <summary>
    /// Container which entity with skill components. This overrides base one.
    /// </summary>
    [ViewVariables]
    public ContainerSlot OverrideExperienceContainer = default!;

    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<ProtoId<SkillTreePrototype>, FixedPoint4> StudyingProgress = new();

    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<ProtoId<SkillTreePrototype>, SkillTreeInfo> Skills = new();

    /// <summary>
    /// This field saves information about earned by progressing sublevels to proper handle other adding <br/>
    /// Only one thing because it networked - prediction of other things
    /// </summary>
    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public Dictionary<ProtoId<SkillTreePrototype>, int> EarnedSkillSublevel = new();

    /// <summary>
    /// This is used to override <see cref="ExperienceComponent.Skills"/> in checks
    /// </summary>
    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<ProtoId<SkillTreePrototype>, SkillTreeInfo> OverrideSkills = new();

    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public HashSet<ProtoId<KnowledgePrototype>> ConstantKnowledge = new();

    /// <summary>
    /// Contains resolved knowledges, this is not actual knowledges
    /// </summary>
    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadOnly)]
    public HashSet<ProtoId<KnowledgePrototype>> ResolvedKnowledge = new();

    [AutoNetworkedField]
    [ViewVariables(VVAccess.ReadWrite)]
    public int FreeSublevelPoints = 0;
}

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class SkillTreeInfo
{
    // This property kinda save coder sanity
    public int SkillTreeIndex => _level - 1;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public int Level
    {
        get => _level;
        set
        {
            DebugTools.Assert(value >= ExperienceSystem.StartSkillLevel);
            _level = value;
        }
    }

    private int _level = ExperienceSystem.StartSkillLevel;

    [DataField]
    [ViewVariables(VVAccess.ReadOnly)]
    public int Sublevel
    {
        get => _sublevel;
        set
        {
            DebugTools.Assert(value >= ExperienceSystem.StartSublevel);
            _sublevel = value;
        }
    }

    private int _sublevel = ExperienceSystem.StartSublevel;

    public override string ToString()
    {
        return $"level: {Level}. Sublevel: {Sublevel}";
    }

    public void Add(SkillTreeInfo other)
    {
        Level += (other.Level - ExperienceSystem.StartSkillLevel);
        Sublevel += (other.Sublevel - ExperienceSystem.StartSublevel);
    }

    public SkillTreeInfo(SkillTreeInfo other)
    {
        Level = other.Level;
        Sublevel = other.Sublevel;
    }
}

/// <summary>
/// This struct used for more complex behavior then just add number to progress level <br/>
/// It can be used for raising / learning shape or falling \ depending on LearningDecreaseFactorPerLevel <br/>
/// Also keep in mind that
/// </summary>
[DataDefinition]
[Serializable, NetSerializable]
public partial struct LearningInformation
{
    [DataField(required: true)]
    public FixedPoint4 BaseLearning;

    [DataField]
    public int? PeakLearningLevel;

    [DataField]
    public FixedPoint4? LearningDecreaseFactorPerLevel;

    [DataField]
    public FixedPoint4 MinProgress = 0f;

    [DataField]
    public FixedPoint4 MaxProgress = 0.05f; // out of head base number, feel free to change if needed
}
