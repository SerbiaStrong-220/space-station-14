using System.Linq;
using Content.Shared.ActionBlocker;
using Content.Shared.CombatMode;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.Hands;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Implants.Components;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Lock;
using Content.Shared.Placeable;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Storage.Components;
using Content.Shared.Timing;
using Content.Shared.Verbs;
using Robust.Shared.Containers;
using Content.Shared.Storage;
using Robust.Shared.Map;
using Robust.Shared.Random;

namespace Content.Shared.SS220.MachineStorage.SmartFridge;

public abstract class SharedSmartFridgeSystem : EntitySystem
{
    [Dependency] private   readonly ActionBlockerSystem _actionBlockerSystem = default!;
    [Dependency] private   readonly SharedHandsSystem _sharedHandsSystem = default!;
    [Dependency] private   readonly SharedInteractionSystem _interactionSystem = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;

    private EntityQuery<ItemComponent> _itemQuery;
    private EntityQuery<StackComponent> _stackQuery;
    private EntityQuery<TransformComponent> _xformQuery;

    /// <inheritdoc />
    public override void Initialize()
    {
        base.Initialize();

        _itemQuery = GetEntityQuery<ItemComponent>();
        _stackQuery = GetEntityQuery<StackComponent>();
        _xformQuery = GetEntityQuery<TransformComponent>();
    }
    private void OnInteractWithItem(EntityUid uid, StorageComponent storageComp, SmartFridgeInteractWithItemEvent args)
    {
        if (args.Session.AttachedEntity is not EntityUid player)
            return;

        var entity = GetEntity(args.InteractedItemUID);

        if (!Exists(entity))
        {
            Log.Error($"Player {args.Session} interacted with non-existent item {args.InteractedItemUID} stored in {ToPrettyString(uid)}");
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

        // Else, interact using the held item
        _interactionSystem.InteractUsing(player, hands.ActiveHandEntity.Value, entity, Transform(entity).Coordinates, checkCanInteract: false);
    }
}
