// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Storage;
using Content.Shared.VendingMachines;
using Content.Shared.Hands.Components;
using Content.Shared.ActionBlocker;
using Robust.Shared.Audio.Systems;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.Player;

namespace Content.Shared.SS220.SmartFridge;
public abstract class SharedSmartFridgeSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlockerSystem = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] private readonly SharedHandsSystem _sharedHandsSystem = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StorageComponent, SmartFridgeInteractWithItemEvent>(OnInteractWithItem);
    }

    //transfering storage into List<VendingMachineInventoryEntry>
    public List<VendingMachineInventoryEntry> GetAllInventory(EntityUid uid, StorageComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return new();

        Dictionary<string, VendingMachineInventoryEntry> sortedInventory = new();

        foreach (var item in component.Container.ContainedEntities)
        {
            if (!TryAddItem(item, sortedInventory))
                continue;
        }

        var inventory = new List<VendingMachineInventoryEntry>(sortedInventory.Values);

        return inventory;
    }
    private bool TryAddItem(EntityUid entityUid, Dictionary<string, VendingMachineInventoryEntry> sortedInventory)
    {
        if (!_entity.TryGetComponent<MetaDataComponent>(entityUid, out var metadata))
            return false;

        if (sortedInventory.ContainsKey(metadata.EntityName) &&
            sortedInventory.TryGetValue(metadata.EntityName, out var entry))
        {
            entry.Amount++;
            entry.EntityUids.Add(GetNetEntity(entityUid));
            return true;
        }

        var newEntry = new VendingMachineInventoryEntry(InventoryType.Regular, metadata.EntityName, 1);
        newEntry.EntityUids.Add(GetNetEntity(entityUid));
        sortedInventory.Add(metadata.EntityName, newEntry);

        return true;
    }

    private void OnInteractWithItem(EntityUid uid, StorageComponent storageComp, SmartFridgeInteractWithItemEvent args)
    {
        var player = args.Actor;
        var entity = GetEntity(args.InteractedItemUID);

        if (!Exists(entity))
        {
            if (TryComp<ActorComponent>(player, out var actor))
            {
                var session = actor.PlayerSession;
                Log.Error($"Player {session} interacted with non-existent item {args.InteractedItemUID} stored in {ToPrettyString(uid)}");
            }

            return;
        }

        if (!_actionBlockerSystem.CanInteract(player, entity) || !storageComp.Container.Contains(entity))
            return;

        // Does the player have hands?
        if (!TryComp(player, out HandsComponent? hands) || hands.Count == 0)
            return;

        // If the user's active hand is empty, try pick up the item.
        if (hands.ActiveHandEntity == null)
        {
            if (_sharedHandsSystem.TryPickupAnyHand(player, entity, handsComp: hands)
                && storageComp.StorageRemoveSound != null)
                Audio.PlayPredicted(storageComp.StorageRemoveSound, uid, player);
            {
                return;
            }
        }
        else
        {
            _container.Remove(entity, storageComp.Container);
        }
    }
}
