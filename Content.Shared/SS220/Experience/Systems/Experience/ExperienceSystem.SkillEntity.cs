// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Database;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.Experience.Systems;

public sealed partial class ExperienceSystem : EntitySystem
{
    private readonly EntProtoId _baseSkillProto = "InitSkillEntity";

    [Dependency] private readonly SharedContainerSystem _container = default!;

    private void InitializeSkillEntityEvents()
    {
        SubscribeLocalEvent<ExperienceComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<ExperienceComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ExperienceComponent, ComponentShutdown>(OnShutdown);

    }

    private void OnComponentInit(Entity<ExperienceComponent> entity, ref ComponentInit _)
    {
        entity.Comp.ExperienceContainer = _container.EnsureContainer<ContainerSlot>(entity.Owner, ExperienceComponent.ContainerId);
        entity.Comp.OverrideExperienceContainer = _container.EnsureContainer<ContainerSlot>(entity.Owner, ExperienceComponent.OverrideContainerId);
    }

    private void OnMapInit(Entity<ExperienceComponent> entity, ref MapInitEvent _)
    {
        // entity.Comp.ExperienceContainer = _container.EnsureContainer<ContainerSlot>(entity.Owner, ExperienceComponent.ContainerId);
        // entity.Comp.OverrideExperienceContainer = _container.EnsureContainer<ContainerSlot>(entity.Owner, ExperienceComponent.OverrideContainerId);

        if (entity.Comp.ExperienceContainer.Count != 0 || entity.Comp.OverrideExperienceContainer.Count != 0)
        {
            Log.Warning($"Something was in {ToPrettyString(entity)} experience containers, cleared it");
            PredictedQueueDel(_container.EmptyContainer(entity.Comp.ExperienceContainer).FirstOrNull());
            PredictedQueueDel(_container.EmptyContainer(entity.Comp.OverrideExperienceContainer).FirstOrNull());
        }

        if (!PredictedTrySpawnInContainer(_baseSkillProto, entity, ExperienceComponent.ContainerId, out var skillEntity))
            Log.Fatal($"Cant spawn and insert skill entity into {nameof(entity.Comp.ExperienceContainer)} of {ToPrettyString(entity)}");
        else
            DirtyEntity(skillEntity.Value);

        if (!PredictedTrySpawnInContainer(_baseSkillProto, entity, ExperienceComponent.OverrideContainerId, out var overrideSkillEntity))
            Log.Fatal($"Cant spawn and insert skill entity into {nameof(entity.Comp.OverrideExperienceContainer)} of {ToPrettyString(entity)}");
        else
            DirtyEntity(overrideSkillEntity.Value);


        Dirty(entity);
    }

    private void OnShutdown(Entity<ExperienceComponent> entity, ref ComponentShutdown _)
    {
        QueueDel(_container.EmptyContainer(entity.Comp.ExperienceContainer).FirstOrNull());
        QueueDel(_container.EmptyContainer(entity.Comp.OverrideExperienceContainer).FirstOrNull());
    }

    public bool TryAddSkillToSkillEntity(Entity<ExperienceComponent> entity, string containerId, ProtoId<SkillPrototype> skill)
    {
        if (!_prototype.TryIndex(skill, out var skillPrototype))
            return false;

        return TryAddSkillToSkillEntity(entity, containerId, skillPrototype);
    }

    public bool TryAddSkillToSkillEntity(Entity<ExperienceComponent> entity, string containerId, SkillPrototype skill)
    {
        if (!ValidContainerId(containerId, entity))
            return false;

        var skillEntity = containerId == ExperienceComponent.OverrideContainerId
                            ? entity.Comp.OverrideExperienceContainer.ContainedEntity
                            : entity.Comp.ExperienceContainer.ContainedEntity;

        if (skillEntity is null)
        {
            Log.Error($"Got null skill entity for entity {entity} and container id {containerId}");
            // TODO can I reinit skill entity here?
            return false;
        }

        EntityManager.AddComponents(skillEntity.Value, skill.Components, skill.ApplyIfAlreadyHave);

        return true;
    }

    #region Event relays

    public void RelayEventToSkillEntity<T>() where T : notnull
    {
        SubscribeLocalEvent<SkillComponent, SkillEntityOverrideCheckEvent<T>>(OnOverrideSkillEntityCheck);
        SubscribeLocalEvent<ExperienceComponent, T>(RelayEventToSkillEntity);
    }

    public void AddToAdminLogs<T>(Entity<T> entity, string message, LogImpact logImpact = LogImpact.Low) where T : IComponent
    {
        if (!_container.TryGetOuterContainer(entity, Transform(entity), out var container))
        {
            Log.Error($"Couldn't resolve skill entity owner for entity {ToPrettyString(entity)}");
            return;
        }

        if (!HasComp<ExperienceComponent>(container.Owner))
        {
            Log.Error($"Couldn't resolve {nameof(ExperienceComponent)} on entity {ToPrettyString(container.Owner)} which contains skill entity {ToPrettyString(entity)}");
            return;
        }

        _adminLogManager.Add(LogType.Experience, logImpact, $"{ToPrettyString(container.Owner):user} {message} because of {nameof(T)}");
    }

    private void OnOverrideSkillEntityCheck<T>(Entity<SkillComponent> entity, ref SkillEntityOverrideCheckEvent<T> args) where T : notnull
    {
        args.Subscribed = true;
    }

    private void RelayEventToSkillEntity<T>(Entity<ExperienceComponent> entity, ref T args) where T : notnull
    {
        var overrideSkillEntity = entity.Comp.ExperienceContainer.ContainedEntity;
        var skillEntity = entity.Comp.ExperienceContainer.ContainedEntity;

        if (overrideSkillEntity is null && skillEntity is null)
        {
            Log.Error($"Event {nameof(args)} was skipped because entity {ToPrettyString(entity)} don't have any skill entity");
            return;
        }

        // This check works as assert of not missrelaying
        if ((!TryComp<SkillComponent>(skillEntity, out var comp) && skillEntity is not null)
            || (!TryComp<SkillComponent>(overrideSkillEntity, out var overrideComp) && overrideSkillEntity is not null))
        {
            Log.Error($"Got skill entities not null but without skill component! entity is {ToPrettyString(skillEntity)}, override is {ToPrettyString(overrideSkillEntity)}!");
            return;
        }

        var overrideEv = new SkillEntityOverrideCheckEvent<T>();

        if (overrideSkillEntity is not null)
        {
            RaiseLocalEvent(overrideSkillEntity.Value, ref overrideEv);

            if (overrideEv.Subscribed)
            {
                RaiseLocalEvent(overrideSkillEntity.Value, ref args);
                return;
            }
        }

        if (!overrideEv.Subscribed && skillEntity is not null)
            RaiseLocalEvent(skillEntity.Value, ref args);
    }

    #endregion

    /// <summary>
    /// Easy-to-use-slow-to-compute-method for ensuring components on skill entity
    /// </summary>
    public void EnsureSkill(Entity<ExperienceComponent> entity)
    {
        EnsureSkill(entity, ExperienceComponent.ContainerId);
        EnsureSkill(entity, ExperienceComponent.OverrideContainerId);
    }

    /// <summary>
    /// Do 2 things: <br/>
    /// 1. If <paramref name="proto"/> not null - ensure that we have more or equal skills that provided in proto <br/>
    /// 2. Ensures that skill entity have all needed skills components by implementing all skills ComponentRegistry by tree order
    /// </summary>
    public void EnsureSkill(Entity<ExperienceComponent> entity, string containerId, ProtoId<AddSkillOnInitPrototype>? proto = null)
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
    private void EnsureSkillTree(Entity<ExperienceComponent> entity, string containerId, ProtoId<AddSkillOnInitPrototype> proto)
    {
        if (!_prototype.TryIndex(proto, out var addSkill))
            return;

        var dictRef = containerId == ExperienceComponent.ContainerId ? entity.Comp.Skills : entity.Comp.OverrideSkills;

        foreach (var (key, ensureInfo) in addSkill.Skills)
        {
            if (!dictRef.ContainsKey(key))
                InitExperienceSkillTree(entity, key);

            var currentInfo = dictRef[key];

            if (currentInfo.SkillLevel > ensureInfo.SkillLevel)
                continue;

            if (currentInfo.SkillLevel == ensureInfo.SkillLevel
                && currentInfo.SkillStudied == ensureInfo.SkillStudied)
            {
                currentInfo.SkillSublevel = currentInfo.SkillSublevel > ensureInfo.SkillSublevel ? currentInfo.SkillSublevel : ensureInfo.SkillSublevel;
                continue;
            }

            dictRef[key] = ensureInfo;
        }
    }

    /// <summary>
    /// Adds skills' components to entity. Actually ensures by the way of doing from beginning, can't blame this method it does what it does
    /// </summary>
    private void EnsureSkillEntityComponents(Entity<ExperienceComponent> entity, string containerId)
    {
        var dictView = containerId == ExperienceComponent.ContainerId ? entity.Comp.Skills : entity.Comp.OverrideSkills;

        foreach (var skillTree in dictView.Keys)
        {
            var resultLevel = dictView[skillTree].SkillStudied ? dictView[skillTree].SkillLevel : Math.Max(dictView[skillTree].SkillLevel - 1, 0);

            var skillTreeProto = _prototype.Index(skillTree);

            for (var i = 0; i <= resultLevel; i++)
            {
                if (!TryAddSkillToSkillEntity(entity, containerId, skillTreeProto.SkillTree[i]))
                    Log.Error("Cant add skill to skill entity");
            }
        }
    }
}
