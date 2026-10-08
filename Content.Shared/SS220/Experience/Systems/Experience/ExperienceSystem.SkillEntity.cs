// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.Experience.Systems;

public sealed partial class ExperienceSystem
{
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private INetManager _net = default!;

    private HashSet<Type> _subscribedToExperienceComponentTypes = [];

    private readonly EntProtoId _baseSKillPrototype = "InitSkillEntity";

    private void InitializeSkillEntityEvents()
    {
        SubscribeLocalEvent<ExperienceComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<ExperienceComponent, ComponentRemove>(OnRemove);
    }

    private void OnComponentInit(Entity<ExperienceComponent> entity, ref ComponentInit _)
    {
        entity.Comp.SkillEntityContainer = _container.EnsureContainer<ContainerSlot>(entity.Owner, ExperienceComponent.ContainerId);
    }

    private void OnMapInitSkillEntity(Entity<ExperienceComponent> entity, ref MapInitEvent _)
    {
        if (entity.Comp.SkillEntityContainer.Count != 0)
        {
            Log.Warning($"Something was in {ToPrettyString(entity)} experience containers, cleared it");
            PredictedQueueDel(_container.EmptyContainer(entity.Comp.SkillEntityContainer).FirstOrNull());
        }

        if (!PredictedTrySpawnInContainer(_baseSKillPrototype, entity, ExperienceComponent.ContainerId, out var skillEntity))
            Log.Fatal($"Cant spawn and insert skill entity into {nameof(entity.Comp.SkillEntityContainer)} of {ToPrettyString(entity)}");
        else
            DirtyEntity(skillEntity.Value);

        entity.Comp.SkillEntityInitialized = true;
        Dirty(entity);
    }

    private void OnRemove(Entity<ExperienceComponent> entity, ref ComponentRemove _)
    {
        QueueDel(_container.EmptyContainer(entity.Comp.SkillEntityContainer).FirstOrNull());
    }

    public bool TryAddSkillToSkillEntity(Entity<ExperienceComponent> entity, [ForbidLiteral] ProtoId<SkillPrototype> skill)
    {
        if (!_prototype.Resolve(skill, out var skillPrototype))
            return false;

        return TryAddSkillToSkillEntity(entity, skillPrototype);
    }

    public bool TryAddSkillToSkillEntity(Entity<ExperienceComponent> entity, SkillPrototype skill)
    {
        var skillEntity = entity.Comp.SkillEntityContainer.ContainedEntity;
        if (skillEntity is null)
        {
            Log.Error($"Got null skill entity for entity {ToPrettyString(entity)}");
            return false;
        }

        EntityManager.RemoveComponents(skillEntity.Value, skill.RemoveComponents);
        EntityManager.AddComponents(skillEntity.Value, skill.Components, skill.ApplyIfAlreadyHave);

        return true;
    }

    public void RemoveSkillFromSkillEntity(Entity<ExperienceComponent> entity, ProtoId<SkillPrototype> skillId)
    {
        if (!_prototype.Resolve(skillId, out var skillPrototype))
            return;

        RemoveSkillFromSkillEntity(entity, skillPrototype);
    }

    public void RemoveSkillFromSkillEntity(Entity<ExperienceComponent> entity, SkillPrototype skillProto)
    {
        var skillEntity = entity.Comp.SkillEntityContainer.ContainedEntity;

        if (skillEntity is null)
        {
            Log.Error($"Got null skill entity for entity {ToPrettyString(entity)}");
            return;
        }

        EntityManager.RemoveComponents(skillEntity.Value, skillProto.Components);
    }

    private void ClearAllSkillComponents(Entity<ExperienceComponent> entity)
    {
        var oldSkillEntity = entity.Comp.SkillEntityContainer?.ContainedEntity;

        Del(oldSkillEntity);
        if (oldSkillEntity is null)
            return;

        if (!PredictedTrySpawnInContainer(_baseSKillPrototype, entity, ExperienceComponent.ContainerId, out var skillEntity))
            Log.Fatal($"Cant respawn and insert skill entity into {nameof(entity.Comp.SkillEntityContainer)} of {ToPrettyString(entity)}");
        else
            DirtyEntity(skillEntity.Value);
    }

    #region Event relays

    public void RelayEventToSkillEntity<TComp, TEvent>() where TEvent : notnull where TComp : Component
    {
        if (_subscribedToExperienceComponentTypes.Add(typeof(TEvent)))
            SubscribeLocalEvent<ExperienceComponent, TEvent>(RelayEventToSkillEntity);
    }

    private void RelayEventToSkillEntity<T>(Entity<ExperienceComponent> entity, ref T args) where T : notnull
    {
        if (!entity.Comp.SkillEntityInitialized || MetaData(entity).EntityLifeStage >= EntityLifeStage.Terminating)
            return;

        // Client sometime needs time to figure out pvs containers
        if (entity.Comp.SkillEntityContainer is null)
        {
            DebugTools.AssertEqual(_net.IsServer, false);
            return;
        }

        if (entity.Comp.SkillEntityContainer.ContainedEntity is not { Valid: true } skillEntity)
            return;

        // This check works as assert of not missrelaying
        if (!HasComp<SkillComponent>(skillEntity))
        {
            Log.Error($"Got skill entities not null but without skill component! entity is {ToPrettyString(skillEntity)}!");
            return;
        }

        RaiseLocalEvent(skillEntity, ref args);
    }

    #endregion

    /// <summary>
    /// Easy-to-use-slow-to-compute-method for ensuring components on skill entity
    /// </summary>
    public void EnsureSkillEffectApplied(Entity<ExperienceComponent?> entity)
    {
        if (!Resolve(entity.Owner, ref entity.Comp, logMissing: false))
            return;

        EnsureSkill(entity!, ExperienceComponent.ContainerId);
    }

    /// <summary>
    /// Method to bypass possible collection override in update loop
    /// </summary>
    private void EnsureSkill(EntityUid uid)
    {
        if (!TryComp<ExperienceComponent>(uid, out var comp))
            return;

        EnsureSkill((uid, comp), ExperienceComponent.ContainerId);
    }

    /// <summary>
    /// Do 2 things: <br/>
    /// 1. If <paramref name="proto"/> not null - ensure that we have more or equal skills that provided in proto <br/>
    /// 2. Ensures that skill entity have all needed skills components by implementing all skills ComponentRegistry by tree order
    /// </summary>
    public void EnsureSkill(Entity<ExperienceComponent> entity, ProtoId<ExperienceDefinitionPrototype>? proto = null)
    {
        if (proto is not null)
            EnsureSkillTree(entity, proto.Value);

        EnsureSkillEntityComponents(entity);
    }

    /// <summary>
    /// Ensures that current entity have skills more or equal to that in provided proto
    /// </summary>
    private void EnsureSkillTree(Entity<ExperienceComponent> entity, [ForbidLiteral] ProtoId<ExperienceDefinitionPrototype> proto)
    {
        if (!_prototype.TryIndex(proto, out var addSkill))
            return;

        var dictRef = entity.Comp.Skills;

        foreach (var (key, ensureInfo) in addSkill.Skills)
        {
            if (!dictRef.ContainsKey(key))
                InitExperienceSkillTree(entity, key);

            var currentInfo = dictRef[key];

            if (currentInfo.Level > ensureInfo.Level)
                continue;

            if (currentInfo.Level == ensureInfo.Level)
            {
                currentInfo.Sublevel = Math.Max(currentInfo.Sublevel, ensureInfo.Sublevel);
                continue;
            }

            dictRef[key] = ensureInfo;
        }
    }

    /// <summary>
    /// From clear start adds skills' components to entity so use cautious.
    /// Actually ensures by the way of doing from beginning, can't blame this method it does what it does
    /// </summary>
    private void EnsureSkillEntityComponents(Entity<ExperienceComponent> entity)
    {
        var dictView = GetResultSkills(entity);

        if (dictView.Count == 0)
            ClearAllSkillComponents(entity);

        foreach (var skillTree in dictView.Keys)
        {
            var skillTreeProto = _prototype.Index(skillTree);

            foreach (var skillId in skillTreeProto.SkillTree)
            {
                RemoveSkillFromSkillEntity(entity, skillId);
            }

            for (var i = 0; i <= dictView[skillTree].SkillTreeIndex; i++)
            {
                if (!TryAddSkillToSkillEntity(entity, skillTreeProto.SkillTree[i]))
                    Log.Warning("Cant add skill to skill entity");
            }
        }
    }
}
