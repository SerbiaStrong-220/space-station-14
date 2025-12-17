// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Experience.Systems;

public sealed partial class ExperienceSystem : EntitySystem
{
    private bool TryGetPreviousSkillPrototype(SkillTreeInfo info, ProtoId<SkillTreePrototype> treeProto, [NotNullWhen(true)] out SkillPrototype? skillPrototype)
    {
        skillPrototype = null;
        if (!_prototype.Resolve(treeProto, out var resolvedProto))
            return false;

        return TryGetPreviousSkillPrototype(info, resolvedProto, out skillPrototype);
    }

    private bool TryGetPreviousSkillPrototype(SkillTreeInfo info, SkillTreePrototype treeProto, [NotNullWhen(true)] out SkillPrototype? skillPrototype)
    {
        if (info.Level <= 0)
        {
            skillPrototype = null;
            return false;
        }

        return ResolveSkillPrototypeInternal(info.Level - 1, treeProto, out skillPrototype);
    }

    private bool ResolveCurrentSkillPrototype(SkillTreeInfo info, ProtoId<SkillTreePrototype> treeProto, [NotNullWhen(true)] out SkillPrototype? skillPrototype)
    {
        skillPrototype = null;
        if (!_prototype.Resolve(treeProto, out var resolvedProto))
            return false;

        return ResolveCurrentSkillPrototype(info, resolvedProto, out skillPrototype);
    }

    private bool ResolveCurrentSkillPrototype(SkillTreeInfo info, SkillTreePrototype treeProto, [NotNullWhen(true)] out SkillPrototype? skillPrototype)
    {
        return ResolveSkillPrototypeInternal(info.Level, treeProto, out skillPrototype);
    }

    private bool TryGetNextSkillPrototype(SkillTreeInfo info, ProtoId<SkillTreePrototype> treeProto, [NotNullWhen(true)] out SkillPrototype? skillPrototype)
    {
        skillPrototype = null;
        if (!_prototype.Resolve(treeProto, out var resolvedProto))
            return false;

        return TryGetNextSkillPrototype(info, resolvedProto, out skillPrototype);
    }

    private bool TryGetNextSkillPrototype(SkillTreeInfo info, SkillTreePrototype treeProto, [NotNullWhen(true)] out SkillPrototype? skillPrototype)
    {
        if (info.Level >= treeProto.SkillTree.Count)
        {
            skillPrototype = null;
            return false;
        }

        return ResolveSkillPrototypeInternal(info.Level + 1, treeProto, out skillPrototype);
    }

    private bool ResolveSkillPrototypeInternal(int skillLevel, SkillTreePrototype treeProto, [NotNullWhen(true)] out SkillPrototype? skillPrototype)
    {
        skillPrototype = null;
        var skillIndex = skillLevel - 1;

        if (skillIndex > treeProto.SkillTree.Count || skillIndex < 0)
        {
            Log.Error($"Got error with resolving skill in {treeProto.ID} skill tree with provided level {skillLevel}");
            return false;
        }

        var skillDefinition = treeProto.SkillTree[skillIndex];

        if (!_prototype.TryIndex(skillDefinition, out skillPrototype))
        {
            Log.Error($"Cant index skill proto with id {skillDefinition}");
            return false;
        }

        return true;
    }
}
