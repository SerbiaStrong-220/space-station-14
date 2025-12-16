// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Experience.Systems;

public sealed partial class ExperienceSystem : EntitySystem
{
    private void InitializeGainedExperience()
    {
        SubscribeLocalEvent<ExperienceComponent, RecalculateEntityExperience>(OnRecalculateEntityExperience);

        SubscribeLocalEvent<AntagFreeSublevelPointsAddComponent, SublevelAdditionPointInitialResolve>(AddFreeSublevelPointOnSublevelAdditionPointInitialResolve);
        SubscribeLocalEvent<AntagFreeSublevelPointsAddComponent, MapInitEvent>(AddFreeSublevelPointOnMapInit);

        SubscribeAddComponentToInit<AdminForcedExperienceAddComponent>(SkillForceSetOnSkillTreeAdded, KnowledgeForceSetOnKnowledgeInitialResolve, ForceSetAdditionOnSublevelAdditionPointInitialResolve);
        SubscribeAddComponentToInit<RoleExperienceAddComponent>(SkillAddOnSkillTreeAdded, KnowledgeAddOnKnowledgeInitialResolve, AdditionOnSublevelAdditionPointInitialResolve);

        SubscribeSublevelAddComponentToInit<BackgroundSublevelAddComponent>(SublevelAddOnSkillTreeAdded, SublevelAddOnSublevelAdditionPointInitialResolve);
    }

    private void SubscribeAddComponentToInit<T>(EntityEventRefHandler<T, SkillTreeAdded> handlerSkill,
                                            EntityEventRefHandler<T, KnowledgeInitialResolve> handlerKnowledge,
                                            EntityEventRefHandler<T, SublevelAdditionPointInitialResolve> handlerAdditionPoint)
                                            where T : BaseExperienceAddComponent
    {
        SubscribeLocalEvent<T, SkillTreeAdded>(handlerSkill);
        SubscribeLocalEvent<T, KnowledgeInitialResolve>(handlerKnowledge);
        SubscribeLocalEvent<T, SublevelAdditionPointInitialResolve>(handlerAdditionPoint);
    }

    private void SubscribeSublevelAddComponentToInit<T>(EntityEventRefHandler<T, SkillTreeAdded> handlerSkill,
                                            EntityEventRefHandler<T, SublevelAdditionPointInitialResolve> handlerAdditionPoint)
                                            where T : BaseSublevelAddComponent
    {
        SubscribeLocalEvent<T, SkillTreeAdded>(handlerSkill);
        SubscribeLocalEvent<T, SublevelAdditionPointInitialResolve>(handlerAdditionPoint);
    }

    private void OnRecalculateEntityExperience(Entity<ExperienceComponent> entity, ref RecalculateEntityExperience args)
    {
        if (!entity.Comp.SkillEntityInitialized)
        {
            Log.Debug($"Called {nameof(RecalculateEntityExperience)} before {ToPrettyString(entity)} skill entities was initialized!");
            return;
        }

        InitializeExperienceComp(entity);
    }

    private void AddFreeSublevelPointOnMapInit<T>(Entity<T> entity, ref MapInitEvent _) where T : BaseFreeSublevelPointsAddComponent
    {
        var recalculateEv = new RecalculateEntityExperience();
        RaiseLocalEvent(entity, ref recalculateEv);
    }

    private void AddFreeSublevelPointOnSublevelAdditionPointInitialResolve<T>(Entity<T> entity, ref SublevelAdditionPointInitialResolve args) where T : BaseFreeSublevelPointsAddComponent
    {
        args.FreeSublevelPoints += entity.Comp.AddFreeSublevelPoints;
    }

    private void InitializeExperienceComp(Entity<ExperienceComponent> entity)
    {
        foreach (var treeProto in _prototype.EnumeratePrototypes<SkillTreePrototype>())
        {
            if (!treeProto.CanBeShownOnInit)
                continue;

            if (!entity.Comp.EarnedSkillSublevel.ContainsKey(treeProto))
                entity.Comp.EarnedSkillSublevel.Add(treeProto, 0);

            // Not logging reiniting cause it defined behavior for our case
            InitExperienceSkillTree(entity, treeProto, false);
        }

        EnsureSkillEffectApplied(entity!);

        var addSublevelEvent = new SublevelAdditionPointInitialResolve(0);
        RaiseLocalEvent(entity, ref addSublevelEvent);
        entity.Comp.FreeSublevelPoints = Math.Max(addSublevelEvent.FreeSublevelPoints, 0);

        var ev = new KnowledgeInitialResolve([]);
        RaiseLocalEvent(entity, ref ev);

        entity.Comp.ConstantKnowledge.Clear();
        entity.Comp.ResolvedKnowledge.Clear();

        foreach (var knowledge in ev.Knowledges)
        {
            if (!TryAddKnowledge(entity!, knowledge))
                Log.Error($"Cant add knowledge {knowledge} to {ToPrettyString(entity)}");
        }
    }

    #region Skill

    private void SkillForceSetOnSkillTreeAdded<T>(Entity<T> entity, ref SkillTreeAdded args) where T : BaseExperienceAddComponent
    {
        if (args.DenyChanges)
            return;

        args.DenyChanges = true;

        if (entity.Comp.Skills.TryGetValue(args.SkillTree, out var info))
        {
            args.Info.Level = info.Level;
            args.Info.Sublevel = info.Sublevel;
        }

        if (entity.Comp.SkillAddId is null || !_prototype.TryIndex(entity.Comp.SkillAddId, out var skillAddProto))
            return;

        if (skillAddProto.Skills.TryGetValue(args.SkillTree, out var infoProto))
        {
            args.Info.Add(infoProto);
        }
    }

    private void SkillAddOnSkillTreeAdded<T>(Entity<T> entity, ref SkillTreeAdded args) where T : BaseExperienceAddComponent
    {
        if (args.DenyChanges)
            return;

        if (entity.Comp.Skills.TryGetValue(args.SkillTree, out var info))
        {
            args.Info.Add(info);
        }

        if (entity.Comp.SkillAddId is null || !_prototype.TryIndex(entity.Comp.SkillAddId, out var skillAddProto))
            return;

        if (skillAddProto.Skills.TryGetValue(args.SkillTree, out var infoProto))
        {
            args.Info.Add(infoProto);
        }
    }

    private void SublevelAddOnSkillTreeAdded<T>(Entity<T> entity, ref SkillTreeAdded args) where T : BaseSublevelAddComponent
    {
        if (!entity.Comp.Skills.TryGetValue(args.SkillTree, out var sublevels))
            return;

        args.Info.Sublevel += sublevels;
    }

    #endregion

    #region FreeSublevelPoints

    private void AdditionOnSublevelAdditionPointInitialResolve<T>(Entity<T> entity, ref SublevelAdditionPointInitialResolve args) where T : BaseExperienceAddComponent
    {
        if (args.DenyChanges)
            return;

        args.FreeSublevelPoints += entity.Comp.AddSublevelPoints;

        if (entity.Comp.SkillAddId is null || !_prototype.TryIndex(entity.Comp.SkillAddId, out var skillAddProto))
            return;

        args.FreeSublevelPoints += skillAddProto.AddSublevelPoints;
    }

    private void ForceSetAdditionOnSublevelAdditionPointInitialResolve<T>(Entity<T> entity, ref SublevelAdditionPointInitialResolve args) where T : BaseExperienceAddComponent
    {
        if (args.DenyChanges)
            return;

        args.DenyChanges = true;
        args.FreeSublevelPoints = entity.Comp.AddSublevelPoints;

        if (entity.Comp.SkillAddId is null || !_prototype.TryIndex(entity.Comp.SkillAddId, out var skillAddProto))
            return;

        args.FreeSublevelPoints += skillAddProto.AddSublevelPoints;
    }

    private void SublevelAddOnSublevelAdditionPointInitialResolve<T>(Entity<T> entity, ref SublevelAdditionPointInitialResolve args) where T : BaseSublevelAddComponent
    {
        // we dont care about args.DenyChanges because it marks spent points which applied to entity
        args.FreeSublevelPoints -= entity.Comp.SpentSublevelPoints;
    }

    #endregion

    #region Knowledge

    private void KnowledgeForceSetOnKnowledgeInitialResolve<T>(Entity<T> entity, ref KnowledgeInitialResolve args) where T : BaseExperienceAddComponent
    {
        if (args.DenyChanges)
            return;

        args.DenyChanges = true;

        args.Knowledges = entity.Comp.Knowledges;
    }

    private void KnowledgeAddOnKnowledgeInitialResolve<T>(Entity<T> entity, ref KnowledgeInitialResolve args) where T : BaseExperienceAddComponent
    {
        if (args.DenyChanges)
            return;

        if (entity.Comp.SkillAddId is not null && _prototype.TryIndex(entity.Comp.SkillAddId, out var skillAddProto))
            args.Knowledges.UnionWith(skillAddProto.Knowledges);

        args.Knowledges.UnionWith(entity.Comp.Knowledges);
    }

    #endregion
}
