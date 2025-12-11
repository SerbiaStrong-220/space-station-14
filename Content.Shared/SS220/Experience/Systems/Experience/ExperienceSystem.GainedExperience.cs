// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.Experience.Systems;

/// <summary>
/// Handles initing experience, exists only because of tons of ways to spawn in yourself
/// </summary>
public sealed partial class ExperienceSystem : EntitySystem
{
    private void InitializeGainedExperience()
    {
        SubscribeLocalEvent<ExperienceComponent, AfterExperienceInitComponentGained>(OnPlayerMobAfterSpawned);

        SubscribeInitComponent<SkillAdminForcedAddComponent>(SkillForceSetOnSkillTreeAdded, KnowledgeForceSetOnKnowledgeInitialResolve, ForceSetAdditionComponentOnSublevelAdditionPointInitialResolve);

        SubscribeInitComponent<SkillRoleAddComponent>(SkillAddOnSkillTreeAdded, KnowledgeAddOnKnowledgeInitialResolve, AdditionComponentOnSublevelAdditionPointInitialResolve);
        SubscribeInitComponent<SkillBackgroundAddComponent>(SkillAddOnSkillTreeAdded, KnowledgeAddOnKnowledgeInitialResolve, AdditionComponentOnSublevelAdditionPointInitialResolve);
    }

    private void SubscribeInitComponent<T>(EntityEventRefHandler<T, SkillTreeAdded> handlerSkill,
                                            EntityEventRefHandler<T, KnowledgeInitialResolve> handlerKnowledge,
                                            EntityEventRefHandler<T, SublevelAdditionPointInitialResolve> handlerAdditionPoint) where T : SkillBaseAddComponent
    {
        SubscribeLocalEvent<T, SkillTreeAdded>(handlerSkill);
        SubscribeLocalEvent<T, KnowledgeInitialResolve>(handlerKnowledge);
        SubscribeLocalEvent<T, SublevelAdditionPointInitialResolve>(handlerAdditionPoint);
    }

    private void OnPlayerMobAfterSpawned(Entity<ExperienceComponent> entity, ref AfterExperienceInitComponentGained args)
    {
        InitializeExperienceComp(entity, args.Type);
    }

    private void InitializeExperienceComp(Entity<ExperienceComponent> entity, InitGainedExperienceType type)
    {
        var byteType = (byte)type;
        // This handles re initing experience if same init event type called again
        var shiftedType = byteType << 1;
        if (shiftedType < entity.Comp.InitMask)
        {
            // prevent release client meta
#if !FULL_RELEASE
            Log.Debug($"Got init event for entity {ToPrettyString(entity)}, event was dropped. Current init mask is {entity.Comp.InitMask}, event init type was {type}");
#endif
            return;
        }

        var treesProto = _prototype.EnumeratePrototypes<SkillTreePrototype>();
        foreach (var treeProto in treesProto)
        {
            if (!treeProto.CanBeShownOnInit)
                continue;

            // Not logging reiniting cause it defined behavior for our case
            InitExperienceSkillTree(entity, treeProto, false);
        }

        entity.Comp.InitMask |= byteType;

        EnsureSkill(entity!);

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

    private void SkillForceSetOnSkillTreeAdded<T>(Entity<T> entity, ref SkillTreeAdded args) where T : SkillBaseAddComponent
    {
        if (args.DenyChanges)
            return;

        args.DenyChanges = true;

        if (entity.Comp.Skills.TryGetValue(args.SkillTree, out var info))
        {
            args.Info.SkillLevel = info.SkillLevel;
            args.Info.SkillSublevel = info.SkillSublevel;
            args.Info.SkillStudied = info.SkillStudied;
        }

        if (entity.Comp.SkillAddId is null || !_prototype.TryIndex(entity.Comp.SkillAddId, out var skillAddProto))
            return;

        if (skillAddProto.Skills.TryGetValue(args.SkillTree, out var infoProto))
        {
            args.Info.SkillLevel += infoProto.SkillLevel;
            args.Info.SkillSublevel += infoProto.SkillSublevel;
            args.Info.SkillStudied &= infoProto.SkillStudied;
        }
    }

    private void SkillAddOnSkillTreeAdded<T>(Entity<T> entity, ref SkillTreeAdded args) where T : SkillBaseAddComponent
    {
        if (args.DenyChanges)
            return;

        if (entity.Comp.Skills.TryGetValue(args.SkillTree, out var info))
        {
            args.Info.SkillLevel += info.SkillLevel;
            args.Info.SkillSublevel += info.SkillSublevel;
            args.Info.SkillStudied &= info.SkillStudied;
        }

        if (entity.Comp.SkillAddId is null || !_prototype.TryIndex(entity.Comp.SkillAddId, out var skillAddProto))
            return;

        if (skillAddProto.Skills.TryGetValue(args.SkillTree, out var infoProto))
        {
            args.Info.SkillLevel += infoProto.SkillLevel;
            args.Info.SkillSublevel += infoProto.SkillSublevel;
            args.Info.SkillStudied &= infoProto.SkillStudied;
        }
    }

    private void AdditionComponentOnSublevelAdditionPointInitialResolve<T>(Entity<T> entity, ref SublevelAdditionPointInitialResolve args) where T : SkillBaseAddComponent
    {
        if (args.DenyChanges)
            return;

        args.FreeSublevelPoints += entity.Comp.AddSublevelPoints;

        if (entity.Comp.SkillAddId is null || !_prototype.TryIndex(entity.Comp.SkillAddId, out var skillAddProto))
            return;

        args.FreeSublevelPoints += skillAddProto.AddSublevelPoints;
    }

    private void ForceSetAdditionComponentOnSublevelAdditionPointInitialResolve<T>(Entity<T> entity, ref SublevelAdditionPointInitialResolve args) where T : SkillBaseAddComponent
    {
        if (args.DenyChanges)
            return;

        args.DenyChanges = true;
        args.FreeSublevelPoints = 0;

        args.FreeSublevelPoints += entity.Comp.AddSublevelPoints;

        if (entity.Comp.SkillAddId is null || !_prototype.TryIndex(entity.Comp.SkillAddId, out var skillAddProto))
            return;

        args.FreeSublevelPoints += skillAddProto.AddSublevelPoints;
    }

    private void KnowledgeForceSetOnKnowledgeInitialResolve<T>(Entity<T> entity, ref KnowledgeInitialResolve args) where T : SkillBaseAddComponent
    {
        if (args.DenyChanges)
            return;

        args.DenyChanges = true;

        args.Knowledges = entity.Comp.Knowledges;
    }

    private void KnowledgeAddOnKnowledgeInitialResolve<T>(Entity<T> entity, ref KnowledgeInitialResolve args) where T : SkillBaseAddComponent
    {
        if (args.DenyChanges)
            return;

        if (entity.Comp.SkillAddId is not null && _prototype.TryIndex(entity.Comp.SkillAddId, out var skillAddProto))
            args.Knowledges.UnionWith(skillAddProto.Knowledges);

        args.Knowledges.UnionWith(entity.Comp.Knowledges);
    }
}
