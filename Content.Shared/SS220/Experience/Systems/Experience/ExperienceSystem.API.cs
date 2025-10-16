// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt


using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Experience.Systems;

public sealed partial class ExperienceSystem : EntitySystem
{
    #region Skill getters
    public HashSet<ProtoId<SkillPrototype>> GetAcquiredSkills(Entity<ExperienceComponent?> entity, ProtoId<SkillTreePrototype> skillTree)
    {
        if (!Resolve(entity.Owner, ref entity.Comp))
            return [];

        if (!entity.Comp.OverrideSkills.ContainsKey(skillTree) || !entity.Comp.Skills.ContainsKey(skillTree))
            return [];

        if (!_prototype.TryIndex(skillTree, out var treeProto))
            return [];

        var treeInfo = entity.Comp.Skills[skillTree];
        var amountToTake = treeInfo.SkillStudied ? treeInfo.SkillLevel : treeInfo.SkillLevel - 1;

        return [.. treeProto.SkillTree.Take(amountToTake)];
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
