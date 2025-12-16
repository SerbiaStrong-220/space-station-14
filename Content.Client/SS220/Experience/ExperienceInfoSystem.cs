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

    public Action<ExperienceData, int>? OnExperienceUpdated;

    /// <summary> Temporary collection for methods</summary>
    private HashSet<ProtoId<KnowledgePrototype>> _knowledges = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<ExperienceComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);
    }

    private void OnAfterAutoHandleState(Entity<ExperienceComponent> entity, ref AfterAutoHandleStateEvent args)
    {
        if (_playerManager.LocalEntity != entity)
            return;

        RequestLocalPlayerExperienceData();
    }

    public void SendChangeEntityExperiencePlayerRequest(PlayerChangeSkill changeSkill)
    {
        var ev = new ChangeEntityExperiencePlayerRequest(changeSkill);

        RaiseNetworkEvent(ev);
    }

    public void RequestLocalPlayerExperienceData()
    {
        OnExperienceUpdated?.Invoke(GetEntityExperienceData(_playerManager.LocalEntity), GetFreeSublevelPoints(_playerManager.LocalEntity));
    }

    public int GetFreeSublevelPoints(EntityUid? uid)
    {
        if (!TryComp<ExperienceComponent>(uid, out var experienceComponent))
            return 0;

        return experienceComponent.FreeSublevelPoints;
    }

    public ExperienceData GetEntityExperienceData(EntityUid? uid)
    {
        var data = new ExperienceData();

        if (uid is null)
            return data;

        data.SkillDictionary = GetEntitySkillData(uid.Value);

        if (_experience.TryGetEntityKnowledge(uid.Value, ref _knowledges))
            data.Knowledges = [.. _knowledges];

        return data;
    }

    public Dictionary<ProtoId<SkillTreeGroupPrototype>, List<SkillTreeView>> GetEntitySkillData(Entity<ExperienceComponent?> entity)
    {
        if (!Resolve(entity.Owner, ref entity.Comp, logMissing: false))
            return new();

        var progressDict = entity.Comp.StudyingProgress;
        var skillDict = entity.Comp.Skills;

        var overrideSkills = entity.Comp.OverrideSkills;

        Dictionary<ProtoId<SkillTreeGroupPrototype>, List<SkillTreeView>> result = new();

        foreach (var (key, skillInfo) in skillDict)
        {
            if (!_prototype.Resolve(key, out var keyProto))
            {
                Log.Error($"Cant resolve skill prototype with id {key} on entity {ToPrettyString(entity)}");
                continue;
            }

            var progress = progressDict.TryGetValue(key, out var experienceProgress) ? experienceProgress : 0f;
            overrideSkills.TryGetValue(key, out var overrideSkillInfo);

            if (result.TryGetValue(keyProto.SkillGroupId, out var value))
                value.Add(new(key, skillInfo, overrideSkillInfo, progress));
            else
                result.Add(keyProto.SkillGroupId, new() { new(key, skillInfo, overrideSkillInfo, progress) });
        }

        return result;
    }
}
