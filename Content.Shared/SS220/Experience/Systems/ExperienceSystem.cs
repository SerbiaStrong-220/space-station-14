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
    private readonly FixedPoint4 _startSublevelProgress = 0;
    private readonly FixedPoint4 _endSublevelProgress = 1;

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

    public bool TryProgressLevel(Entity<ExperienceComponent?> entity, ProtoId<SkillTreePrototype> skillTree, float delta)
    {
        if (!Resolve(entity.Owner, ref entity.Comp, false))
            return false;

        if (!entity.Comp.StudyingProgress.ContainsKey(skillTree))
            entity.Comp.StudyingProgress.Add(skillTree, _startSublevelProgress);

        var result = entity.Comp.StudyingProgress[skillTree] + delta;
        if (result > _endSublevelProgress)
        {
            InternalProgressSubLevel(entity!, skillTree);
            return true;
        }

        entity.Comp.StudyingProgress[skillTree] = FixedPoint4.Max(result, _startSublevelProgress);
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

        if (!CanProgressTree(info, treeProto))
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
