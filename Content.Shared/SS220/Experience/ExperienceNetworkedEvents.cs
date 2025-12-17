// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Experience;

/// <summary>
/// Network event, send from client to server, prevent unnecessary using of redactor ui
/// </summary>
[Serializable, NetSerializable]
public sealed class OpenExperienceRedactorRequest(NetEntity? target = null) : EntityEventArgs
{
    public NetEntity? Target = target;
}

/// <summary>
/// Network event, send from client to server. After checking that sender is admin the changes to experience will be applied
/// </summary>
[Serializable, NetSerializable]
public sealed class ChangeEntityExperienceAdminRequest(NetEntity target, ExperienceData data) : EntityEventArgs
{
    public NetEntity Target = target;
    public ExperienceData Data = data;
}

/// <summary>
/// Network event, send from client to server. Represents how player spent free points. After validating free points and local entity. changes will be applied.
/// </summary>
[Serializable, NetSerializable]
public sealed class ChangeEntityExperiencePlayerRequest(PlayerChangeSkill changeSkill) : EntityEventArgs
{
    public PlayerChangeSkill ChangeSkill = changeSkill;
}

[Serializable, NetSerializable]
public readonly record struct SkillTreeView(ProtoId<SkillTreePrototype> SkillTreeId, SkillTreeInfo Info, SkillTreeInfo? OverrideInfo, FixedPoint4 Progress);


[Serializable, NetSerializable]
public sealed class ExperienceData
{
    public Dictionary<ProtoId<SkillTreeGroupPrototype>, List<SkillTreeView>> SkillDictionary = new();

    public HashSet<ProtoId<KnowledgePrototype>> Knowledges = new();
}

[Serializable, NetSerializable]
public sealed class PlayerChangeSkill
{
    public Dictionary<ProtoId<SkillTreePrototype>, int> SkillSublevels = new();
}
