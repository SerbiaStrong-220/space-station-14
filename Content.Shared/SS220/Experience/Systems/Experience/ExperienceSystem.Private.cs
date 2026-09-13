// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.Experience.Systems;

public sealed partial class ExperienceSystem : EntitySystem
{
    private FrozenDictionary<ProtoId<SkillPrototype>, ProtoId<SkillTreePrototype>> _skillSkillTrees = default!;

    private void InitializePrivate()
    {
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnReloadPrototypes);

        RebuildSkillSkillTree();
    }

    private void OnReloadPrototypes(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<SkillTreePrototype>() || args.WasModified<SkillPrototype>())
            RebuildSkillSkillTree();
    }

    private void RebuildSkillSkillTree()
    {
        var skillTrees = _prototype.EnumeratePrototypes<SkillTreePrototype>();
        var result = new Dictionary<ProtoId<SkillPrototype>, ProtoId<SkillTreePrototype>>();

        foreach (var skillTree in skillTrees)
        {
            foreach (var skill in skillTree.SkillTree)
            {
                DebugTools.Assert(!result.ContainsKey(skill), "Cant have same skill in two different skill tree prototypes");

                result.Add(skill, skillTree);
            }
        }

        _skillSkillTrees = result.ToFrozenDictionary();
    }

    private bool ValidContainerId(string containerId, EntityUid? entity = null)
    {
        if (!ContainerIds.Contains(containerId))
        {
            Log.Error($"Tried to ensure skill of entity {ToPrettyString(entity)} but skill entity container was incorrect, provided value {containerId}");
            return false;
        }

        return true;
    }

    private bool ResolveSkillTreeFromSkill(ProtoId<SkillPrototype> skillId, [NotNullWhen(true)] out SkillTreePrototype? skillTree)
    {
        skillTree = null;
        if (!_skillSkillTrees.TryGetValue(skillId, out var skillTreeId))
        {
            Log.Error($"Cant get {nameof(SkillTreePrototype)} id for {nameof(SkillPrototype)} with id {skillId}");
            return false;
        }

        // Here Index because _skillSkillTrees build on existing protos
        skillTree = _prototype.Index(skillTreeId);
        return true;
    }

    private bool ResolveInfoAndTree(Entity<ExperienceComponent> entity, ProtoId<SkillTreePrototype> skillTree,
                                [NotNullWhen(true)] out SkillTreeInfo? info,
                                [NotNullWhen(true)] out SkillTreePrototype? prototype,
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

        DebugTools.Assert(skillInfo.Level <= prototype.SkillTree.Count);
        DebugTools.Assert(skillInfo.Sublevel <= _prototype.Index(prototype.SkillTree[skillInfo.SkillTreeIndex]).LevelInfo.MaximumSublevel);

        info = skillInfo;
        return true;
    }

    private FixedPoint4 ApplyMentorEffect(Entity<ExperienceComponent?> entity, ProtoId<SkillTreePrototype> treeId, FixedPoint4 oldValue)
    {
        if (!TryComp<AffectedByMentorComponent>(entity, out var affectedByMentorComponent))
            return oldValue;

        if (!affectedByMentorComponent.TeachInfo.TryGetValue(treeId, out var mentorEffectData))
            return oldValue;

        if (!TryGetSkillTreeLevel(entity, treeId, out var level))
            return oldValue;

        // care this check use that null gives false!
        if (level >= mentorEffectData.MaxBuffSkillLevel)
            return oldValue;

        return mentorEffectData.Flat + oldValue * mentorEffectData.Multiplier;
    }
}
