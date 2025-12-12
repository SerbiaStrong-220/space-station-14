// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
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
}
