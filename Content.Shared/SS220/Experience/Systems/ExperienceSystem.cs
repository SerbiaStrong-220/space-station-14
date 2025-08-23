// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.Experience.Systems;

public sealed partial class ExperienceSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private const int StartSkillLevelIndex = 0;
    private const int StartSubLevelIndex = 0;
    private const float StartSublevelProgress = 0f;
    private const float EndSublevelProgress = 1f;

    public bool TryGetSkillTreeLevels(Entity<ExperienceComponent?> entity, ProtoId<SkillTreePrototype> skillTree, [NotNullWhen(true)] out int? level, [NotNullWhen(true)] out int? sublevel)
    {
        if (!Resolve(entity.Owner, ref entity.Comp) || !entity.Comp.Skills.TryGetValue(skillTree, out var info))
        {
            level = null;
            sublevel = null;
            return false;
        }

        sublevel = info.ExperienceLevel;
        level = info.SkillLevel;
        return true;
    }

    public bool TryProgressLevel(Entity<ExperienceComponent?> entity, ProtoId<SkillTreePrototype> skillTree, float delta)
    {
        if (!Resolve(entity.Owner, ref entity.Comp, false))
            return false;

        if (!entity.Comp.LearningProgress.ContainsKey(skillTree))
            entity.Comp.LearningProgress.Add(skillTree, StartSublevelProgress);

        var result = entity.Comp.LearningProgress[skillTree] + delta;
        if (result > EndSublevelProgress)
        {
            InternalProgressSubLevel(entity!, skillTree);
            return true;
        }

        entity.Comp.LearningProgress[skillTree] = Math.Max(result, StartSublevelProgress);
        return true;
    }


    private void InternalProgressSubLevel(Entity<ExperienceComponent> entity, ProtoId<SkillTreePrototype> skillTree)
    {
        if (!TryGetSkillTreeSubLevel(entity!, skillTree, out var sublevel))
        {
            InitExperienceSkillTree(entity, skillTree);

            // this can be strange but we raise event in init to collect all modifiers
            if (!TryGetSkillTreeSubLevel(entity!, skillTree, out sublevel))
            {
                Log.Error("Cant get sublevel info after initializing it");
                return;
            }
        }

        var prototype = _prototype.Index(skillTree);
        //TODO
    }

    public void InitExperienceSkillTree(Entity<ExperienceComponent> entity, ProtoId<SkillTreePrototype> skillTree)
    {
        if (entity.Comp.Skills.ContainsKey(skillTree))
        {
            Log.Error("Tried to init skill that already existed");
            entity.Comp.Skills.Remove(skillTree);
        }

        var ev = new SkillTreeAdded
        {
            SkillTree = skillTree,
            Info = new SkillTreeExperienceInfo { SkillLevel = StartSkillLevelIndex, ExperienceLevel = StartSubLevelIndex }
        };
        RaiseLocalEvent(entity, ref ev);

        // never knows what coming...
        DebugTools.Assert(ev.SkillTree == skillTree);
        ResolveLeveling(ev.Info, ev.SkillTree);

        entity.Comp.Skills.Add(skillTree, ev.Info);
    }

    private void InternalProgressSkillLevel(Entity<ExperienceComponent> entity, ProtoId<SkillTreePrototype> skillTree)
    {
        // TODO
    }

    private void ResolveLeveling(SkillTreeExperienceInfo info, ProtoId<SkillTreePrototype> tree)
    {
        var treeProto = _prototype.Index(tree);

        if (!CanProgressTree(info, treeProto))
        {
            var lastSkill = treeProto.SkillTree.Last();
            var lastSkillProto = _prototype.Index(lastSkill);
            info.ExperienceLevel = Math.Min(info.ExperienceLevel, lastSkillProto.LevelInfo.NumberOfSublevels);
        }

        const int maxCycle = 100;
        int cycle;
        bool canProgress = true;
        for (cycle = 0; (cycle < maxCycle) && canProgress; cycle++)
        {
            canProgress = CanProgressLevel(info, treeProto);
            if (canProgress)
            {

            }
        }

        if (cycle == maxCycle - 1)
        {
            Log.Error("Cant");
        }



    }




}
