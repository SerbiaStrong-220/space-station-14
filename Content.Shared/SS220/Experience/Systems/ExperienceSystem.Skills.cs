// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.Experience.Systems;

public sealed partial class ExperienceSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public void OnComponentInit(Entity<ExperienceComponent> entity, ref ComponentInit _)
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

    public void OnShutdown(Entity<ExperienceComponent> entity, ref ComponentShutdown _)
    {
        QueueDel(_container.EmptyContainer(entity.Comp.ExperienceContainer).FirstOrNull());
        QueueDel(_container.EmptyContainer(entity.Comp.OverrideExperienceContainer).FirstOrNull());
    }

    public bool TryAddSkillToSkillEntity(Entity<ExperienceComponent> entity, string containerId, SkillPrototype skill)
    {
        // TODO
        return true;


        // var mat = (PhysicalCompositionComponent) compositionReg.Component;

        // foreach (var (name, data) in comps)
        // {
        //     if (HasComp(target, data.Component.GetType()))
        //         continue;

        //     var component = (Component)Factory.GetComponent(name);
        //     var temp = (object)component;
        //     _seriMan.CopyTo(data.Component, ref temp);
        //     AddComp(target, (Component)temp!);
        // }
    }

    public void SubscribeSkillEntityToEvent<T>() where T : EntityEventArgs
    {
        SubscribeLocalEvent<ExperienceComponent, T>(RelayEventToSkillEntity);
    }

    private void RelayEventToSkillEntity<T>(Entity<ExperienceComponent> entity, ref T args) where T : EntityEventArgs
    {
        var overrideSkillEntity = entity.Comp.ExperienceContainer.ContainedEntity;
        var skillEntity = entity.Comp.ExperienceContainer.ContainedEntity;

        if (skillEntity is null && overrideSkillEntity is null)
            return;

        // This check works as safe from missrelaying
        if ((!TryComp<SkillComponent>(skillEntity, out var comp) && skillEntity is not null)
            || (!TryComp<SkillComponent>(overrideSkillEntity, out var overrideComp) && overrideSkillEntity is not null))
        {
            Log.Error($"Got skill entities not null but without skill component! entity is {ToPrettyString(skillEntity)}, override is {ToPrettyString(overrideSkillEntity)}!");
            return;
        }

        if (skillEntity is not null)
            RaiseLocalEvent(skillEntity.Value, ref args);

        if (overrideSkillEntity is not null)
            RaiseLocalEvent(overrideSkillEntity.Value, ref args);
    }
}
