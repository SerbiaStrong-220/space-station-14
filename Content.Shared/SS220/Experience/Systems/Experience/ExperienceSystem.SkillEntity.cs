// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Database;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.Experience.Systems;

public sealed partial class ExperienceSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;

    private void OnComponentInit(Entity<ExperienceComponent> entity, ref ComponentInit _)
    {
        entity.Comp.ExperienceContainer = _container.EnsureContainer<ContainerSlot>(entity.Owner, ExperienceComponent.ContainerId);
        entity.Comp.OverrideExperienceContainer = _container.EnsureContainer<ContainerSlot>(entity.Owner, ExperienceComponent.OverrideContainerId);

        if (entity.Comp.ExperienceContainer.Count != 0 || entity.Comp.OverrideExperienceContainer.Count != 0)
        {
            Log.Error($"Something was in {ToPrettyString(entity)} experience containers, cleared it");
            QueueDel(_container.EmptyContainer(entity.Comp.ExperienceContainer).FirstOrNull());
            QueueDel(_container.EmptyContainer(entity.Comp.OverrideExperienceContainer).FirstOrNull());
        }
    }

    private void OnShutdown(Entity<ExperienceComponent> entity, ref ComponentShutdown _)
    {
        QueueDel(_container.EmptyContainer(entity.Comp.ExperienceContainer).FirstOrNull());
        QueueDel(_container.EmptyContainer(entity.Comp.OverrideExperienceContainer).FirstOrNull());
    }

    public bool TryAddSkillToSkillEntity(Entity<ExperienceComponent> entity, string containerId, SkillPrototype skill)
    {
        if (!ContainerIds.Contains(containerId))
        {
            Log.Error($"Tried to add skill {skill.ID} to entity {ToPrettyString(entity)} but skill entity container was incorrect, provided value {containerId}");
            return false;
        }

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

    public void RelayEventToSkillEntity<T>() where T : notnull
    {
        SubscribeLocalEvent<SkillComponent, SkillEntityOverrideCheckEvent<T>>(OnOverrideSkillEntityCheck);
        SubscribeLocalEvent<ExperienceComponent, T>(RelayEventToSkillEntity);
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
            Log.Error($"Event {nameof(T)} was skipped because entity {ToPrettyString(entity)} don't have any skill entity");
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
            if (overrideEv.Subscribed)
            {
                RaiseLocalEvent(overrideSkillEntity.Value, ref args);
                return;
            }
        }

        if (!overrideEv.Subscribed && skillEntity is not null)
            RaiseLocalEvent(skillEntity.Value, ref args);
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
}
