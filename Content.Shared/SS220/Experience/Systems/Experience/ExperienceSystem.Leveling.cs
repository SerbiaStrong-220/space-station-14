// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using Content.Shared.Database;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.Experience.Systems;

public sealed partial class ExperienceSystem : EntitySystem
{
    #region Try methods

    /// <summary>
    ///  CARE this method are not making adminlog!
    /// </summary>
    private bool TryProgressTree(SkillTreeExperienceInfo info, SkillTreePrototype treeProto, Entity<ExperienceComponent>? entity = null)
    {
        if (!CanProgressTree(info, treeProto))
            return false;

        InternalProgressTree(info, treeProto, entity);
        return true;
    }

    private bool TryProgressLevel(Entity<ExperienceComponent> entity, ProtoId<SkillTreePrototype> tree)
    {
        if (!ResolveInfoAndTree(entity, tree, out var info, out var treeProto))
            return false;

        return TryProgressLevel(entity, info, treeProto);
    }

    private bool TryProgressLevel(Entity<ExperienceComponent> entity, SkillTreeExperienceInfo info, SkillTreePrototype treeProto)
    {
        if (!CanProgressLevel(info, treeProto))
            return false;

        InternalProgressLevel(entity, info, treeProto);
        return true;
    }

    private bool TryProgressSublevel(Entity<ExperienceComponent> entity, ProtoId<SkillTreePrototype> tree)
    {
        if (!CanProgressSublevel(entity, tree))
            return false;

        InternalProgressSublevel(entity, tree);
        return true;
    }

    #endregion

    #region Can methods

    /// <summary>
    /// Checks if we can start studying next skill
    /// image: [xxx]|[oo] -> [xxx][|oo]
    /// </summary>
    private bool CanProgressTree(SkillTreeExperienceInfo info, SkillTreePrototype treeProto)
    {
        if (TryGetNextSkillPrototype(info, treeProto, out var nextSkillProto))
            return nextSkillProto.LevelInfo.CanStartStudying;

        return false;
    }

    /// <summary>
    /// Checks if we can end studying current skill
    /// image: [xx|][ooo] -> [xx]|[ooo]
    /// </summary>
    private bool CanProgressLevel(SkillTreeExperienceInfo info, SkillTreePrototype treeProto)
    {
        if (info.SkillLevel >= treeProto.SkillTree.Count)
            return false;

        if (!TryGetCurrentSkillPrototype(info, treeProto, out var skillProto))
            return false;

        if (!skillProto.LevelInfo.CanEndStudying)
            return false;

        return info.SkillSublevel >= skillProto.LevelInfo.MaximumSublevel;
    }

    /// <summary>
    /// Checks if we gain next sublevel
    /// image: [xx|oo] -> [xxx|o]
    /// </summary>
    private bool CanProgressSublevel(Entity<ExperienceComponent> entity, ProtoId<SkillTreePrototype> tree)
    {
        if (!entity.Comp.StudyingProgress.TryGetValue(tree, out var progress))
            return false;

        if (!entity.Comp.Skills.TryGetValue(tree, out var info) || !_prototype.TryIndex(tree, out var treeProto))
            return false;

        if (!TryGetCurrentSkillPrototype(info, treeProto, out var studyingSkillProto))
            return false;

        // We care of start only at start
        if (info.SkillSublevel == StartLearningProgress && !studyingSkillProto.LevelInfo.CanStartStudying)
            return false;

        return progress >= EndLearningProgress;
    }

    #endregion

    #region Internal methods

    /// <summary>
    /// Handles starting studying next skill
    /// image: [xxx]|[oo] -> [xxx][|oo]
    /// </summary>
    private void InternalProgressTree(Entity<ExperienceComponent> entity, ProtoId<SkillTreePrototype> skillTree)
    {
        if (!ResolveInfoAndTree(entity, skillTree, out var info, out var treeProto))
            return;

        InternalProgressTree(info, treeProto, entity);

        DirtyField(entity.AsNullable(), nameof(ExperienceComponent.Skills));
    }

    private void InternalProgressTree(SkillTreeExperienceInfo info, SkillTreePrototype skillTree, Entity<ExperienceComponent>? effectedEntity)
    {
        DebugTools.Assert(CanProgressTree(info, skillTree), $"Called {nameof(InternalProgressTree)} but tree progress is blocked, info {info} and tree id is {skillTree.ID}");
        info.SkillLevel++;
        info.SkillStudied = false;

        if (effectedEntity is not null)
            _adminLogManager.Add(LogType.Experience, $"{ToPrettyString(effectedEntity):user} gained new skill");
    }

    /// <summary>
    /// Handles ending studying skill
    /// image: [xx|][ooo] -> [xx]|[ooo]
    /// </summary>
    private void InternalProgressLevel(Entity<ExperienceComponent> entity, ProtoId<SkillTreePrototype> skillTree)
    {
        if (!ResolveInfoAndTree(entity, skillTree, out var info, out var treeProto))
            return;

        InternalProgressLevel(entity, info, treeProto);
    }

    private void InternalProgressLevel(Entity<ExperienceComponent> entity, SkillTreeExperienceInfo info, SkillTreePrototype skillTree)
    {
        DebugTools.Assert(CanProgressLevel(info, skillTree));

        if (info.SkillStudied)
        {
            TryProgressTree(info, skillTree, entity);
            return;
        }

        if (!TryGetCurrentSkillPrototype(info, skillTree, out var skillPrototype))
        {
            Log.Error($"Cant get current skill proto for tree {skillTree.ID} and info is {info}");
            return;
        }

        if (!TryAddSkillToSkillEntity(entity, ExperienceComponent.ContainerId, skillPrototype))
            return;

        // we save meta level progress of sublevel
        info.SkillSublevel = Math.Max(StartSubLevelIndex, info.SkillLevel - skillPrototype.LevelInfo.MaximumSublevel);
        info.SkillStudied = true;

        DirtyField(entity.AsNullable(), nameof(ExperienceComponent.Skills));

        var ev = new SkillLevelGainedEvent(skillTree.ID, skillPrototype);

        RaiseLocalEvent(entity, ref ev);
    }

    /// <summary>
    /// Handles leveling sublevel
    /// image: [xx|oo] -> [xxx|o]
    /// </summary>
    private void InternalProgressSublevel(Entity<ExperienceComponent> entity, ProtoId<SkillTreePrototype> skillTree)
    {
        DebugTools.Assert(CanProgressSublevel(entity, skillTree));

        // start studying tree is not handled by this method
        if (!entity.Comp.StudyingProgress.TryGetValue(skillTree, out var _))
            return;

        if (!ResolveInfoAndTree(entity, skillTree, out var info, out var _))
            return;

        // Do not save overflow progress of it
        entity.Comp.StudyingProgress[skillTree] = StartLearningProgress;
        info.SkillSublevel++;

        DirtyFields(entity.AsNullable(), null, [nameof(ExperienceComponent.Skills), nameof(ExperienceComponent.StudyingProgress)]);
    }

    private bool ResolveInfoAndTree(Entity<ExperienceComponent> entity, ProtoId<SkillTreePrototype> skillTree,
                                [NotNullWhen(true)] out SkillTreeExperienceInfo? info, [NotNullWhen(true)] out SkillTreePrototype? prototype,
                                bool logMissing = true)
    {
        prototype = null;
        info = null;

        if (!entity.Comp.Skills.TryGetValue(skillTree, out var skillInfo))
        {
            if (logMissing)
                Log.Error($"Cant get skill info for progress sublevel in tree {skillTree} and entity {ToPrettyString(entity)}!");

            return false;
        }

        if (!_prototype.TryIndex(skillTree, out prototype))
        {
            if (logMissing)
                Log.Error($"Cant index skill tree prototype with id {skillTree}");

            return false;
        }

        info = skillInfo;
        return true;
    }
    #endregion
}
