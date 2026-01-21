// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.Experience.Systems;

public sealed partial class ExperienceSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly INetManager _net = default!;

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
        entity.Comp.OverrideSkillEntityContainer = _container.EnsureContainer<ContainerSlot>(entity.Owner, ExperienceComponent.OverrideContainerId);
    }

    private void OnMapInitSkillEntity(Entity<ExperienceComponent> entity, ref MapInitEvent _)
    {
        if (entity.Comp.SkillEntityContainer.Count != 0 || entity.Comp.OverrideSkillEntityContainer.Count != 0)
        {
            Log.Warning($"Something was in {ToPrettyString(entity)} experience containers, cleared it");
            PredictedQueueDel(_container.EmptyContainer(entity.Comp.SkillEntityContainer).FirstOrNull());
            PredictedQueueDel(_container.EmptyContainer(entity.Comp.OverrideSkillEntityContainer).FirstOrNull());
        }

        if (!PredictedTrySpawnInContainer(_baseSKillPrototype, entity, ExperienceComponent.ContainerId, out var skillEntity))
            Log.Fatal($"Cant spawn and insert skill entity into {nameof(entity.Comp.SkillEntityContainer)} of {ToPrettyString(entity)}");
        else
            DirtyEntity(skillEntity.Value);

        if (!PredictedTrySpawnInContainer(_baseSKillPrototype, entity, ExperienceComponent.OverrideContainerId, out var overrideSkillEntity))
            Log.Fatal($"Cant spawn and insert skill entity into {nameof(entity.Comp.OverrideSkillEntityContainer)} of {ToPrettyString(entity)}");
        else
            DirtyEntity(overrideSkillEntity.Value);

        entity.Comp.SkillEntityInitialized = true;
        Dirty(entity);
    }

    private void OnRemove(Entity<ExperienceComponent> entity, ref ComponentRemove _)
    {
        QueueDel(_container.EmptyContainer(entity.Comp.SkillEntityContainer).FirstOrNull());
        QueueDel(_container.EmptyContainer(entity.Comp.OverrideSkillEntityContainer).FirstOrNull());
    }

    public bool TryAddSkillToSkillEntity(Entity<ExperienceComponent> entity, [ForbidLiteral] string containerId, [ForbidLiteral] ProtoId<SkillPrototype> skill)
    {
        if (!_prototype.Resolve(skill, out var skillPrototype))
            return false;

        return TryAddSkillToSkillEntity(entity, containerId, skillPrototype);
    }

    public bool TryAddSkillToSkillEntity(Entity<ExperienceComponent> entity, [ForbidLiteral] string containerId, SkillPrototype skill)
    {
        if (!ValidContainerId(containerId, entity))
            return false;

        var skillEntity = containerId == ExperienceComponent.OverrideContainerId
                            ? entity.Comp.OverrideSkillEntityContainer.ContainedEntity
                            : entity.Comp.SkillEntityContainer.ContainedEntity;

        if (skillEntity is null)
        {
            Log.Error($"Got null skill entity for entity {ToPrettyString(entity)} and container id {containerId}");
            return false;
        }

        EntityManager.RemoveComponents(skillEntity.Value, skill.RemoveComponents);
        EntityManager.AddComponents(skillEntity.Value, skill.Components, skill.ApplyIfAlreadyHave);

        return true;
    }

    public void RemoveSkillFromSkillEntity(Entity<ExperienceComponent> entity, [ForbidLiteral] string containerId, ProtoId<SkillPrototype> skillId)
    {
        if (!_prototype.Resolve(skillId, out var skillPrototype))
            return;

        RemoveSkillFromSkillEntity(entity, containerId, skillPrototype);
    }

    public void RemoveSkillFromSkillEntity(Entity<ExperienceComponent> entity, [ForbidLiteral] string containerId, SkillPrototype skillProto)
    {
        if (!ValidContainerId(containerId, entity))
            return;

        var skillEntity = containerId == ExperienceComponent.OverrideContainerId
                            ? entity.Comp.OverrideSkillEntityContainer.ContainedEntity
                            : entity.Comp.SkillEntityContainer.ContainedEntity;

        if (skillEntity is null)
        {
            Log.Error($"Got null skill entity for entity {ToPrettyString(entity)} and container id {containerId}");
            return;
        }

        EntityManager.RemoveComponents(skillEntity.Value, skillProto.Components);
    }

    private void ClearAllSkillComponents(Entity<ExperienceComponent> entity, [ForbidLiteral] string containerId)
    {
        if (!ValidContainerId(containerId, entity))
            return;

        var skillContainer = containerId == ExperienceComponent.OverrideContainerId
                            ? entity.Comp.OverrideSkillEntityContainer
                            : entity.Comp.SkillEntityContainer;

        var oldSkillEntity = skillContainer?.ContainedEntity;

        EntityManager.DeleteEntity(oldSkillEntity);
        if (oldSkillEntity is null)
            return;

        if (!PredictedTrySpawnInContainer(_baseSKillPrototype, entity, containerId, out var skillEntity))
            Log.Fatal($"Cant respawn and insert skill entity into {nameof(entity.Comp.SkillEntityContainer)} of {ToPrettyString(entity)}");
        else
            DirtyEntity(skillEntity.Value);
    }

    #region Event relays

    public void RelayEventToSkillEntity<TComp, TEvent>() where TEvent : notnull where TComp : Component
    {
        if (_subscribedToExperienceComponentTypes.Add(typeof(TEvent)))
            SubscribeLocalEvent<ExperienceComponent, TEvent>(RelayEventToSkillEntity);

        // TODO-thinking: override check event bad if we want to handle do after overrides and actually bad in most cases
        // for current realization this is dead end code, so no one use this checks for real
        // Also we might want not to subscribe some event but made collection which cares of events
        // in core of this hides one more problem - two different component subscribing to one event
        // 1. we can ban this 2. ???
        SubscribeLocalEvent<TComp, SkillEntityOverrideCheckEvent<TEvent>>(OnOverrideSkillEntityCheck);
    }

    private void OnOverrideSkillEntityCheck<TComp, TEvent>(Entity<TComp> entity, ref SkillEntityOverrideCheckEvent<TEvent> args) where TEvent : notnull where TComp : Component
    {
        args.Subscribed = true;
    }

    private void RelayEventToSkillEntity<T>(Entity<ExperienceComponent> entity, ref T args) where T : notnull
    {
        if (!entity.Comp.SkillEntityInitialized || MetaData(entity).EntityLifeStage >= EntityLifeStage.Terminating)
            return;

        // Client sometime need time to figure out pvs containers
        if (entity.Comp.OverrideSkillEntityContainer is null || entity.Comp.SkillEntityContainer is null)
        {
            DebugTools.AssertEqual(_net.IsServer, false);
            return;
        }

        var overrideSkillEntity = entity.Comp.OverrideSkillEntityContainer.ContainedEntity;
        var skillEntity = entity.Comp.SkillEntityContainer.ContainedEntity;

        if (skillEntity is null || overrideSkillEntity is null)
            return;

        // This check works as assert of not missrelaying
        if ((!TryComp<SkillComponent>(skillEntity, out var comp) && skillEntity is not null)
            || (!TryComp<SkillComponent>(overrideSkillEntity, out var overrideComp) && overrideSkillEntity is not null))
        {
            Log.Error($"Got skill entities not null but without skill component! entity is {ToPrettyString(skillEntity)}, override is {ToPrettyString(overrideSkillEntity)}!");
            return;
        }

        var overrideEntityEv = new SkillEntityOverrideCheckEvent<T>();

        if (overrideSkillEntity is not null)
            RaiseLocalEvent(overrideSkillEntity.Value, ref overrideEntityEv);

        var targetEntity = overrideEntityEv.Subscribed ? overrideSkillEntity : skillEntity;

        if (targetEntity is not null)
            RaiseLocalEvent(targetEntity.Value, ref args);

    }

    #endregion

    /// <summary>
    /// Easy-to-use-slow-to-compute-method for ensuring components on skill entity
    /// </summary>
    public void EnsureSkillEffectApplied(Entity<ExperienceComponent?> entity)
    {
        if (!Resolve(entity.Owner, ref entity.Comp, logMissing: false))
            return;

        // TODO this seem can work without mapinit of entity so I need to add cache and resolve in update func;
        EnsureSkill(entity!, ExperienceComponent.ContainerId);
        EnsureSkill(entity!, ExperienceComponent.OverrideContainerId);
    }

    /// <summary>
    /// Method to bypass possible collection override in update loop
    /// </summary>
    private void EnsureSkill(EntityUid uid)
    {
        if (!TryComp<ExperienceComponent>(uid, out var comp))
            return;

        EnsureSkill((uid, comp), ExperienceComponent.ContainerId);
        EnsureSkill((uid, comp), ExperienceComponent.OverrideContainerId);
    }

    /// <summary>
    /// Do 2 things: <br/>
    /// 1. If <paramref name="proto"/> not null - ensure that we have more or equal skills that provided in proto <br/>
    /// 2. Ensures that skill entity have all needed skills components by implementing all skills ComponentRegistry by tree order
    /// </summary>
    public void EnsureSkill(Entity<ExperienceComponent> entity, [ForbidLiteral] string containerId, ProtoId<ExperienceDefinitionPrototype>? proto = null)
    {
        if (!ValidContainerId(containerId, entity))
            return;

        if (proto is not null)
            EnsureSkillTree(entity, containerId, proto.Value);

        EnsureSkillEntityComponents(entity, containerId);
    }

    /// <summary>
    /// Ensures that current entity have skills more or equal to that in provided proto
    /// </summary>
    private void EnsureSkillTree(Entity<ExperienceComponent> entity, [ForbidLiteral] string containerId, [ForbidLiteral] ProtoId<ExperienceDefinitionPrototype> proto)
    {
        if (!_prototype.TryIndex(proto, out var addSkill))
            return;

        var dictRef = containerId == ExperienceComponent.ContainerId ? entity.Comp.Skills : entity.Comp.OverrideSkills;

        foreach (var (key, ensureInfo) in addSkill.Skills)
        {
            if (!dictRef.ContainsKey(key))
                InitExperienceSkillTree(entity, key);

            var currentInfo = dictRef[key];

            if (currentInfo.Level > ensureInfo.Level)
                continue;

            if (currentInfo.Level == ensureInfo.Level)
            {
                currentInfo.Sublevel = currentInfo.Sublevel > ensureInfo.Sublevel ? currentInfo.Sublevel : ensureInfo.Sublevel;
                continue;
            }

            dictRef[key] = ensureInfo;
        }
    }

    /// <summary>
    /// Adds skills' components to entity. Actually ensures by the way of doing from beginning, can't blame this method it does what it does
    /// </summary>
    private void EnsureSkillEntityComponents(Entity<ExperienceComponent> entity, [ForbidLiteral] string containerId)
    {
        var dictView = containerId == ExperienceComponent.ContainerId ? entity.Comp.Skills : entity.Comp.OverrideSkills;

        if (dictView.Count == 0)
        {
            ClearAllSkillComponents(entity, containerId);
        }

        foreach (var skillTree in dictView.Keys)
        {
            var skillTreeProto = _prototype.Index(skillTree);

            foreach (var skillId in skillTreeProto.SkillTree)
            {
                RemoveSkillFromSkillEntity(entity, containerId, skillId);
            }

            for (var i = 0; i <= dictView[skillTree].SkillTreeIndex; i++)
            {
                if (!TryAddSkillToSkillEntity(entity, containerId, skillTreeProto.SkillTree[i]))
                    Log.Warning("Cant add skill to skill entity");
            }
        }
    }
}
