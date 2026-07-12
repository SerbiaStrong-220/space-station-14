using System.Numerics;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Actions.Events;
using Content.Shared.ActionBlocker;
using Content.Shared.Disposal.Components;
using Content.Shared.Emoting;
using Content.Shared.Eye;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.SS220.Felinid.Components;
using Content.Shared.Storage.Events;
using Content.Shared.Tag;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.Felinid;

public sealed class FelinidPipecrawlSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<FelinidPipecrawlComponent, MapInitEvent>(OnPipecrawlMapInit);
        SubscribeLocalEvent<FelinidPipecrawlActionComponent, ActionPerformedEvent>(OnPipecrawlActionPerformed);
        SubscribeLocalEvent<FelinidPipecrawlComponent, AfterAutoHandleStateEvent>(OnPipecrawlHandleState);
        SubscribeLocalEvent<FelinidPipecrawlComponent, ComponentShutdown>(OnPipecrawlShutdown);
        SubscribeLocalEvent<FelinidPipecrawlComponent, EntGotInsertedIntoContainerMessage>(OnPipecrawlContainerModified);
        SubscribeLocalEvent<FelinidPipecrawlComponent, EntGotRemovedFromContainerMessage>(OnPipecrawlContainerModified);
        SubscribeLocalEvent<FelinidPipecrawlComponent, UpdateCanMoveEvent>(OnPipecrawlCanMove);
        SubscribeLocalEvent<FelinidPipecrawlComponent, InteractionAttemptEvent>(OnPipecrawlInteractAttempt);
        SubscribeLocalEvent<FelinidPipecrawlComponent, AccessibleOverrideEvent>(OnPipecrawlAccessibleOverride);
        SubscribeLocalEvent<FelinidPipecrawlComponent, InRangeOverrideEvent>(OnPipecrawlInRangeOverride);
        SubscribeLocalEvent<FelinidPipecrawlComponent, UseAttemptEvent>(OnPipecrawlUseAttempt);
        SubscribeLocalEvent<FelinidPipecrawlComponent, AttackAttemptEvent>(OnPipecrawlAttempt);
        SubscribeLocalEvent<FelinidPipecrawlComponent, PickupAttemptEvent>(OnPipecrawlPickupAttempt);
        SubscribeLocalEvent<FelinidPipecrawlComponent, DropAttemptEvent>(OnPipecrawlAttempt);
        SubscribeLocalEvent<FelinidPipecrawlComponent, EmoteAttemptEvent>(OnPipecrawlAttempt);
        SubscribeLocalEvent<FelinidPipecrawlComponent, IsUnequippingAttemptEvent>(OnPipecrawlUnequipAttempt);
        SubscribeLocalEvent<FelinidPipecrawlComponent, IsUnequippingTargetAttemptEvent>(OnPipecrawlUnequipTargetAttempt);
        SubscribeLocalEvent<FelinidPipecrawlComponent, DidUnequipEvent>(OnPipecrawlUnequipped);
        SubscribeLocalEvent<FelinidPipecrawlComponent, StorageInsertHeldItemAttemptEvent>(OnPipecrawlStorageInsertAttempt);
        SubscribeLocalEvent<FelinidPipecrawlUnequippedItemComponent, EntGotInsertedIntoContainerMessage>(OnUnequippedItemInserted);
        SubscribeLocalEvent<FelinidPipecrawlComponent, CanSeeAttemptEvent>(OnPipecrawlCanSeeAttempt);
        SubscribeLocalEvent<FelinidPipecrawlComponent, GetVisMaskEvent>(OnPipecrawlGetVisMask);
    }

    private void OnPipecrawlMapInit(Entity<FelinidPipecrawlComponent> ent, ref MapInitEvent args)
    {
        RefreshPipecrawlState(ent);
    }

    private void OnPipecrawlActionPerformed(
        Entity<FelinidPipecrawlActionComponent> ent,
        ref ActionPerformedEvent args)
    {
        if (TryComp<FelinidPipecrawlComponent>(args.Performer, out var pipecrawl))
            SyncPipecrawlAction((args.Performer, pipecrawl));
    }

    private void OnPipecrawlHandleState(Entity<FelinidPipecrawlComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        RefreshPipecrawlState(ent);
    }

    private void OnPipecrawlShutdown(Entity<FelinidPipecrawlComponent> ent, ref ComponentShutdown args)
    {
        if (Terminating(ent.Owner))
            return;

        if (ent.Comp.ActionEntity is { } action &&
            TryComp<ActionComponent>(action, out var actionComp))
        {
            _actions.RemoveAction((action, actionComp));
        }

        ent.Comp.ActionEntity = null;
        ClearUnequippedItemMarkers(ent.Owner);
        RaisePipecrawlVisualsChanged(ent.Owner, false);
        _blocker.UpdateCanMove(ent.Owner);
    }

    private void OnPipecrawlContainerModified(Entity<FelinidPipecrawlComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        RefreshPipecrawlState(ent);
    }

    private void OnPipecrawlContainerModified(Entity<FelinidPipecrawlComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        RefreshPipecrawlState(ent);
    }

    private void SyncPipecrawlAction(Entity<FelinidPipecrawlComponent> ent)
    {
        if (ent.Comp.Active || IsInDisposableUnit(ent.Owner))
        {
            if (ent.Comp.ActionEntity is { } existingAction &&
                TryComp<ActionComponent>(existingAction, out var existingActionComp) &&
                existingActionComp.AttachedEntity == ent.Owner)
            {
                SyncPipecrawlCooldown(ent, (existingAction, existingActionComp));
                return;
            }

            _actions.AddAction(ent.Owner, ref ent.Comp.ActionEntity, ent.Comp.Action);

            if (ent.Comp.ActionEntity is { } addedAction &&
                TryComp<ActionComponent>(addedAction, out var addedActionComp))
            {
                SyncPipecrawlCooldown(ent, (addedAction, addedActionComp));
            }

            return;
        }

        if (ent.Comp.ActionEntity is not { } action ||
            !TryComp<ActionComponent>(action, out var actionComp) ||
            actionComp.AttachedEntity != ent.Owner)
        {
            return;
        }

        _actions.RemoveAction((action, actionComp));
        ent.Comp.ActionEntity = null;
        Dirty(ent);
    }

    private void SyncPipecrawlCooldown(
        Entity<FelinidPipecrawlComponent> ent,
        Entity<ActionComponent?> action)
    {
        if (action.Comp == null)
            return;

        if (ent.Comp.Active)
        {
            if (action.Comp.Cooldown != null)
                _actions.RemoveCooldown(action);

            return;
        }

        if (ent.Comp.NextEntryAllowed > _timing.CurTime)
        {
            _actions.SetCooldown(action, ent.Comp.CooldownStartedAt, ent.Comp.NextEntryAllowed);
            return;
        }

        if (action.Comp.Cooldown != null)
            _actions.RemoveCooldown(action);
    }

    public void RefreshPipecrawlState(Entity<FelinidPipecrawlComponent> ent)
    {
        if (!ent.Comp.Active)
            ClearUnequippedItemMarkers(ent.Owner);

        SyncPipecrawlAction(ent);
        _blocker.UpdateCanMove(ent.Owner);
        RaisePipecrawlVisualsChanged(ent.Owner, ent.Comp.Active);
    }

    private void RaisePipecrawlVisualsChanged(EntityUid uid, bool active)
    {
        var visuals = new FelinidPipecrawlVisualsChangedEvent(active);
        RaiseLocalEvent(uid, ref visuals);
    }

    private void OnPipecrawlCanMove(Entity<FelinidPipecrawlComponent> ent, ref UpdateCanMoveEvent args)
    {
        if (ent.Comp.Active)
            args.Cancel();
    }

    private void OnPipecrawlInteractAttempt(Entity<FelinidPipecrawlComponent> ent, ref InteractionAttemptEvent args)
    {
        if (!ent.Comp.Active || args.Target == null || IsOwnedBy(args.Target.Value, ent.Owner))
            return;

        args.Cancelled = true;
    }

    private void OnPipecrawlUseAttempt(Entity<FelinidPipecrawlComponent> ent, ref UseAttemptEvent args)
    {
        if (ent.Comp.Active && !IsOwnedBy(args.Used, ent.Owner))
            args.Cancel();
    }

    private void OnPipecrawlAccessibleOverride(
        Entity<FelinidPipecrawlComponent> ent,
        ref AccessibleOverrideEvent args)
    {
        if (!ent.Comp.Active || args.User != ent.Owner || !IsOwnedBy(args.Target, ent.Owner))
            return;

        args.Handled = true;
        args.Accessible = true;
    }

    private void OnPipecrawlInRangeOverride(
        Entity<FelinidPipecrawlComponent> ent,
        ref InRangeOverrideEvent args)
    {
        if (!ent.Comp.Active || args.User != ent.Owner || !IsOwnedBy(args.Target, ent.Owner))
            return;

        args.Handled = true;
        args.InRange = true;
    }

    private void OnPipecrawlPickupAttempt(Entity<FelinidPipecrawlComponent> ent, ref PickupAttemptEvent args)
    {
        if (!ent.Comp.Active ||
            IsOwnedBy(args.Item, ent.Owner) ||
            TryComp<FelinidPipecrawlUnequippedItemComponent>(args.Item, out var marker) &&
            marker.Pipecrawler == ent.Owner)
        {
            return;
        }

        args.Cancel();
    }

    private void OnPipecrawlAttempt<T>(Entity<FelinidPipecrawlComponent> ent, ref T args) where T : CancellableEntityEventArgs
    {
        if (ent.Comp.Active)
            args.Cancel();
    }

    private void OnPipecrawlCanSeeAttempt(Entity<FelinidPipecrawlComponent> ent, ref CanSeeAttemptEvent args)
    {
        if (ent.Comp.Active)
            args.Cancel();
    }

    private void OnPipecrawlGetVisMask(Entity<FelinidPipecrawlComponent> ent, ref GetVisMaskEvent args)
    {
        if (ent.Comp.Active)
            args.VisibilityMask |= (int) VisibilityFlags.Subfloor;
    }

    private bool IsInDisposableUnit(EntityUid uid)
    {
        return _container.TryGetContainingContainer((uid, null, null), out var container) &&
               container.ID == DisposalUnitComponent.ContainerId &&
               HasComp<FelinidPipecrawlEntryComponent>(container.Owner);
    }

    private void OnPipecrawlUnequipAttempt(
        Entity<FelinidPipecrawlComponent> ent,
        ref IsUnequippingAttemptEvent args)
    {
        if (!ent.Comp.Active)
            return;

        if (args.User != ent.Owner || args.UnEquipTarget != ent.Owner)
        {
            args.Reason = "felinid-pipecrawl-unequip-external";
            args.Cancel();
            return;
        }

        if (!_hands.TryGetEmptyHand((ent.Owner, null), out _))
        {
            args.Reason = "felinid-pipecrawl-unequip-hands-full";
            args.Cancel();
            return;
        }

        if (!TryComp<InventoryComponent>(ent.Owner, out var inventory))
            return;

        foreach (var slot in inventory.Slots)
        {
            if (slot.DependsOn != args.Slot ||
                !_inventory.TryGetSlotEntity(ent.Owner, slot.Name, out _, inventory))
            {
                continue;
            }

            args.Reason = "felinid-pipecrawl-unequip-dependent";
            args.Cancel();
            return;
        }
    }

    private void OnPipecrawlUnequipTargetAttempt(
        Entity<FelinidPipecrawlComponent> ent,
        ref IsUnequippingTargetAttemptEvent args)
    {
        if (!ent.Comp.Active || args.User == ent.Owner && args.UnEquipTarget == ent.Owner)
            return;

        args.Reason = "felinid-pipecrawl-unequip-external";
        args.Cancel();
    }

    private void OnPipecrawlUnequipped(Entity<FelinidPipecrawlComponent> ent, ref DidUnequipEvent args)
    {
        if (!ent.Comp.Active ||
            args.EquipTarget != ent.Owner ||
            TerminatingOrDeleted(ent.Owner) ||
            TerminatingOrDeleted(args.Equipment))
        {
            return;
        }

        var marker = EnsureComp<FelinidPipecrawlUnequippedItemComponent>(args.Equipment);
        marker.Pipecrawler = ent.Owner;
    }

    private void OnUnequippedItemInserted(
        Entity<FelinidPipecrawlUnequippedItemComponent> ent,
        ref EntGotInsertedIntoContainerMessage args)
    {
        RemCompDeferred<FelinidPipecrawlUnequippedItemComponent>(ent.Owner);
    }

    private void OnPipecrawlStorageInsertAttempt(
        Entity<FelinidPipecrawlComponent> ent,
        ref StorageInsertHeldItemAttemptEvent args)
    {
        if (!ent.Comp.Active ||
            !IsOwnedBy(args.Storage, ent.Owner) ||
            !IsOwnedBy(args.Item, ent.Owner))
        {
            return;
        }

        args.BypassDropActionBlocker = true;
    }

    private bool IsOwnedBy(EntityUid entity, EntityUid owner)
    {
        if (entity == owner)
            return true;

        if (!Exists(entity))
            return false;

        foreach (var container in _container.GetContainingContainers((entity, null)))
        {
            if (container.Owner == owner)
                return true;
        }

        return false;
    }

    private void ClearUnequippedItemMarkers(EntityUid owner)
    {
        var query = EntityQueryEnumerator<FelinidPipecrawlUnequippedItemComponent>();
        while (query.MoveNext(out var uid, out var marker))
        {
            if (marker.Pipecrawler == owner)
                RemCompDeferred<FelinidPipecrawlUnequippedItemComponent>(uid);
        }
    }

}
