using Content.Shared.Actions;
using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Inventory;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Toggleable;
using Robust.Shared.Containers;

namespace Content.Shared.SS220.Clothing;

/// <summary>
/// Handles adding and using a toggle action for <see cref="ToggleClothingComponent"/>.
/// </summary>
public sealed class InnerToggleableSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<InnerToggleableComponent, ComponentInit>(OnInit);
    }

    private void OnInit(Entity<InnerToggleableComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Container = _containerSystem.EnsureContainer<ContainerSlot>(ent, ent.Comp.ContainerId);
    }
}
