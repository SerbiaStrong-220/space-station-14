// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Experience.Systems;

/// <summary>
/// Handles initing experience, exists only because of tons of ways to spawn in yourself
/// </summary>
public sealed partial class ExperienceSystem : EntitySystem
{
    private void InitializeGainedExperience()
    {
        SubscribeLocalEvent<ExperienceComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ExperienceComponent, AfterExperienceInitComponentGained>(OnPlayerMobAfterSpawned);

        // TODO if more this event like will be added make generic <T> version to bypass subscribe errors
        SubscribeLocalEvent<SkillRoleAddComponent, SkillTreeAddedEvent>(SkillAddOnSkillTreeAdded);
    }

    private void OnPlayerMobAfterSpawned(Entity<ExperienceComponent> entity, ref AfterExperienceInitComponentGained args)
    {
        InitializeExperienceComp(entity, args.Type);
    }

    private void OnStartup(Entity<ExperienceComponent> entity, ref ComponentStartup _)
    {
        InitializeExperienceComp(entity, InitGainedExperienceType.ComponentInit);
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

            // Not logging reiniting cause it defined behavior for out case
            InitExperienceSkillTree(entity, treeProto, false);
        }

        entity.Comp.InitMask |= (byte)byteType;

        EnsureSkill(entity);
    }

    private void SkillAddOnSkillTreeAdded(Entity<SkillRoleAddComponent> entity, ref SkillTreeAddedEvent args)
    {
        if (_prototype.TryIndex(entity.Comp.SkillAddId, out var skillAddProto)
            && skillAddProto.Skills.TryGetValue(args.SkillTree, out var infoProto))
        {
            args.Info.SkillLevel += infoProto.SkillLevel;
            args.Info.SkillSublevel += infoProto.SkillSublevel;
            args.Info.SkillStudied &= infoProto.SkillStudied;
        }

        if (entity.Comp.Skills.TryGetValue(args.SkillTree, out var info))
        {
            args.Info.SkillLevel += info.SkillLevel;
            args.Info.SkillSublevel += info.SkillSublevel;
            args.Info.SkillStudied &= info.SkillStudied;
        }
    }
}
