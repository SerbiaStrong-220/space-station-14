// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.Experience.Systems;

public sealed partial class ExperienceSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private const int StartSkillLevelIndex = 0;
    private const int StartSubLevelIndex = 0;
    private readonly FixedPoint4 _startLearningProgress = 0;
    private readonly FixedPoint4 _endLearningProgress = 1;

    public override void Initialize()
    {
        base.Initialize();


    }

    public bool TryProgressLevel(Entity<ExperienceComponent?> entity, ProtoId<SkillTreePrototype> skillTree, float delta)
    {
        if (!Resolve(entity.Owner, ref entity.Comp, false))
            return false;

        if (!entity.Comp.StudyingProgress.ContainsKey(skillTree))
            entity.Comp.StudyingProgress.Add(skillTree, _startLearningProgress);

        var result = entity.Comp.StudyingProgress[skillTree] + delta;
        if (result > _endLearningProgress)
        {
            InternalProgressSublevel(entity!, skillTree);
            return true;
        }

        entity.Comp.StudyingProgress[skillTree] = FixedPoint4.Max(result, _startLearningProgress);
        return true;
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
            Info = new SkillTreeExperienceInfo { SkillLevel = StartSkillLevelIndex, SkillSublevel = StartSubLevelIndex }
        };
        RaiseLocalEvent(entity, ref ev);

        // never knows what coming...
        DebugTools.Assert(ev.SkillTree == skillTree);
        ResolveLeveling(ev.Info, ev.SkillTree);

        entity.Comp.Skills.Add(skillTree, ev.Info);

        DirtyField(entity!, nameof(ExperienceComponent.Skills));
    }

    private void InternalProgressSkillLevel(Entity<ExperienceComponent> entity, ProtoId<SkillTreePrototype> skillTree)
    {
        // TODO
    }

    private void ResolveLeveling(SkillTreeExperienceInfo info, ProtoId<SkillTreePrototype> tree)
    {
        var treeProto = _prototype.Index(tree);

        if (!CanProgressLevel(info, treeProto))
        {
            var lastSkill = treeProto.SkillTree.Last();
            var lastSkillProto = _prototype.Index(lastSkill);
            info.SkillSublevel = Math.Min(info.SkillSublevel, lastSkillProto.LevelInfo.MaximumSublevel);
        }

        const int maxCycles = 50;
        int cycle;
        bool canProgress = true;
        for (cycle = 0; (cycle < maxCycles) && canProgress; cycle++)
        {
            canProgress = CanProgressLevel(info, treeProto);
            if (canProgress)
            {

            }
        }

        if (cycle == maxCycles - 1)
        {
            Log.Error($"Cant update progress for {maxCycles} while resolving {tree.Id}!");
        }



    }




}
