// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Experience.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.Experience;

[Prototype]
public sealed class SkillPrototype : IPrototype, ISerializationHooks
{
    [ViewVariables]
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public SkillLevelInfo LevelInfo;

    [DataField(required: true)]
    public SkillLevelDescription LevelDescription;

    [DataField]
    [AlwaysPushInheritance]
    public ComponentRegistry Components { get; } = [];

    [DataField]
    [AlwaysPushInheritance]
    public ComponentRegistry RemoveComponents { get; } = [];

    /// <summary>
    /// Deletes and then adds component if component with same type existed
    /// </summary>
    [DataField]
    public bool ApplyIfAlreadyHave = true;

    void ISerializationHooks.AfterDeserialization()
    {
        if (LevelInfo.MaximumSublevel < ExperienceSystem.StartSublevel)
            Logger.GetSawmill("skillPrototype").Error($"{nameof(LevelInfo.MaximumSublevel)} cant be less than {ExperienceSystem.StartSublevel}! Error in {nameof(SkillPrototype)} with id {ID}");

    }
}

[DataDefinition]
public partial struct SkillLevelInfo : ISerializationHooks
{
    /// <summary>
    /// Sublevel starts from 1 and progress until reaching this value
    /// </summary>
    [DataField]
    public int MaximumSublevel;

    [DataField(required: true)]
    public LocId LevelUpPopup;

    [DataField(required: true)]
    public LocId SublevelUpPopup;

    /// <summary>
    /// Defines if this skill can be started studying
    /// </summary>
    [DataField]
    public bool CanStartStudying = true;

    /// <summary>
    /// Defines if this skill
    /// </summary>
    [DataField]
    public bool CanEndStudying = true;

    void ISerializationHooks.AfterDeserialization()
    {
        if (MaximumSublevel < ExperienceSystem.StartSublevel)
            Logger.GetSawmill("skillLevelInfo").Error($"{nameof(MaximumSublevel)} cant be less than {ExperienceSystem.StartSublevel}!");

    }
}

[DataDefinition]
public partial struct SkillLevelDescription
{
    [DataField(required: true)]
    public LocId SkillName;

    [DataField(required: true)]
    public LocId SkillDescription;

    [DataField]
    public LocId? SkillHoverOverrideDescription = null;

    [DataField]
    public ResPath? SkillIconResPath = null;
}
