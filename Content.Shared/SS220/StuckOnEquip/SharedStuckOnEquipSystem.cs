// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Mobs;
using Content.Shared.Wieldable;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.StuckOnEquip;

public sealed partial class SharedStuckOnEquipSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StuckOnEquipComponent, GotEquippedEvent>(GotEquipped);
        SubscribeLocalEvent<StuckOnEquipComponent, GotEquippedHandEvent>(GotPickuped);
        SubscribeLocalEvent<MobStateChangedEvent>(OnDeath);
        SubscribeLocalEvent<DropAllStuckOnEquipEvent>(OnRemoveAll);
    }

    private void OnDeath(MobStateChangedEvent ev)
    {
        if (ev.NewMobState == MobState.Dead)
            RemoveItems(ev.Target);
    }

    private void OnRemoveAll(ref DropAllStuckOnEquipEvent ev)
    {
        RemoveItems(ev.Target);
    }

    private void GotPickuped(Entity<StuckOnEquipComponent> entity, ref GotEquippedHandEvent args)
    {
        if (_gameTiming.ApplyingState)
            return;

        if (!entity.Comp.InHandItem)
            return;

        EnsureComp<UnremoveableComponent>(entity, out var comp);
        comp.DeleteOnDrop = false;
    }

    private void GotEquipped(Entity<StuckOnEquipComponent> entity, ref GotEquippedEvent args)
    {
        if (_gameTiming.ApplyingState)
            return;

        if (args.SlotFlags == SlotFlags.POCKET)
            return; // we don't want to make unremovable pocket item

        EnsureComp<UnremoveableComponent>(entity, out var comp);
        comp.DeleteOnDrop = false;
    }

    private void RemoveItems(EntityUid target)
    {
        if (!_inventory.TryGetSlots(target, out var slots))
            return;

        // trying to unequip all item's with component
        foreach (var slot in _inventory.GetHandOrInventoryEntities(target))
        {
            if (!TryComp<StuckOnEquipComponent>(slot, out var stuckOnEquipComponent))
                continue;

            if (!stuckOnEquipComponent.ShouldDropOnDeath)
                continue;

            RemComp<UnremoveableComponent>(slot);
            _transform.DropNextTo(slot, target);
        }
    }
}

/// <summary>
///     Raised when we need to remove all StuckOnEquip objects
/// </summary>
[ByRefEvent, Serializable]
public sealed class DropAllStuckOnEquipEvent : EntityEventArgs
{
    public readonly EntityUid Target;

    public DropAllStuckOnEquipEvent(EntityUid target)
    {
        Target = target;
    }
}
