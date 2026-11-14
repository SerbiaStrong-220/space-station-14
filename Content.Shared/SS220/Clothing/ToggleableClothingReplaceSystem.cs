// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Clothing.Components;
using Content.Shared.Inventory;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.Clothing;

public sealed partial class ToggleableClothingReplaceSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ToggleableClothingComponent, ToggleableClothingSlotOccupiedEvent>(OnSlotOccupied);
        SubscribeLocalEvent<ToggleableClothingReplacedComponent, EntInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<ToggleableClothingReplacedComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnSlotOccupied(Entity<ToggleableClothingComponent> ent, ref ToggleableClothingSlotOccupiedEvent args)
    {
        if (args.Handled || ent.Comp.ClothingUid is not { } attached || HasComp<ToggleableClothingReplacedComponent>(ent))
            return;

        if (!_inventory.TryUnequip(args.User, args.Wearer, ent.Comp.Slot, out var removed))
            return;

        var container = _container.EnsureContainer<ContainerSlot>(ent, ToggleableClothingReplacedComponent.ContainerId);
        if (!_container.Insert(removed.Value, container)
            || !_inventory.TryEquip(args.User, args.Wearer, attached, ent.Comp.Slot, triggerHandContact: true))
        {
            _inventory.TryEquip(args.Wearer, removed.Value, ent.Comp.Slot, silent: true, force: true);
            return;
        }

        var replaced = EnsureComp<ToggleableClothingReplacedComponent>(ent);
        replaced.Wearer = args.Wearer;
        Dirty(ent, replaced);
        args.Handled = true;
    }

    private void OnInserted(Entity<ToggleableClothingReplacedComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (_timing.ApplyingState
            || !TryComp<ToggleableClothingComponent>(ent, out var toggleable)
            || args.Container.ID != toggleable.ContainerId)
            return;

        if (TryGetStored(ent, out var stored))
        {
            var wearer = ent.Comp.Wearer;
            if (TerminatingOrDeleted(wearer)
                || _inventory.TryGetSlotEntity(wearer, toggleable.Slot, out _)
                || !_inventory.TryEquip(wearer, stored, toggleable.Slot, silent: true, force: true))
                _transform.DropNextTo(stored, ent.Owner);
        }

        RemCompDeferred<ToggleableClothingReplacedComponent>(ent);
    }

    private void OnShutdown(Entity<ToggleableClothingReplacedComponent> ent, ref ComponentShutdown args)
    {
        if (_timing.ApplyingState || !TryGetStored(ent, out var stored))
            return;

        _transform.DropNextTo(stored, ent.Owner);
    }

    private bool TryGetStored(EntityUid uid, out EntityUid stored)
    {
        stored = default;
        if (!_container.TryGetContainer(uid, ToggleableClothingReplacedComponent.ContainerId, out var container)
            || container.ContainedEntities.Count == 0)
            return false;

        stored = container.ContainedEntities[0];
        return true;
    }
}
