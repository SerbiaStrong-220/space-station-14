using System.Diagnostics.CodeAnalysis;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Disposal.Components;
using Content.Shared.Emoting;
using Content.Shared.Eye;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Item;
using Content.Shared.Movement.Events;
using Content.Shared.SS220.Felinid.Components;
using Content.Shared.Storage.Events;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared.SS220.Felinid;

public abstract partial class SharedDisposalPipeCrawlerSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private ActionBlockerSystem _blocker = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<DisposalPipeCrawlerComponent, MapInitEvent>(OnPipecrawlMapInit);
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
        SubscribeLocalEvent<DisposalPipeCrawlerComponent, DidEquipHandEvent>(OnPipecrawlEquippedHand);
        SubscribeLocalEvent<DisposalPipeCrawlerComponent, CanSeeAttemptEvent>(OnPipecrawlCanSeeAttempt);
        SubscribeLocalEvent<DisposalPipeCrawlerComponent, GetVisMaskEvent>(OnPipecrawlGetVisMask);
    }

    private void OnPipecrawlMapInit(Entity<DisposalPipeCrawlerComponent> ent, ref MapInitEvent args)
    {
        RefreshPipecrawlState(ent);
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
        var available = ent.Comp.InsidePipe || TryGetContainingDisposalUnit(ent.Owner, out _);

        if (TryComp<ActionComponent>(ent.Comp.ActionEntity, out var action) &&
            action.AttachedEntity == ent.Owner)
        {
            if (available)
            {
                SyncPipecrawlCooldown(ent, (ent.Comp.ActionEntity.Value, action));
                return;
            }

            _actions.RemoveAction((ent.Comp.ActionEntity.Value, action));
            ent.Comp.ActionEntity = null;
            Dirty(ent);
            return;
        }

        if (!available)
            return;

        _actions.AddAction(ent.Owner, ref ent.Comp.ActionEntity, ent.Comp.Action);
        if (TryComp(ent.Comp.ActionEntity, out action))
            SyncPipecrawlCooldown(ent, (ent.Comp.ActionEntity.Value, action));
    }

    private void SyncPipecrawlCooldown(Entity<DisposalPipeCrawlerComponent> ent, Entity<ActionComponent> action)
    {
        if (!ent.Comp.InsidePipe && ent.Comp.NextEntryAllowed > _timing.CurTime)
            _actions.SetCooldown(action.AsNullable(), ent.Comp.CooldownStartedAt, ent.Comp.NextEntryAllowed);
        else if (action.Comp.Cooldown != null)
            _actions.RemoveCooldown(action.AsNullable());
    }

    public void RefreshPipecrawlState(Entity<DisposalPipeCrawlerComponent> ent)
    {
        if (!ent.Comp.InsidePipe)
            ent.Comp.UnequippedItem = null;

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
            args.Item == ent.Comp.UnequippedItem)
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
        if (ent.Comp.InsidePipe)
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

    protected bool TryGetContainingDisposalUnit(
        EntityUid uid,
        [NotNullWhen(true)] out Entity<DisposalUnitComponent>? unit)
    {
        unit = null;
        if (!_container.TryGetContainingContainer(uid, out var container) ||
            container.ID != DisposalUnitComponent.ContainerId ||
            !TryComp<DisposalUnitComponent>(container.Owner, out var comp))
        {
            return false;
        }

        unit = (container.Owner, comp);
        return true;
    }

    private void OnPipecrawlUnequipAttempt(
        Entity<DisposalPipeCrawlerComponent> ent,
        ref IsUnequippingAttemptEvent args)
    {
        if (!ent.Comp.InsidePipe)
            return;

        if (args.User != ent.Owner || args.UnEquipTarget != ent.Owner)
        {
            args.Reason = "disposal-pipe-crawler-unequip-external";
            args.Cancel();
            return;
        }

        if (!_hands.TryGetEmptyHand((ent.Owner, null), out _))
        {
            args.Reason = "disposal-pipe-crawler-unequip-hands-full";
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

            args.Reason = "disposal-pipe-crawler-unequip-dependent";
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

        args.Reason = "disposal-pipe-crawler-unequip-external";
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

        // The unequipped item is no longer inside the crawler, so pickup would refuse it on its way to the hand.
        ent.Comp.UnequippedItem = args.Equipment;
    }

    private void OnPipecrawlEquippedHand(Entity<DisposalPipeCrawlerComponent> ent, ref DidEquipHandEvent args)
    {
        if (args.Equipped == ent.Comp.UnequippedItem)
            ent.Comp.UnequippedItem = null;
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
        return entity == crawler ||
               TryComp(entity, out TransformComponent? xform) && _transform.ContainsEntity(crawler, (entity, xform));
    }
}
