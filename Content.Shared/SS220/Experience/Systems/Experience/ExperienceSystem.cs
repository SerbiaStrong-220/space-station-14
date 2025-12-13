// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Runtime.InteropServices;
using Content.Shared.Administration.Logs;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Content.Shared.SS220.Experience.SkillChecks;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.Experience.Systems;

public sealed partial class ExperienceSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogManager = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public const int StartSkillLevel = 1;
    public const int StartSublevel = 1;
    public static readonly FixedPoint4 StartLearningProgress = 0f;
    public static readonly FixedPoint4 EndLearningProgress = FixedPoint4.New(1f);

    private static readonly HashSet<string> ContainerIds = [
        ExperienceComponent.ContainerId,
        ExperienceComponent.OverrideContainerId
    ];

    public override void Initialize()
    {
        base.Initialize();

        InitializeGainedExperience();
        InitializeSkillEntityEvents();
        InitializePrivate();

        SubscribeLocalEvent<ExperienceComponent, SkillCheckEvent>(OnSkillCheckEvent);
        SubscribeLocalEvent<ExperienceComponent, MapInitEvent>(OnMapInit);
    }

    private void OnSkillCheckEvent(Entity<ExperienceComponent> entity, ref SkillCheckEvent args)
    {
        args.HasSkill = HaveSkill(entity!, args.SkillProto);
    }

    private void OnMapInit(Entity<ExperienceComponent> entity, ref MapInitEvent args)
    {
        OnMapInitSkillEntity(entity, ref args);

        InitializeExperienceComp(entity, InitGainedExperienceType.MapInit);
    }

    public bool TryChangeStudyingProgress(Entity<ExperienceComponent?> entity, ProtoId<SkillTreePrototype> skillTree, LearningInformation info)
    {
        if (!Resolve(entity.Owner, ref entity.Comp, false))
            return false;

        TryGetSkillTreeLevel(entity, skillTree, out var level);

        var levelDeltaModifier = info.LearningDecreaseFactorPerLevel * (level - info.PeakLearningLevel) ?? 0;
        var delta = info.BaseLearning + levelDeltaModifier;

        return TryChangeStudyingProgress(entity, skillTree, FixedPoint4.Clamp(delta, info.MinProgress, info.MaxProgress));
    }

    public bool TryChangeStudyingProgress(Entity<ExperienceComponent?> entity, ProtoId<SkillTreePrototype> skillTree, FixedPoint4 delta)
    {
        // for unpredicted events
        if (!_gameTiming.IsFirstTimePredicted)
            return false;

        if (!Resolve(entity.Owner, ref entity.Comp, false))
            return false;

        if (!entity.Comp.StudyingProgress.ContainsKey(skillTree))
            entity.Comp.StudyingProgress.Add(skillTree, StartLearningProgress);

        entity.Comp.StudyingProgress[skillTree] += delta;

        TryProgressSublevel(entity!, skillTree);
        TryProgressLevel(entity!, skillTree);

        DirtyField(entity, nameof(ExperienceComponent.StudyingProgress));
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

        var ev = new SkillTreeAdded
        {
            SkillTree = skillTree,
            Info = new SkillTreeExperienceInfo
            {
                Level = StartSkillLevel,
                Sublevel = StartSublevel,
            }
        };
        RaiseLocalEvent(entity, ref ev);

        ResolveInitLeveling(entity, ev.Info, ev.SkillTree);

        entity.Comp.Skills.Add(skillTree, ev.Info);
        entity.Comp.StudyingProgress.Add(skillTree, StartLearningProgress);

        DirtyFields(entity.AsNullable(), null, [nameof(ExperienceComponent.Skills), nameof(ExperienceComponent.InitMask)]);
    }

    private void ResolveInitLeveling(Entity<ExperienceComponent> entity, SkillTreeExperienceInfo info, ProtoId<SkillTreePrototype> tree)
    {
        var treeProto = _prototype.Index(tree);

        if (treeProto.SkillTree.Count <= info.Level)
        {
            info.Level = treeProto.SkillTree.Count;
            info.Sublevel = StartSublevel;
        }

        const int maxCycles = 20;
        int cycle;
        var canProgress = true;
        for (cycle = 0; cycle < maxCycles && canProgress; cycle++)
        {
            canProgress = TryProgressLevel(entity, info, treeProto);
        }

        if (cycle == maxCycles - 1)
            Log.Error($"Cant update progress for {maxCycles} while resolving {tree.Id}!");
    }
}
