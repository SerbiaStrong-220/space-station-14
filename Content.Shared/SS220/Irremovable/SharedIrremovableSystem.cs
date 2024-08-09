// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Mobs;

namespace Content.Shared.SS220.Irremovable;

public sealed partial class SharedIrremovableSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedHandsSystem _hand = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<IrremovableComponent, GotEquippedEvent>(GotEquipped);
        SubscribeLocalEvent<IrremovableComponent, GotEquippedHandEvent>(GotPickuped);
        SubscribeLocalEvent<MobStateChangedEvent>(OnDeath);
    }

    private void OnDeath(MobStateChangedEvent ev)
    {
        if (ev.NewMobState != MobState.Dead)
            return;

        if (!_inventory.TryGetSlots(ev.Target, out var slots))
            return;
        // trying to unequip all item's with component
        foreach (var slot in slots)
        {
            if (!_inventory.TryGetSlotEntity(ev.Target, slot.Name, out var entity))
                continue;

            if (!TryComp<IrremovableComponent>(entity, out var irremovableComp))
                continue;

            if (irremovableComp.ShouldDropOnDeath)
            {
                if (irremovableComp.InHandItem)
                    _hand.TryDrop(ev.Target); // trying to drop inhand item (that's sucks i know)

                _inventory.TryUnequip(ev.Target, slot.Name, true, true);
            }
        }
    }

    private void GotPickuped(Entity<IrremovableComponent> entity, ref GotEquippedHandEvent args)
    {
        if (!entity.Comp.InHandItem)
            return;

        EnsureComp<UnremoveableComponent>(entity, out var comp);
        comp.DeleteOnDrop = false;
    }

    private void GotEquipped(Entity<IrremovableComponent> entity, ref GotEquippedEvent args)
    {
        if (args.SlotFlags == SlotFlags.POCKET)
            return; // we don't want to make unremovable pocket item

        EnsureComp<UnremoveableComponent>(entity, out var comp);
        comp.DeleteOnDrop = false;
    }
}
