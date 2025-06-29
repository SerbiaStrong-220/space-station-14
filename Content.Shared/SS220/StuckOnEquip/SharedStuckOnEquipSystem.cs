// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Administration.Managers;
using Content.Shared.Hands;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Mobs;
using Robust.Shared.Containers;

namespace Content.Shared.SS220.StuckOnEquip;

public sealed partial class SharedStuckOnEquipSystem : EntitySystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ISharedAdminManager _adminManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StuckOnEquipComponent, ContainerGettingRemovedAttemptEvent>(OnRemoveAttempt);
        SubscribeLocalEvent<StuckOnEquipComponent, GotEquippedEvent>(GotEquipped);
        SubscribeLocalEvent<StuckOnEquipComponent, GotEquippedHandEvent>(GotPickuped);
        SubscribeLocalEvent<MobStateChangedEvent>(OnDeath);
    }
    private void OnRemoveAttempt(Entity<StuckOnEquipComponent> ent, ref ContainerGettingRemovedAttemptEvent args)
    {
        if (!ent.Comp.IsStuck)
            return;

        args.Cancel();
    }
    private void GotPickuped(Entity<StuckOnEquipComponent> ent, ref GotEquippedHandEvent args)
    {
        if (_adminManager.IsAdmin(args.User))
            return;

        if (!ent.Comp.InHandItem)
            return;

        ent.Comp.IsStuck = true;
        Dirty(ent, ent.Comp);
    }

    private void GotEquipped(Entity<StuckOnEquipComponent> ent, ref GotEquippedEvent args)
    {
        if (_adminManager.IsAdmin(args.Equipee))
            return;

        if (args.SlotFlags == SlotFlags.POCKET)
            return;

        ent.Comp.IsStuck = true;
        Dirty(ent, ent.Comp);
    }

    private void OnDeath(MobStateChangedEvent ev)
    {
        if (ev.NewMobState == MobState.Dead)
            RemoveAllStuckItemsByDeath(ev.Target);
    }

    public void UnstuckItem(Entity<StuckOnEquipComponent> ent)
    {
        ent.Comp.IsStuck = false;
        Dirty(ent);
    }

    public void RemoveAllStuckItems(EntityUid target)
    {
        if (!_inventory.TryGetSlots(target, out var _))
            return;

        foreach (var item in _inventory.GetHandOrInventoryEntities(target))
        {
            if (!TryComp<StuckOnEquipComponent>(item, out var stuckOnEquipComp))
                continue;

            UnstuckItem((item, stuckOnEquipComp));
            _transform.DropNextTo(item, target);
        }
    }

    public void RemoveAllStuckItemsByDeath(EntityUid target)
    {
        if (!_inventory.TryGetSlots(target, out var _))
            return;

        foreach (var item in _inventory.GetHandOrInventoryEntities(target))
        {
            if (!TryComp<StuckOnEquipComponent>(item, out var stuckOnEquipComp))
                continue;

            if (!stuckOnEquipComp.ShouldDropOnDeath)
                continue;

            UnstuckItem((item, stuckOnEquipComp));
            _transform.DropNextTo(item, target);
        }
    }

    public bool TryRemoveStuckItems(EntityUid target)
    {
        if (!_inventory.TryGetSlots(target, out var _))
            return false;

        bool isRemoved = false;

        foreach (var item in _inventory.GetHandOrInventoryEntities(target))
        {
            if (!TryComp<StuckOnEquipComponent>(item, out var stuckOnEquipComp))
                continue;

            UnstuckItem((item, stuckOnEquipComp));
            _transform.DropNextTo(item, target);
            isRemoved = true;
        }

        return isRemoved;
    }
}
