using Content.Shared.Wieldable;

namespace Content.Shared.SS220.Wieldable;

public sealed partial class ComponentsOnWieldSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ComponentsOnWieldComponent, ItemWieldedEvent>((uid, comp, _) => OnWieldToggled((uid, comp), true));
        SubscribeLocalEvent<ComponentsOnWieldComponent, ItemUnwieldedEvent>((uid, comp, _) => OnWieldToggled((uid, comp), false));
    }

    private void OnWieldToggled(Entity<ComponentsOnWieldComponent> entity, bool wielded)
    {
        if (wielded)
            EntityManager.AddComponents(entity.Owner, entity.Comp.Components);
        else
            EntityManager.RemoveComponents(entity.Owner, entity.Comp.RemoveComponents ?? entity.Comp.Components);
    }
}
