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
        SubscribeLocalEvent<InnerToggleableComponent, ToggleClothingEvent>(OnToggleItem);
    }

    private void OnInit(Entity<InnerToggleableComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Container = _containerSystem.EnsureContainer<ContainerSlot>(ent, ent.Comp.ContainerId);
    }

    private void OnToggleItem(Entity<InnerToggleableComponent> ent, ref ToggleClothingEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        ToggleItem(args.Performer, uid, ent.Comp);
    }

    private void ToggleItem(EntityUid user, EntityUid target, InnerToggleableComponent comp)
    {
        if (comp.Container == null || comp.ClothingUid == null)
            return;

        var parent = Transform(target).ParentUid;
        if (comp.Container.ContainedEntity == null)
            _inventorySystem.TryUnequip(user, parent, component.Slot, force: true);
        else if (_inventorySystem.TryGetSlotEntity(parent, component.Slot, out var existing))
        {
            _popupSystem.PopupClient(Loc.GetString("toggleable-clothing-remove-first", ("entity", existing)),
                user, user);
        }
        else
            _inventorySystem.TryEquip(user, parent, comp.ClothingUid.Value, comp.Slot);
    }
}
