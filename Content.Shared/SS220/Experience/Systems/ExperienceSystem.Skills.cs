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

}
