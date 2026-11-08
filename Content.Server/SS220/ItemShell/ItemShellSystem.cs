// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.SS220.ItemShell;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;

namespace Content.Server.SS220.ItemShell;

/// <summary>
/// Unfolds persistent item shells.
/// </summary>
public sealed partial class ItemShellSystem : EntitySystem
{
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedContainerSystem _containers = default!;
    [Dependency] private ShellableItemSystem _shellableItems = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ItemShellComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<ItemShellComponent, EntityTerminatingEvent>(OnShellTerminating);
    }

    private void OnUseInHand(Entity<ItemShellComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (!_hands.IsHolding(args.User, ent, out var hand))
            return;

        var contents = _containers.EnsureContainer<ContainerSlot>(ent, ItemShellComponent.ContentContainerId);
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

        var shellContainer = _containers.EnsureContainer<ContainerSlot>(item, ShellableItemComponent.ShellContainerId);
        if (!_containers.Insert(ent.Owner, shellContainer))
        {
            // The item has already left the shell, so folding can safely restore the pair.
            if (_shellableItems.TryFold((item, Comp<ShellableItemComponent>(item)), (ent.Owner, ent.Comp)))
                _hands.PickupOrDrop(args.User, ent);
            return;
        }

        _audio.PlayPvs(ent.Comp.Sound, args.User);
        args.Handled = true;
    }

    private void OnShellTerminating(Entity<ItemShellComponent> ent, ref EntityTerminatingEvent args)
    {
        if (ent.Comp.LinkedItem != null && !TerminatingOrDeleted(ent.Comp.LinkedItem.Value))
            QueueDel(ent.Comp.LinkedItem.Value);
    }
}
