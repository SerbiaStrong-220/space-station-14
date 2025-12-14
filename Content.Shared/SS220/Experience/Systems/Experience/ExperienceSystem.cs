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
    public const int StartSublevel = 0;
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

        foreach (var treeProto in _prototype.EnumeratePrototypes<SkillTreePrototype>())
        {
            entity.Comp.EarnedSkillSublevel.Add(treeProto, 0);
        }
        DirtyField(entity!, nameof(ExperienceComponent.EarnedSkillSublevel));

        InitializeExperienceComp(entity);
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

        if (entity.Comp.EarnedSkillSublevel.TryGetValue(skillTree, out var earnedSublevel))
            ev.Info.Sublevel += earnedSublevel;
        else
            Log.Error($"EarnedSkillSublevel of {ToPrettyString(entity)} doesn't contain {nameof(SkillTreePrototype)} with id {skillTree}!");

        ResolveInitLeveling(entity, ev.Info, ev.SkillTree);

        entity.Comp.Skills.Add(skillTree, ev.Info);
        entity.Comp.StudyingProgress.Add(skillTree, StartLearningProgress);

        DirtyField(entity!, nameof(ExperienceComponent.Skills));
    }

    private void ResolveInitLeveling(Entity<ExperienceComponent> entity, SkillTreeExperienceInfo info, ProtoId<SkillTreePrototype> tree)
    {
        var treeProto = _prototype.Index(tree);
        while (info.Level < treeProto.SkillTree.Count)
        {
            var skillId = treeProto.SkillTree[info.SkillTreeIndex];
            if (!_prototype.Resolve(skillId, out var skillPrototype))
                break;

            var maximumSublevel = skillPrototype.LevelInfo.MaximumSublevel;
            if (info.Sublevel < maximumSublevel)
                break;

            InternalProgressLevel(entity, info, skillPrototype, treeProto.ID);
        }
    }
}
