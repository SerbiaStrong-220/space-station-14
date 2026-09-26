// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using Content.Shared.Ghost;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.StuckOnEquip;

public sealed partial class SharedStuckOnEquipSystem : EntitySystem
{
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StuckOnEquipComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<StuckOnEquipComponent, ContainerGettingRemovedAttemptEvent>(OnRemoveAttempt);
        SubscribeLocalEvent<StuckOnEquipComponent, EntGotInsertedIntoContainerMessage>(OnInserted);
        SubscribeLocalEvent<StuckOnEquipComponent, EntGotRemovedFromContainerMessage>(OnRemoved);
        SubscribeLocalEvent<MobStateChangedEvent>(OnDeath);
    }

    private void OnStartup(Entity<StuckOnEquipComponent> ent, ref ComponentStartup args)
    {
        RefreshStuck(ent);
    }

    private void OnInserted(Entity<StuckOnEquipComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        RefreshStuck(ent);
    }

    private void OnRemoved(Entity<StuckOnEquipComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        // Forced removals must clear the old lock too. Removal can also reparent into another container.
        RefreshStuck(ent);
    }

    private void RefreshStuck(Entity<StuckOnEquipComponent> ent)
    {
        // Applying server container state must not overwrite the networked lock with a transient local state.
        if (_timing.ApplyingState)
            return;

        SetStuck(ent, ShouldStick(ent));
    }

    private bool ShouldStick(Entity<StuckOnEquipComponent> ent)
    {
        if (!_containers.TryGetContainingContainer((ent.Owner, null, null), out var container))
            return false;

        if (_hands.IsHolding(container.Owner, ent, out _))
            return ent.Comp.InHandItem;

        return _inventory.TryGetSlot(container.Owner, container.ID, out var slot)
            && (slot.SlotFlags & SlotFlags.POCKET) == 0;
    }

    private void OnRemoveAttempt(Entity<StuckOnEquipComponent> ent, ref ContainerGettingRemovedAttemptEvent args)
    {
        if (_timing.ApplyingState || !ent.Comp.IsStuck)
            return;

        // Aghost can manage its own equipment without changing whether the item is stuck.
        if (IsAdminGhost(args.Container.Owner))
            return;

        args.Cancel();
    }

    private bool IsAdminGhost(EntityUid user)
    {
        return TryComp<GhostComponent>(user, out var ghost) && ghost.CanGhostInteract;
    }

    private void SetStuck(Entity<StuckOnEquipComponent> ent, bool stuck)
    {
        if (ent.Comp.IsStuck == stuck)
            return;

        ent.Comp.IsStuck = stuck;
        Dirty(ent);
    }

    public void UnstuckItem(Entity<StuckOnEquipComponent> ent)
    {
        SetStuck(ent, false);
    }

    /// <summary>
    /// Moves an item into its internal storage, restoring its lock if insertion fails.
    /// </summary>
    public bool TryInsertUnstuckItem(Entity<StuckOnEquipComponent?> ent, BaseContainer container)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return false;

        var wasStuck = ent.Comp.IsStuck;
        UnstuckItem((ent.Owner, ent.Comp));
        if (_containers.Insert(ent.Owner, container))
            return true;

        SetStuck((ent.Owner, ent.Comp), wasStuck);
        return false;
    }

    /// <summary>
    /// Allows an acting admin ghost to remove a stuck item through the stripping UI.
    /// Does not bypass other equipment restrictions or grant an exception to an admin's living body.
    /// </summary>
    public bool TryAdminGhostRemove(EntityUid user, EntityUid item)
    {
        if (!IsAdminGhost(user))
            return false;

        if (!TryComp<StuckOnEquipComponent>(item, out var stuck))
            return false;

        if (!stuck.IsStuck)
            return false;

        return TryRemoveItem((item, stuck), user);
    }

    /// <summary>
    /// Releases and removes an equipped item. Restores its lock if removal fails.
    /// Callers must authorize the removal before calling this method.
    /// </summary>
    public bool TryRemoveItem(Entity<StuckOnEquipComponent?> ent, EntityUid user, bool force = false)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return false;

        if (!_containers.TryGetContainingContainer((ent.Owner, null, null), out var container))
            return false;

        var owner = container.Owner;
        var inHand = _hands.IsHolding(owner, ent, out _);
        if (!inHand && !_inventory.TryGetSlot(owner, container.ID, out _))
            return false;

        if (!force && !_inventory.CanAccess(user, owner, ent))
            return false;

        var wasStuck = ent.Comp.IsStuck;
        UnstuckItem((ent.Owner, ent.Comp));

        // Use the inventory API so dependent slots and attached hardsuit helmets are handled correctly.
        var removed = (inHand, force) switch
        {
            (false, _) => _inventory.TryUnequip(
                user,
                owner,
                container.ID,
                silent: true,
                force: force,
                triggerHandContact: true),
            (true, true) => _containers.Remove(ent.Owner, container, force: true),
            (true, false) => _hands.TryDrop(owner, ent.Owner, checkActionBlocker: false),
        };

        if (!removed)
            SetStuck((ent.Owner, ent.Comp), wasStuck);

        return removed;
    }

    private void OnDeath(MobStateChangedEvent ev)
    {
        if (ev.NewMobState == MobState.Dead && !_timing.ApplyingState)
            RemoveAllStuckItemsByDeath(ev.Target);
    }

    public void RemoveAllStuckItems(EntityUid target)
    {
        TryRemoveStuckItems(target);
    }

    public void RemoveAllStuckItemsByDeath(EntityUid target)
    {
        TryRemoveStuckItems(target, onDeath: true);
    }

    public bool TryRemoveStuckItems(EntityUid target)
    {
        return TryRemoveStuckItems(target, onDeath: false);
    }

    private bool TryRemoveStuckItems(EntityUid target, bool onDeath)
    {
        var removed = false;
        // Unequipping may also remove dependent slots, so take a snapshot before modifying the inventory.
        foreach (var item in _inventory.GetHandOrInventoryEntities(target).ToArray())
        {
            if (!TryComp<StuckOnEquipComponent>(item, out var stuck))
                continue;

            if (onDeath && !stuck.ShouldDropOnDeath)
                continue;

            // Keep the cult cleanup behavior: remove matching items even when they were not stuck (e.g. pockets).
            removed |= TryRemoveItem((item, stuck), target, force: true);
        }

        return removed;
    }
}
