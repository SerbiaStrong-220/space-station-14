// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Experience;

[Serializable, NetSerializable]
public sealed class OpenExperienceRedactorRequest(NetEntity? target = null) : EntityEventArgs
{
    public NetEntity? Target = target;
}

[Serializable, NetSerializable]
public sealed class ChangeEntityExperienceRequest(NetEntity target, ExperienceData data) : EntityEventArgs
{
    public NetEntity Target = target;
    public ExperienceData Data = data;
}

[Serializable, NetSerializable]
public sealed class SkillTreeExperienceContainer(SkillTreeExperienceInfo info, SkillTreeExperienceInfo? overrideInfo = null)
{
    public SkillTreeExperienceInfo Info = info;
    public SkillTreeExperienceInfo? OverrideInfo = overrideInfo;
}

[Serializable, NetSerializable]
public sealed class ExperienceData
{
    public Dictionary<ProtoId<SkillTreeGroupPrototype>,
                    List<(ProtoId<SkillTreePrototype>,
                    SkillTreeExperienceContainer,
                    FixedPoint4)>> SkillDictionary = new();

    public HashSet<ProtoId<KnowledgePrototype>> Knowledges = new();
}
