// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Hands;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Mobs;
using Robust.Shared.Containers;

namespace Content.Shared.SS220.StuckOnEquip;

public sealed partial class SharedStuckOnEquipSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StuckOnEquipComponent, ContainerGettingRemovedAttemptEvent>(OnRemoveAttempt);
        SubscribeLocalEvent<StuckOnEquipComponent, GotEquippedEvent>(GotEquipped);
        SubscribeLocalEvent<StuckOnEquipComponent, GotEquippedHandEvent>(GotPickuped);
        SubscribeLocalEvent<MobStateChangedEvent>(OnDeath);
        SubscribeLocalEvent<DropAllStuckOnEquipEvent>(OnRemoveAll);
    }
    private void OnRemoveAttempt(Entity<StuckOnEquipComponent> ent, ref ContainerGettingRemovedAttemptEvent args)
    {
        if (!ent.Comp.IsStuck)
            return;

        args.Cancel();
    }
    private void GotPickuped(Entity<StuckOnEquipComponent> ent, ref GotEquippedHandEvent args)
    {
        if (!ent.Comp.InHandItem)
            return;

        ent.Comp.IsStuck = true;
        Dirty(ent, ent.Comp);
    }

    private void GotEquipped(Entity<StuckOnEquipComponent> ent, ref GotEquippedEvent args)
    {
        if (args.SlotFlags == SlotFlags.POCKET)
            return;

        ent.Comp.IsStuck = true;
        Dirty(ent, ent.Comp);
    }

    private void OnDeath(MobStateChangedEvent ev)
    {
        if (ev.NewMobState == MobState.Dead)
            RemoveItems(ev.Target);
    }

    public void UnstuckItem(Entity<StuckOnEquipComponent> ent)
    {
        ent.Comp.IsStuck = false;
        Dirty(ent);
    }

    private void OnRemoveAll(ref DropAllStuckOnEquipEvent ev)
    {
        var removedItems = RemoveItems(ev.Target);
        ev.DroppedItems.UnionWith(removedItems);
    }

    private HashSet<EntityUid> RemoveItems(EntityUid target)
    {
        HashSet<EntityUid> removedItems = [];
        if (!_inventory.TryGetSlots(target, out var _))
            return removedItems;

        // trying to unequip all item's with component
        foreach (var item in _inventory.GetHandOrInventoryEntities(target))
        {
            if (!TryComp<StuckOnEquipComponent>(item, out var stuckOnEquipComp))
                continue;

            if (!stuckOnEquipComp.ShouldDropOnDeath)
                continue;

            UnstuckItem((item, stuckOnEquipComp));
            _transform.DropNextTo(item, target);
            removedItems.Add(item);
        }

        return removedItems;
    }
}

/// <summary>
///     Raised when we need to remove all StuckOnEquip objects
/// </summary>
[ByRefEvent, Serializable]
public sealed class DropAllStuckOnEquipEvent(EntityUid target, HashSet<EntityUid>? droppedItems = null) : EntityEventArgs
{
    public readonly EntityUid Target = target;

    public HashSet<EntityUid> DroppedItems = droppedItems ?? [];
}
