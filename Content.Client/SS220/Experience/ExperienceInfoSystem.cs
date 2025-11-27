// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.FixedPoint;
using Content.Shared.SS220.Experience;
using Content.Shared.SS220.Experience.Systems;
using Robust.Client.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.Experience;

public sealed class ExperienceInfoSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly ExperienceSystem _experience = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public Action<ExperienceData>? OnExperienceUpdated;

    private HashSet<ProtoId<KnowledgePrototype>> _knowledges = new();

    public void RequestLocalPlayerExperienceData()
    {
        var entity = _playerManager.LocalEntity;

        if (entity is null)
            return;

        var data = new ExperienceData();

        data.SkillDictionary = GetPlayerSkillData(entity.Value);

        if (_experience.TryGetEntityKnowledge(entity.Value, ref _knowledges))
            data.Knowledges = [.. _knowledges];

        OnExperienceUpdated?.Invoke(data);
    }

    public Dictionary<ProtoId<SkillTreeGroupPrototype>, List<(ProtoId<SkillTreePrototype>, SkillTreeExperienceContainer, FixedPoint4)>>?
        GetPlayerSkillData(EntityUid? uid)
    {
        if (!TryComp<ExperienceComponent>(uid, out var experienceComponent))
            return null;

        var progressDict = experienceComponent.StudyingProgress;
        var skillDict = experienceComponent.Skills;

        var overrideSkills = experienceComponent.OverrideSkills;

        Dictionary<ProtoId<SkillTreeGroupPrototype>,
                    List<(ProtoId<SkillTreePrototype>,
                    SkillTreeExperienceContainer,
                    FixedPoint4)>> result = new();

        foreach (var (key, skillInfo) in skillDict)
        {
            if (!_prototype.Resolve(key, out var keyProto))
            {
                Log.Error($"Cant resolve skill prototype with id {key} on entity {ToPrettyString(uid)}");
                continue;
            }

            var progress = progressDict.TryGetValue(key, out var experienceProgress) ? experienceProgress : 0f;

            var overrideSkillInfo = overrideSkills.TryGetValue(key, out var overrideSkillInfoTemp) ? overrideSkillInfoTemp : null;

            var skillContainer = new SkillTreeExperienceContainer(skillInfo, overrideSkillInfo);

            if (result.TryGetValue(keyProto.SkillGroupId, out var value))
                value.Add((key, skillContainer, progress));
            else
                result.Add(keyProto.SkillGroupId,
                    new List<(ProtoId<SkillTreePrototype>, SkillTreeExperienceContainer, FixedPoint4)>([(key, skillContainer, progress)]));
        }

        return result;
    }
}

public sealed class SkillTreeExperienceContainer(SkillTreeExperienceInfo info, SkillTreeExperienceInfo? overrideInfo = null)
{
    public SkillTreeExperienceInfo Info = info;
    public SkillTreeExperienceInfo? OverrideInfo = overrideInfo;
}


public sealed class ExperienceData
{
    public Dictionary<ProtoId<SkillTreeGroupPrototype>,
                    List<(ProtoId<SkillTreePrototype>,
                    SkillTreeExperienceContainer,
                    FixedPoint4)>>? SkillDictionary = null;

    public HashSet<ProtoId<KnowledgePrototype>>? Knowledges = null;
}
