// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Experience.Systems;

public sealed partial class ExperienceSystem : EntitySystem
{
    private void InitializeGainedExperience()
    {
        SubscribeLocalEvent<ExperienceComponent, RecalculateEntityExperience>(OnRecalculateEntityExperience);

        SubscribeLocalEvent<ExperienceAdditionComponent, SublevelAdditionPointInitialResolve>(ExperienceAdditionOnSublevelAdditionPointInitialResolve);
        SubscribeLocalEvent<ExperienceAdditionComponent, KnowledgeInitialResolve>(ExperienceAdditionOnKnowledgeInitialResolve);

        SubscribeLocalEvent<ExperienceAdditionComponent, SkillTreeAdded>(ExperienceAdditionOnSkillTreeAdded);

        SubscribeSublevelAddComponentToInit<JobBackgroundSublevelAddComponent>(SublevelAddOnSkillTreeAdded, SublevelAddOnSublevelAdditionPointInitialResolve);
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

    private void ExperienceAdditionOnSkillTreeAdded(Entity<ExperienceAdditionComponent> entity, ref SkillTreeAdded args)
    {
        if (args.DenyChanges)
            return;

        if (entity.Comp.Forced)
        {
            args.DenyChanges = true;
            args.Info = new SkillTreeInfo()
            {
                Level = ExperienceSystem.StartSkillLevel,
                Sublevel = ExperienceSystem.StartSublevel,
            };
        }

        foreach (var (source, specifier) in entity.Comp.Sources)
        {
            if (entity.Comp.Forced && !ExperienceAdditionComponent.AdminForceWhitelistSources.Contains(source))
                continue;

            if (specifier.Skills.TryGetValue(args.SkillTree, out var info))
            {
                args.Info.Add(info);
            }
        }

        if (entity.Comp.Forced)
            return;

        if (entity.Comp.DefinitionId is null || !_prototype.Resolve(entity.Comp.DefinitionId, out var skillAddProto))
            return;

        if (skillAddProto.Skills.TryGetValue(args.SkillTree, out var infoProto))
        {
            args.Info.Add(infoProto);
        }
    }

    private void ExperienceAdditionOnSublevelAdditionPointInitialResolve(Entity<ExperienceAdditionComponent> entity, ref SublevelAdditionPointInitialResolve args)
    {
        if (args.DenyChanges)
            return;

        if (entity.Comp.Forced)
        {
            args.DenyChanges = true;
            args.FreeSublevelPoints = 0;
        }

        foreach (var (source, specifier) in entity.Comp.Sources)
        {
            if (entity.Comp.Forced && !ExperienceAdditionComponent.AdminForceWhitelistSources.Contains(source))
                continue;

            args.FreeSublevelPoints += specifier.FreeSublevelPoints;
        }

        if (entity.Comp.Forced)
            return;

        if (entity.Comp.DefinitionId is null || !_prototype.Resolve(entity.Comp.DefinitionId, out var experienceDefinition))
            return;

        args.FreeSublevelPoints += experienceDefinition.AddSublevelPoints;
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
        DirtyField(entity!, nameof(ExperienceComponent.FreeSublevelPoints));

        var ev = new KnowledgeInitialResolve([]);
        RaiseLocalEvent(entity, ref ev);

        entity.Comp.ConstantKnowledge.Clear();
        entity.Comp.ResolvedKnowledge.Clear();

        foreach (var knowledge in ev.Knowledges)
        {
            if (!TryAddKnowledge(entity!, knowledge))
                Log.Error($"Cant add knowledge {knowledge} to {ToPrettyString(entity)}");
        }

        DirtyFields(entity!, null, [nameof(ExperienceComponent.ConstantKnowledge), nameof(ExperienceComponent.ResolvedKnowledge)]);
    }

    private void SublevelAddOnSkillTreeAdded<T>(Entity<T> entity, ref SkillTreeAdded args) where T : BaseSublevelAddComponent
    {
        if (args.DenyChanges)
            return;

        if (!entity.Comp.Skills.TryGetValue(args.SkillTree, out var sublevels))
            return;

        args.Info.Sublevel += sublevels;
    }

    private void SublevelAddOnSublevelAdditionPointInitialResolve<T>(Entity<T> entity, ref SublevelAdditionPointInitialResolve args) where T : BaseSublevelAddComponent
    {
        // we dont care about args.DenyChanges because it marks spent points which applied to entity
        args.FreeSublevelPoints -= entity.Comp.SpentSublevelPoints;
    }

    private void ExperienceAdditionOnKnowledgeInitialResolve(Entity<ExperienceAdditionComponent> entity, ref KnowledgeInitialResolve args)
    {
        if (args.DenyChanges)
            return;

        if (entity.Comp.Forced)
            args.DenyChanges = true;

        var tmp = new HashSet<ProtoId<KnowledgePrototype>>(4);
        foreach (var (source, specifier) in entity.Comp.Sources)
        {
            if (entity.Comp.Forced && !ExperienceAdditionComponent.AdminForceWhitelistSources.Contains(source))
                continue;

            tmp.UnionWith(specifier.Knowledges);
        }

        args.Knowledges.UnionWith(tmp);

        if (entity.Comp.Forced)
            return;

        if (entity.Comp.DefinitionId is null || !_prototype.Resolve(entity.Comp.DefinitionId, out var knowledgeAddProto))
            return;

        args.Knowledges.UnionWith(knowledgeAddProto.Knowledges);
    }
}
