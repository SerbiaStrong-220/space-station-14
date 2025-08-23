// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.Experience.Systems;

public sealed partial class ExperienceSystem : EntitySystem
{
    private bool CanProgressSubLevel(Entity<ExperienceComponent> entity, SkillTreePrototype treeProto)
    {
        //TODO
        return false;
    }

    private bool CanProgressLevel(SkillTreeExperienceInfo info, SkillTreePrototype treeProto)
    {
        if (!TryGetCurrentSkillPrototype(info, treeProto, out var skillProto))
            return false;

        return info.ExperienceLevel >= skillProto.LevelInfo.NumberOfSublevels;
    }

    private bool CanProgressTree(SkillTreeExperienceInfo info, SkillTreePrototype treeProto)
    {
        return info.SkillLevel < (treeProto.SkillTree.Count - 1);
    }

    private void ProgressLevelInInfo(SkillTreeExperienceInfo info, SkillTreePrototype treeProto)
    {
        if (!TryGetCurrentSkillPrototype(info, treeProto, out var skillProto))
            return;

        DebugTools.Assert(CanProgressLevel(info, treeProto));

        info.ExperienceLevel = Math.Max(StartSubLevelIndex, info.ExperienceLevel - skillProto.LevelInfo.NumberOfSublevels);

        // TODO: ????
        if (CanProgressTree(info, treeProto))
            info.SkillLevel++;
    }

    private bool TryGetCurrentSkillPrototype(SkillTreeExperienceInfo info, SkillTreePrototype treeProto, [NotNullWhen(true)] out SkillPrototype? skillPrototype)
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
