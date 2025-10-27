// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Experience.Systems;

public sealed partial class ExperienceSystem : EntitySystem
{
    #region Progress skill tree

    public bool TryAddSkillTreeProgress(Entity<ExperienceComponent?> entity, ProtoId<SkillTreePrototype> skillTree, FixedPoint4 addition)
    {
        if (!Resolve(entity.Owner, ref entity.Comp, logMissing: false))
            return false;

        if (!entity.Comp.StudyingProgress.ContainsKey(skillTree))
            return false;

        entity.Comp.StudyingProgress[skillTree] += addition;

        TryProgressSublevel(entity!, skillTree);
        return true;
    }

    #endregion

    #region Skill getters

    public bool HaveSkill(Entity<ExperienceComponent?> entity, ProtoId<SkillTreePrototype> skillTree, ProtoId<SkillPrototype> skill)
    {
        if (!_prototype.Resolve(skillTree, out var treeProto))
            return false;

        if (HasComp<BypassSkillCheckComponent>(entity))
            return true;

        if (!Resolve(entity.Owner, ref entity.Comp, logMissing: false))
            return false;

        var treeInfo = entity.Comp.OverrideSkills.TryGetValue(skillTree, out var overrideSkills) ? overrideSkills :
                        entity.Comp.Skills.TryGetValue(skillTree, out var skills) ? skills : null;

        if (treeInfo is null)
            return false;

        var amountToTake = treeInfo.SkillStudied ? treeInfo.SkillLevel : treeInfo.SkillLevel - 1;

        return treeProto.SkillTree.Take(amountToTake).Contains(skill);
    }

    public bool TryGetAcquiredSkills(Entity<ExperienceComponent?> entity, ProtoId<SkillTreePrototype> skillTree, ref HashSet<ProtoId<SkillPrototype>> resultSkills)
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

        var amountToTake = treeInfo.SkillStudied ? treeInfo.SkillLevel : treeInfo.SkillLevel - 1;

        resultSkills = [.. treeProto.SkillTree.Take(amountToTake)];
        return true;
    }

    #endregion

    #region TryGet methods

    public bool TryGetExperienceEntityFromSkillEntity(Entity<SkillComponent?> entity, [NotNullWhen(true)] out Entity<ExperienceComponent>? experienceEntity)
    {
        experienceEntity = null;

        if (!Resolve(entity.Owner, ref entity.Comp, logMissing: false))
            return false;

        if (!_container.IsEntityInContainer(entity))
        {
            Log.Error($"Got entity {ToPrettyString(entity)} with {nameof(SkillComponent)} but not in container");
            return false;
        }

        var parentUid = Transform(entity).ParentUid;

        if (!TryComp<ExperienceComponent>(parentUid, out var experienceComponent))
        {
            Log.Error($"Got entity {ToPrettyString(entity)} in container which entity owner don't have {nameof(ExperienceComponent)}");
            return false;
        }

        experienceEntity = (parentUid, experienceComponent);
        return true;
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

        sublevel = info.SkillSublevel;
        level = info.SkillLevel;
        return true;
    }
    #endregion
}
