// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Experience.Systems;

public sealed partial class ExperienceSystem : EntitySystem
{
    #region Progress skill tree

    public bool TryChangeStudyingProgress(Entity<ExperienceComponent?> entity, ProtoId<SkillTreePrototype> skillTree, LearningInformation info)
    {
        if (!Resolve(entity.Owner, ref entity.Comp, false))
            return false;

        TryGetSkillTreeLevel(entity, skillTree, out var level);

        var levelDeltaModifier = info.LearningDecreaseFactorPerLevel * (level - info.PeakLearningLevel) ?? 0;
        var delta = info.BaseLearning + levelDeltaModifier;

        return TryChangeStudyingProgress(entity, skillTree, FixedPoint4.Clamp(delta, info.MinProgress, info.MaxProgress));
    }

    public bool TryChangeStudyingProgress(Entity<ExperienceComponent?> entity, ProtoId<SkillTreePrototype> skillTree, FixedPoint4 delta)
    {
        // for unpredicted events
        if (!_gameTiming.IsFirstTimePredicted)
            return false;

        if (!Resolve(entity.Owner, ref entity.Comp, false))
            return false;

        if (!entity.Comp.StudyingProgress.ContainsKey(skillTree))
            entity.Comp.StudyingProgress.Add(skillTree, StartLearningProgress);

        delta = ApplyMentorEffect(entity, skillTree, delta);

        entity.Comp.StudyingProgress[skillTree] += delta;

        TryProgressSublevel(entity!, skillTree);
        TryProgressLevel(entity!, skillTree);

        DirtyField(entity, nameof(ExperienceComponent.StudyingProgress));
        return true;
    }

    #endregion

    #region Skill getters

    public bool HaveSkill(Entity<ExperienceComponent?> entity, [ForbidLiteral] ProtoId<SkillPrototype> skill)
    {
        if (!_prototype.Resolve(skill, out var _))
            return false;

        if (HasComp<BypassSkillCheckComponent>(entity))
            return true;

        if (!Resolve(entity.Owner, ref entity.Comp, logMissing: false))
            return false;

        if (!ResolveSkillTreeFromSkill(skill, out var skillTree))
            return false;

        var treeInfo = entity.Comp.OverrideSkills.TryGetValue(skillTree, out var overrideSkills) ? overrideSkills :
                        entity.Comp.Skills.TryGetValue(skillTree, out var skills) ? skills : null;

        if (treeInfo is null)
            return false;

        return skillTree.SkillTree.Take(treeInfo.Level).Contains(skill);
    }

    public bool TryGetAcquiredSkills(Entity<ExperienceComponent?> entity, [ForbidLiteral] ProtoId<SkillTreePrototype> skillTree, ref HashSet<ProtoId<SkillPrototype>> resultSkills)
    {
        if (!_prototype.Resolve(skillTree, out var treeProto))
            return false;

        if (HasComp<BypassSkillCheckComponent>(entity))
        {
            resultSkills = [.. treeProto.SkillTree];
            return true;
        }

        if (!Resolve(entity.Owner, ref entity.Comp, logMissing: false))
            return false;

        if (!entity.Comp.OverrideSkills.ContainsKey(skillTree) || !entity.Comp.Skills.ContainsKey(skillTree))
            return false;

        var treeInfo = entity.Comp.OverrideSkills.TryGetValue(skillTree, out var overrideSkills) ? overrideSkills :
                        entity.Comp.Skills.TryGetValue(skillTree, out var skills) ? skills : null;

        if (treeInfo is null)
            return false;

        resultSkills = [.. treeProto.SkillTree.Take(treeInfo.Level)];
        return true;
    }

    #endregion

    #region Set override skill

    public bool TrySetOverrideSkill(Entity<ExperienceComponent?> entity, ProtoId<SkillTreePrototype> skillTree, SkillTreeInfo overrideInfo)
    {
        if (!Resolve(entity.Owner, ref entity.Comp, false))
            return false;

        entity.Comp.OverrideSkills[skillTree] = overrideInfo;

        EnsureSkillEntityComponents(entity!, ExperienceComponent.OverrideContainerId);

        return true;
    }

    public bool TryRemoveOverrideSkill(Entity<ExperienceComponent?> entity, ProtoId<SkillTreePrototype> skillTree)
    {
        if (!Resolve(entity.Owner, ref entity.Comp, false))
            return false;

        entity.Comp.OverrideSkills.Remove(skillTree);

        EnsureSkillEntityComponents(entity!, ExperienceComponent.OverrideContainerId);

        return true;
    }
    #endregion

    #region Try get methods

    public bool TryGetOverrideSkillInfo(Entity<ExperienceComponent?> entity, ProtoId<SkillTreePrototype> skillTree, [NotNullWhen(true)] out SkillTreeInfo? overrideInfo)
    {
        overrideInfo = null;
        if (!Resolve(entity.Owner, ref entity.Comp, false))
            return false;

        return entity.Comp.OverrideSkills.TryGetValue(skillTree, out overrideInfo);
    }

    public bool TryGetSkillTreeLevel(Entity<ExperienceComponent?> entity, ProtoId<SkillTreePrototype> skillTree, [NotNullWhen(true)] out int? level)
    {
        return TryGetSkillTreeLevels(entity, skillTree, out level, out _);
    }

    public bool TryGetSkillTreeSubLevel(Entity<ExperienceComponent?> entity, ProtoId<SkillTreePrototype> skillTree, [NotNullWhen(true)] out int? sublevel)
    {
        return TryGetSkillTreeLevels(entity, skillTree, out _, out sublevel);
    }

    public bool TryGetSkillTreeLevels(Entity<ExperienceComponent?> entity, ProtoId<SkillTreePrototype> skillTree, [NotNullWhen(true)] out int? level, [NotNullWhen(true)] out int? sublevel)
    {
        if (!Resolve(entity.Owner, ref entity.Comp) || !entity.Comp.Skills.TryGetValue(skillTree, out var info))
        {
            level = null;
            sublevel = null;
            return false;
        }

        sublevel = info.Sublevel;
        level = info.Level;
        return true;
    }

    public bool TryGetEarnedSublevel(Entity<ExperienceComponent?> entity, ProtoId<SkillTreePrototype> skillTree, [NotNullWhen(true)] out int? earnedSublevels)
    {
        if (!Resolve(entity.Owner, ref entity.Comp) || !entity.Comp.EarnedSkillSublevel.TryGetValue(skillTree, out var earned))
        {
            earnedSublevels = null;
            return false;
        }

        earnedSublevels = earned;
        return true;
    }

    public bool TryGetLearningProgress(Entity<ExperienceComponent?> entity, ProtoId<SkillTreePrototype> skillTree, [NotNullWhen(true)] out FixedPoint4? progress)
    {
        if (!Resolve(entity.Owner, ref entity.Comp) || !entity.Comp.StudyingProgress.TryGetValue(skillTree, out var cachedProgress))
        {
            progress = null;
            return false;
        }

        progress = cachedProgress;
        return true;
    }
    #endregion
}
