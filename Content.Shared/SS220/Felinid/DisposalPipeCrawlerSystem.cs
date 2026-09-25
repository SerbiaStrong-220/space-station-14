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

public sealed partial class DisposalPipeCrawlerSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<DisposalPipeCrawlerComponent, MapInitEvent>(OnPipecrawlMapInit);
        SubscribeLocalEvent<DisposalPipeCrawlerActionComponent, ActionPerformedEvent>(OnPipecrawlActionPerformed);
        SubscribeLocalEvent<DisposalPipeCrawlerComponent, AfterAutoHandleStateEvent>(OnPipecrawlHandleState);
        SubscribeLocalEvent<DisposalPipeCrawlerComponent, ComponentShutdown>(OnPipecrawlShutdown);
        SubscribeLocalEvent<DisposalPipeCrawlerComponent, EntGotInsertedIntoContainerMessage>(OnPipecrawlContainerModified);
        SubscribeLocalEvent<DisposalPipeCrawlerComponent, EntGotRemovedFromContainerMessage>(OnPipecrawlContainerModified);
        SubscribeLocalEvent<DisposalPipeCrawlerComponent, UpdateCanMoveEvent>(OnPipecrawlCanMove);
        SubscribeLocalEvent<DisposalPipeCrawlerComponent, InteractionAttemptEvent>(OnPipecrawlInteractAttempt);
        SubscribeLocalEvent<DisposalPipeCrawlerComponent, AccessibleOverrideEvent>(OnPipecrawlAccessibleOverride);
        SubscribeLocalEvent<DisposalPipeCrawlerComponent, InRangeOverrideEvent>(OnPipecrawlInRangeOverride);
        SubscribeLocalEvent<DisposalPipeCrawlerComponent, UseAttemptEvent>(OnPipecrawlUseAttempt);
        SubscribeLocalEvent<DisposalPipeCrawlerComponent, AttackAttemptEvent>(OnPipecrawlAttackAttempt);
        SubscribeLocalEvent<DisposalPipeCrawlerComponent, PickupAttemptEvent>(OnPipecrawlPickupAttempt);
        SubscribeLocalEvent<DisposalPipeCrawlerComponent, DropAttemptEvent>(OnPipecrawlDropAttempt);
        SubscribeLocalEvent<DisposalPipeCrawlerComponent, EmoteAttemptEvent>(OnPipecrawlEmoteAttempt);
        SubscribeLocalEvent<DisposalPipeCrawlerComponent, IsUnequippingAttemptEvent>(OnPipecrawlUnequipAttempt);
        SubscribeLocalEvent<DisposalPipeCrawlerComponent, IsUnequippingTargetAttemptEvent>(OnPipecrawlUnequipTargetAttempt);
        SubscribeLocalEvent<DisposalPipeCrawlerComponent, DidUnequipEvent>(OnPipecrawlUnequipped);
        SubscribeLocalEvent<DisposalPipeCrawlerComponent, StorageInsertHeldItemAttemptEvent>(OnPipecrawlStorageInsertAttempt);
        SubscribeLocalEvent<DisposalPipeCrawlerUnequippedItemComponent, EntGotInsertedIntoContainerMessage>(OnUnequippedItemInserted);
        SubscribeLocalEvent<DisposalPipeCrawlerComponent, CanSeeAttemptEvent>(OnPipecrawlCanSeeAttempt);
        SubscribeLocalEvent<DisposalPipeCrawlerComponent, GetVisMaskEvent>(OnPipecrawlGetVisMask);
    }

    private void OnPipecrawlMapInit(Entity<DisposalPipeCrawlerComponent> ent, ref MapInitEvent args)
    {
        RefreshPipecrawlState(ent);
    }

    private void OnPipecrawlActionPerformed(
        Entity<DisposalPipeCrawlerActionComponent> ent,
        ref ActionPerformedEvent args)
    {
        if (TryComp<DisposalPipeCrawlerComponent>(args.Performer, out var pipecrawl))
            SyncPipecrawlAction((args.Performer, pipecrawl));
    }

    private void OnPipecrawlHandleState(Entity<DisposalPipeCrawlerComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        RefreshPipecrawlState(ent);
    }

    private void OnPipecrawlShutdown(Entity<DisposalPipeCrawlerComponent> ent, ref ComponentShutdown args)
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

    private void OnPipecrawlContainerModified(Entity<DisposalPipeCrawlerComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        RefreshPipecrawlState(ent);
    }

    private void OnPipecrawlContainerModified(Entity<DisposalPipeCrawlerComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        RefreshPipecrawlState(ent);
    }

    private void SyncPipecrawlAction(Entity<DisposalPipeCrawlerComponent> ent)
    {
        if (ent.Comp.InsidePipe || IsInDisposableUnit(ent.Owner))
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
        Entity<DisposalPipeCrawlerComponent> ent,
        Entity<ActionComponent?> action)
    {
        if (action.Comp == null)
            return;

        if (ent.Comp.InsidePipe)
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

    public void RefreshPipecrawlState(Entity<DisposalPipeCrawlerComponent> ent)
    {
        if (!ent.Comp.InsidePipe)
            ClearUnequippedItemMarkers(ent.Owner);

        SyncPipecrawlAction(ent);
        _blocker.UpdateCanMove(ent.Owner);
        RaisePipecrawlVisualsChanged(ent.Owner, ent.Comp.InsidePipe);
    }

    private void RaisePipecrawlVisualsChanged(EntityUid uid, bool active)
    {
        var visuals = new DisposalPipeCrawlerVisualsChangedEvent(active);
        RaiseLocalEvent(uid, ref visuals);
    }

    private void OnPipecrawlCanMove(Entity<DisposalPipeCrawlerComponent> ent, ref UpdateCanMoveEvent args)
    {
        if (ent.Comp.InsidePipe)
            args.Cancel();
    }

    private void OnPipecrawlInteractAttempt(Entity<DisposalPipeCrawlerComponent> ent, ref InteractionAttemptEvent args)
    {
        if (!ent.Comp.InsidePipe ||
            args.Target == null ||
            IsOwnedByCrawler(args.Target.Value, ent.Owner) ||
            args.Target == ent.Comp.CurrentTube)
            return;

        args.Cancelled = true;
    }

    private void OnPipecrawlUseAttempt(Entity<DisposalPipeCrawlerComponent> ent, ref UseAttemptEvent args)
    {
        if (ent.Comp.InsidePipe && !IsOwnedByCrawler(args.Used, ent.Owner))
            args.Cancel();
    }

    private void OnPipecrawlAccessibleOverride(
        Entity<DisposalPipeCrawlerComponent> ent,
        ref AccessibleOverrideEvent args)
    {
        if (!ent.Comp.InsidePipe ||
            args.User != ent.Owner ||
            !IsOwnedByCrawler(args.Target, ent.Owner) && args.Target != ent.Comp.CurrentTube)
            return;

        args.Handled = true;
        args.Accessible = true;
    }

    private void OnPipecrawlInRangeOverride(
        Entity<DisposalPipeCrawlerComponent> ent,
        ref InRangeOverrideEvent args)
    {
        if (!ent.Comp.InsidePipe ||
            args.User != ent.Owner ||
            !IsOwnedByCrawler(args.Target, ent.Owner) && args.Target != ent.Comp.CurrentTube)
            return;

        args.Handled = true;
        args.InRange = true;
    }

    private void OnPipecrawlPickupAttempt(Entity<DisposalPipeCrawlerComponent> ent, ref PickupAttemptEvent args)
    {
        if (!ent.Comp.InsidePipe ||
            IsOwnedByCrawler(args.Item, ent.Owner) ||
            TryComp<DisposalPipeCrawlerUnequippedItemComponent>(args.Item, out var marker) &&
            marker.Pipecrawler == ent.Owner)
        {
            return;
        }

        args.Cancel();
    }

    private void OnPipecrawlAttackAttempt(Entity<DisposalPipeCrawlerComponent> ent, ref AttackAttemptEvent args)
    {
        if (ent.Comp.InsidePipe && args.Target != ent.Comp.CurrentTube)
            args.Cancel();
    }

    private void OnPipecrawlDropAttempt(Entity<DisposalPipeCrawlerComponent> ent, ref DropAttemptEvent args)
    {
        if (ent.Comp.InsidePipe || HasComp<DisposalPipeCrawlerContentsComponent>(ent.Owner))
            args.Cancel();
    }

    private void OnPipecrawlEmoteAttempt(Entity<DisposalPipeCrawlerComponent> ent, ref EmoteAttemptEvent args)
    {
        if (ent.Comp.InsidePipe)
            args.Cancel();
    }

    private void OnPipecrawlCanSeeAttempt(Entity<DisposalPipeCrawlerComponent> ent, ref CanSeeAttemptEvent args)
    {
        if (ent.Comp.InsidePipe)
            args.Cancel();
    }

    private void OnPipecrawlGetVisMask(Entity<DisposalPipeCrawlerComponent> ent, ref GetVisMaskEvent args)
    {
        if (ent.Comp.InsidePipe)
            args.VisibilityMask |= (int) VisibilityFlags.Subfloor;
    }

    private bool IsInDisposableUnit(EntityUid uid)
    {
        return _container.TryGetContainingContainer(uid, out var container) &&
               container.ID == DisposalUnitComponent.ContainerId &&
               HasComp<DisposalUnitComponent>(container.Owner);
    }

    private void OnPipecrawlUnequipAttempt(
        Entity<DisposalPipeCrawlerComponent> ent,
        ref IsUnequippingAttemptEvent args)
    {
        if (!ent.Comp.InsidePipe)
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
        Entity<DisposalPipeCrawlerComponent> ent,
        ref IsUnequippingTargetAttemptEvent args)
    {
        if (!ent.Comp.InsidePipe || args.User == ent.Owner && args.UnEquipTarget == ent.Owner)
            return;

        args.Reason = "felinid-pipecrawl-unequip-external";
        args.Cancel();
    }

    private void OnPipecrawlUnequipped(Entity<DisposalPipeCrawlerComponent> ent, ref DidUnequipEvent args)
    {
        if (!ent.Comp.InsidePipe ||
            args.EquipTarget != ent.Owner ||
            TerminatingOrDeleted(ent.Owner) ||
            TerminatingOrDeleted(args.Equipment))
        {
            return;
        }

        var marker = EnsureComp<DisposalPipeCrawlerUnequippedItemComponent>(args.Equipment);
        marker.Pipecrawler = ent.Owner;
    }

    private void OnUnequippedItemInserted(
        Entity<DisposalPipeCrawlerUnequippedItemComponent> ent,
        ref EntGotInsertedIntoContainerMessage args)
    {
        RemCompDeferred<DisposalPipeCrawlerUnequippedItemComponent>(ent.Owner);
    }

    private void OnPipecrawlStorageInsertAttempt(
        Entity<DisposalPipeCrawlerComponent> ent,
        ref StorageInsertHeldItemAttemptEvent args)
    {
        if (!ent.Comp.InsidePipe ||
            !IsOwnedByCrawler(args.Storage, ent.Owner) ||
            !IsOwnedByCrawler(args.Item, ent.Owner))
        {
            return;
        }

        args.BypassDropActionBlocker = true;
    }

    private bool IsOwnedByCrawler(EntityUid entity, EntityUid crawler)
    {
        if (entity == crawler)
            return true;

        if (!Exists(entity))
            return false;

        foreach (var container in _container.GetContainingContainers(entity))
        {
            if (container.Owner == crawler)
                return true;
        }

        return false;
    }

    private void ClearUnequippedItemMarkers(EntityUid owner)
    {
        var query = EntityQueryEnumerator<DisposalPipeCrawlerUnequippedItemComponent>();
        while (query.MoveNext(out var uid, out var marker))
        {
            if (marker.Pipecrawler == owner)
                RemCompDeferred<DisposalPipeCrawlerUnequippedItemComponent>(uid);
        }
    }
}
