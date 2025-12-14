// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using Content.Shared.Database;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.Experience.Systems;

public sealed partial class ExperienceSystem : EntitySystem
{
    #region Try methods

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
    /// Checks if we can end studying current skill
    /// image: [xx|][ooo] -> [xx][|ooo]
    /// </summary>
    private bool CanProgressLevel(SkillTreeExperienceInfo info, SkillTreePrototype treeProto)
    {
        if (info.Level >= treeProto.SkillTree.Count)
            return false;

        if (!ResolveCurrentSkillPrototype(info, treeProto, out var skillProto))
            return false;

        if (!TryGetNextSkillPrototype(info, treeProto, out var nextSkillProto))
            return false;

        return CanProgressLevel(info, skillProto, nextSkillProto);
    }

    /// <summary>
    /// <inheritdoc cref="CanProgressLevel(SkillTreeExperienceInfo, SkillTreePrototype)" />
    /// </summary>
    private bool CanProgressLevel(SkillTreeExperienceInfo info, SkillPrototype skillProto, SkillPrototype nextSkillProto)
    {
        if (!nextSkillProto.LevelInfo.CanStartStudying)
            return false;

        if (!skillProto.LevelInfo.CanEndStudying)
            return false;

        return info.Sublevel >= skillProto.LevelInfo.MaximumSublevel;
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

        if (info.Level >= treeProto.SkillTree.Count)
            return false;

        if (!ResolveCurrentSkillPrototype(info, treeProto, out var studyingSkillProto))
            return false;

        // We care of start only at start
        if (info.Sublevel == StartLearningProgress && !studyingSkillProto.LevelInfo.CanStartStudying)
            return false;

        return progress >= EndLearningProgress;
    }

    #endregion

    #region Internal methods

    /// <summary>
    /// Handles ending studying skill
    /// image: [xx|][ooo] -> [xx][|ooo]
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

        if (!ResolveCurrentSkillPrototype(info, skillTree, out var skillPrototype))
            return;

        // not logging cause all false in method logged
        if (!TryAddSkillToSkillEntity(entity, ExperienceComponent.ContainerId, skillPrototype))
            return;

        InternalProgressLevel(entity, info, skillPrototype, skillTree.ID);
    }

    private void InternalProgressLevel(Entity<ExperienceComponent> entity, SkillTreeExperienceInfo info, SkillPrototype skillPrototype, ProtoId<SkillTreePrototype> skillTreeId)
    {
        // we save meta level progress of sublevel
        info.Sublevel = Math.Max(StartSublevel, info.Sublevel - skillPrototype.LevelInfo.MaximumSublevel + StartSublevel);
        info.Level++;

        DirtyField(entity.AsNullable(), nameof(ExperienceComponent.Skills));

        _popup.PopupClient(Loc.GetString(skillPrototype.LevelInfo.LevelUpPopup), entity, entity, Popups.PopupType.Medium);

        _adminLogManager.Add(LogType.Experience, $"{ToPrettyString(entity):user} gained new skill");

        var ev = new SkillLevelGainedEvent(skillTreeId, skillPrototype);

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

        if (!ResolveInfoAndTree(entity, skillTree, out var info, out var skillTreePrototype))
            return;

        if (!ResolveCurrentSkillPrototype(info, skillTree, out var skillPrototype))
            return;

        // Do not save overflow progress of it
        entity.Comp.StudyingProgress[skillTree] = StartLearningProgress;
        entity.Comp.EarnedSkillSublevel[skillTree]++;
        info.Sublevel++;

        if (!CanProgressLevel(info, skillTreePrototype))
            _popup.PopupClient(Loc.GetString(skillPrototype.LevelInfo.SublevelUpPopup), entity, entity);

        DirtyFields(entity.AsNullable(), null, [nameof(ExperienceComponent.Skills), nameof(ExperienceComponent.StudyingProgress)]);
    }
    #endregion
}
