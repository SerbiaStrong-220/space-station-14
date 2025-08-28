// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.Experience.Systems;

public sealed partial class ExperienceSystem : EntitySystem
{
    private bool CanProgressSublevel(Entity<ExperienceComponent> entity, ProtoId<SkillTreePrototype> tree)
    {
        if (!entity.Comp.StudyingProgress.TryGetValue(tree, out var progress))
            return false;

        if (!entity.Comp.Skills.TryGetValue(tree, out var skillInfo))
        {
            Log.Error($"TreeProto have started studying but don't have skill info. Entity is {ToPrettyString(entity)}");
            return false;
        }

        var treeProto = _prototype.Index(tree);
        if (TryGetStudyingSkillPrototype(skillInfo, treeProto, out var skillProto)
            && skillInfo.SkillSublevel == skillProto.LevelInfo.MaximumSublevel)
            return skillProto.LevelInfo.CanEndStudying;

        return progress >= _endSublevelProgress;
    }

    private bool CanProgressLevel(SkillTreeExperienceInfo info, SkillTreePrototype treeProto)
    {
        if (!TryGetStudyingSkillPrototype(info, treeProto, out var skillProto))
            return false;

        return info.SkillSublevel >= skillProto.LevelInfo.MaximumSublevel;
    }

    private bool CanProgressTree(SkillTreeExperienceInfo info, SkillTreePrototype treeProto)
    {
        return info.SkillLevel < treeProto.SkillTree.Count - 1;
    }

    private void ProgressLevelInInfo(SkillTreeExperienceInfo info, SkillTreePrototype treeProto)
    {
        if (!TryGetStudyingSkillPrototype(info, treeProto, out var studyingSkillProto))
            return;

        if (!studyingSkillProto.LevelInfo.CanEndStudying)
            return;

        DebugTools.Assert(CanProgressLevel(info, treeProto));

        info.SkillSublevel = Math.Max(StartSubLevelIndex, info.SkillSublevel - studyingSkillProto.LevelInfo.MaximumSublevel);
        if (CanProgressTree(info, treeProto))
            info.SkillLevel++;
    }

    private bool TryGetAcquiredSkillPrototype(SkillTreeExperienceInfo info, SkillTreePrototype treeProto, [NotNullWhen(true)] out SkillPrototype? skillPrototype)
    {
        return TryGetSkillPrototypeInternal(info, treeProto, out skillPrototype);
    }

    private bool TryGetStudyingSkillPrototype(SkillTreeExperienceInfo info, SkillTreePrototype treeProto, [NotNullWhen(true)] out SkillPrototype? skillPrototype)
    {
        return TryGetSkillPrototypeInternal(info, treeProto, out skillPrototype);
    }

    private bool TryGetSkillPrototypeInternal(SkillTreeExperienceInfo info, SkillTreePrototype treeProto, [NotNullWhen(true)] out SkillPrototype? skillPrototype)
    {
        skillPrototype = null;

        if (info.SkillLevel >= treeProto.SkillTree.Count || info.SkillLevel < 0)
        {
            Log.Error($"Got error with resolving skill in ${treeProto} skill tree with provided level {info.SkillLevel}");
            return false;
        }

        var skillDefinition = treeProto.SkillTree[info.SkillLevel];

        if (!_prototype.TryIndex(skillDefinition, out skillPrototype))
        {
            Log.Error($"Cant index skill proto with id {skillDefinition}");
            return false;
        }

        return true;
    }
}
