// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.SS220.InnerHandToggleable;
using Content.Shared.SS220.ItemShell;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;

namespace Content.Server.SS220.ItemShell;

/// <summary>
/// Swaps which member of a persistent shell/item pair contains the other.
/// Container changes are settled before deciding whether an item must fold.
/// </summary>
public sealed partial class ItemShellSystem : EntitySystem
{
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private readonly HashSet<EntityUid> _pending = [];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ItemShellComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<ItemShellComponent, EntityTerminatingEvent>(OnShellTerminating);
        SubscribeLocalEvent<ShellableItemComponent, EntGotRemovedFromContainerMessage>(OnRemove);
        SubscribeLocalEvent<ShellableItemComponent, EntGotInsertedIntoContainerMessage>(OnInsert);
        SubscribeLocalEvent<ShellableItemComponent, EntityTerminatingEvent>(OnItemTerminating);
    }

    private void OnUseInHand(Entity<ItemShellComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (!_hands.IsHolding(args.User, ent, out var hand))
            return;

        var contents = (ContainerSlot)_containers.GetContainer(ent, ItemShellComponent.ContentContainerId);
        if (contents.ContainedEntity == null)
            return;

        var item = contents.ContainedEntity.Value;
        if (TerminatingOrDeleted(item) || EntityManager.IsQueuedForDeletion(item))
            return;

        // Free this exact hand before pickup; keep both entities if pickup is refused.
        if (!_hands.TryDrop(args.User, ent, checkActionBlocker: false))
            return;

        if (!_hands.TryPickup(args.User, item, hand))
        {
            _hands.PickupOrDrop(args.User, ent);
            return;
        }

        var shellContainer = (ContainerSlot)_containers.GetContainer(item, ShellableItemComponent.ShellContainerId);
        if (!_containers.Insert(ent.Owner, shellContainer))
        {
            // The item has already left the shell, so folding can safely restore the pair.
            if (TryFold((item, Comp<ShellableItemComponent>(item)), ent))
                _hands.PickupOrDrop(args.User, ent);
            return;
        }

        _audio.PlayPvs(ent.Comp.Sound, args.User);
        args.Handled = true;
    }

    private void Link(Entity<ItemShellComponent> shell, Entity<ShellableItemComponent> item)
    {
        shell.Comp.LinkedItem = item;
        item.Comp.Shell = shell;
        Dirty(item);
    }

    private void OnRemove(Entity<ShellableItemComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        _pending.Add(ent);
    }

    private void OnInsert(Entity<ShellableItemComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        _pending.Add(ent);

        if (ent.Comp.Shell != null)
            return;

        if (args.Container.ID != ItemShellComponent.ContentContainerId)
            return;

        if (!TryComp<ItemShellComponent>(args.Container.Owner, out var shell))
            return;

        if (shell.LinkedItem != null)
            return;

        Link((args.Container.Owner, shell), ent);
    }

    private void OnShellTerminating(Entity<ItemShellComponent> ent, ref EntityTerminatingEvent args)
    {
        if (ent.Comp.LinkedItem != null && !TerminatingOrDeleted(ent.Comp.LinkedItem.Value))
            QueueDel(ent.Comp.LinkedItem.Value);
    }

    private void OnItemTerminating(Entity<ShellableItemComponent> ent, ref EntityTerminatingEvent args)
    {
        _pending.Remove(ent);
        if (ent.Comp.Shell != null && !TerminatingOrDeleted(ent.Comp.Shell.Value))
            QueueDel(ent.Comp.Shell.Value);
    }

    /// <summary>
    /// Folds an item outside hands and registered hidden hand containers.
    /// </summary>
    private bool TryFold(Entity<ShellableItemComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (TerminatingOrDeleted(ent) || EntityManager.IsQueuedForDeletion(ent))
            return false;

        if (CanRemainUnfolded(ent))
            return false;

        if (!TryGetShell((ent.Owner, ent.Comp), out var shell))
            return false;

        return TryFold((ent.Owner, ent.Comp), shell);
    }

    private bool CanRemainUnfolded(EntityUid item)
    {
        if (!_containers.TryGetContainingContainer((item, null, null), out var container))
            return false;

        if (_hands.IsHolding(container.Owner, item))
            return true;

        if (!TryComp<InnerHandToggleableComponent>(container.Owner, out var inner))
            return false;

        foreach (var (hand, info) in inner.HandsContainers)
        {
            if (info.Container != container || info.InnerItemUid != item)
                continue;

            return _hands.TryGetHand(container.Owner, hand, out _);
        }

        return false;
    }

    private bool TryGetShell(Entity<ShellableItemComponent> item, out Entity<ItemShellComponent> shell)
    {
        shell = default;
        if (item.Comp.Shell == null)
            return false;

        var uid = item.Comp.Shell.Value;
        if (TerminatingOrDeleted(uid) || EntityManager.IsQueuedForDeletion(uid))
            return false;

        if (!TryComp<ItemShellComponent>(uid, out var component))
            return false;

        shell = (uid, component);
        return true;
    }

    private bool TryFold(Entity<ShellableItemComponent> item, Entity<ItemShellComponent> shell)
    {
        var contents = (ContainerSlot)_containers.GetContainer(shell, ItemShellComponent.ContentContainerId);
        if (contents.Contains(item))
            return true;

        var coords = Transform(item).Coordinates;
        var previous = _containers.TryGetContainingContainer((item.Owner, null, null), out var container)
            ? container
            : null;

        // Detach the shell first: inserting the item while it still contains the shell would create a cycle.
        if (_containers.TryGetContainingContainer((shell.Owner, null, null), out var shellContainer))
        {
            if (!_containers.Remove(shell.Owner, shellContainer, reparent: false, force: true))
                return false;
        }

        _transform.SetCoordinates(shell, coords);
        if (previous != null && !_containers.Remove(item.Owner, previous, force: true))
            return false;

        if (!_containers.Insert(item.Owner, contents, force: true))
            return false;

        // A destination can reject the differently sized shell; dropping it nearby is still a valid folded state.
        if (previous != null)
            _containers.InsertOrDrop(shell.Owner, previous);

        return true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        ProcessPendingFolds();
    }

    /// <summary>
    /// Processes queued folding checks after container operations have finished.
    /// </summary>
    private void ProcessPendingFolds()
    {
        if (_pending.Count == 0)
            return;

        // Insertion also raises removal events. Inspect only the settled state, without a gameplay timer.
        var pending = _pending.ToArray();
        _pending.Clear();
        foreach (var uid in pending)
        {
            if (TerminatingOrDeleted(uid))
                continue;

            if (EntityManager.IsQueuedForDeletion(uid))
                continue;

            if (!TryComp<ShellableItemComponent>(uid, out var item))
                continue;

            TryFold((uid, item));
        }
    }
}
