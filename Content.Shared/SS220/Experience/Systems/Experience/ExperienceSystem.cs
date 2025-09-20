// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Administration.Logs;
using Content.Shared.FixedPoint;
using Content.Shared.SS220.Experience.SkillChecks;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Experience.Systems;

public sealed partial class ExperienceSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogManager = default!;

    private const int StartSkillLevelIndex = 0;
    private const int StartSubLevelIndex = 0;
    private readonly FixedPoint4 _startLearningProgress = 0;
    private readonly FixedPoint4 _endLearningProgress = 1;

    private static readonly HashSet<string> ContainerIds = [
        ExperienceComponent.ContainerId,
        ExperienceComponent.OverrideContainerId
    ];

    private readonly EntProtoId _baseSKillPrototype = "InitSkillEntity";

    public override void Initialize()
    {
        base.Initialize();

        InitializeGainedExperience();

        SubscribeLocalEvent<ExperienceComponent, ComponentInit>(OnComponentInit);
        // TODO generic <T> version to bypass subscribe errors
        SubscribeLocalEvent<SkillRoleAddComponent, SkillTreeAddedEvent>(SkillAddOnSkillTreeAdded);
        SubscribeLocalEvent<ExperienceComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<ExperienceComponent, SkillCheckEvent>(OnSkillCheckEvent);
    }

    private void SkillAddOnSkillTreeAdded(Entity<SkillRoleAddComponent> entity, ref SkillTreeAddedEvent args)
    {
        if (_prototype.TryIndex(entity.Comp.SkillAddId, out var skillAddProto)
            && skillAddProto.Skills.TryGetValue(args.SkillTree, out var infoProto))
        {
            args.Info.SkillLevel += infoProto.SkillLevel;
            args.Info.SkillSublevel += infoProto.SkillSublevel;
        }

        if (entity.Comp.Skills.TryGetValue(args.SkillTree, out var info))
        {
            args.Info.SkillLevel += info.SkillLevel;
            args.Info.SkillSublevel += info.SkillSublevel;
        }
    }

    private void OnSkillCheckEvent(Entity<ExperienceComponent> entity, ref SkillCheckEvent args)
    {
        args.HasSkill = GetAcquiredSkills(entity.AsNullable(), args.TreeProto).Contains(args.SkillProto);
    }

    public bool TryChangeStudyingProgress(Entity<ExperienceComponent?> entity, ProtoId<SkillTreePrototype> skillTree, float delta)
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

        entity.Comp.StudyingProgress[skillTree] = FixedPoint4.Clamp(result, _startLearningProgress, _endLearningProgress);
        return true;
    }

    public void InitExperienceSkillTree(Entity<ExperienceComponent> entity, ProtoId<SkillTreePrototype> skillTree, bool logReiniting = true)
    {
        if (entity.Comp.Skills.ContainsKey(skillTree) || entity.Comp.StudyingProgress.ContainsKey(skillTree))
        {
            if (logReiniting)
                Log.Error("Tried to init skill that already existed or being studied");
            entity.Comp.Skills.Remove(skillTree);
            entity.Comp.StudyingProgress.Remove(skillTree);
        }

        var ev = new SkillTreeAddedEvent
        {
            SkillTree = skillTree,
            Info = new SkillTreeExperienceInfo { SkillLevel = StartSkillLevelIndex, SkillSublevel = StartSubLevelIndex }
        };
        RaiseLocalEvent(entity, ref ev);

        ResolveInitLeveling(entity, ev.Info, ev.SkillTree);

        entity.Comp.Skills.Add(skillTree, ev.Info);
        entity.Comp.StudyingProgress.Add(skillTree, _startLearningProgress);

        DirtyFields(entity.AsNullable(), null, [nameof(ExperienceComponent.Skills), nameof(ExperienceComponent.InitMask)]);

    }

    private void ResolveInitLeveling(Entity<ExperienceComponent> entity, SkillTreeExperienceInfo info, ProtoId<SkillTreePrototype> tree)
    {
        var treeProto = _prototype.Index(tree);

        if (!CanProgressTree(info, treeProto))
        {
            info.SkillSublevel = 0;
            info.SkillStudied = true;
            return;
        }

        const int maxCycles = 20;
        int cycle;
        bool canProgress = true;
        for (cycle = 0; cycle < maxCycles && canProgress; cycle++)
        {
            canProgress = TryProgressLevel(entity, info, treeProto) && TryProgressTree(info, treeProto);
        }

        if (cycle == maxCycles - 1)
            Log.Error($"Cant update progress for {maxCycles} while resolving {tree.Id}!");
    }
}
