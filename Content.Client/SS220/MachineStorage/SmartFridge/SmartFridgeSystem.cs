using Content.Shared.Storage.EntitySystems;
using Content.Shared.Storage;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Content.Shared.Hands;
using Content.Client.Animations;
using Robust.Shared.Map;
using Robust.Shared.Timing;

using Content.Shared.VendingMachines;
using Robust.Shared.GameObjects;

namespace Content.Client.SS220.MachineStorage.SmartFridge;

public sealed class SmartFridgeSystem : EntitySystem
{
    //[Dependency] private readonly IGameTiming _timing = default!;
    //[Dependency] private readonly EntityPickupAnimationSystem _entityPickupAnimation = default!;

    public event Action<EntityUid, StorageComponent>? StorageUpdated;


    //[Dependency] private readonly AnimationPlayerSystem _animationPlayer = default!;
    //[Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        //SubscribeNetworkEvent<PickupAnimationEvent>(HandlePickupAnimation);
        //SubscribeNetworkEvent<AnimateInsertingEntitiesEvent>(HandleAnimatingInsertingEntities);

        //SubscribeLocalEvent<VendingMachineComponent, AppearanceChangeEvent>(OnAppearanceChange);
        //SubscribeLocalEvent<VendingMachineComponent, AnimationCompletedEvent>(OnAnimationCompleted);
    }

    //перевод storage контейнера в List<VendingMachineInventoryEntry>
    public List<VendingMachineInventoryEntry> GetAllInventory(EntityUid uid, StorageComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return new();

        //VendingMachineComponent vendComponent;
        Dictionary<string, List<EntityUid>> sortedInventory2 = new();

        Dictionary<string, VendingMachineInventoryEntry> sortedInventory = new();

        foreach (var item in component.Container.ContainedEntities)
        {
            if (!TryInsertItem(item, sortedInventory))
                continue;
        }

        //заполнение, через заполненный 
        var inventory = new List<VendingMachineInventoryEntry>(sortedInventory.Values);

        return inventory;
    }

    /*private bool AddInList(EntityUid entityUid, Dictionary<string, List<EntityUid>> sortedInventory)
    {
        if (!TryGetItemCode(entityUid, out var itemId))
            return false;

        if (sortedInventory.ContainsKey(itemId) &&
            sortedInventory.TryGetValue(itemId, out var entry))
        {
            entry.Add(entityUid);
            return true;
        }

        sortedInventory.Add(itemId, (entityUid)));

        return true;
    }*/

    private bool TryInsertItem(EntityUid entityUid, Dictionary<string, VendingMachineInventoryEntry> sortedInventory)
    {
        if (!TryGetItemCode(entityUid, out var itemId))
            return false;

        if (sortedInventory.ContainsKey(itemId) &&
            sortedInventory.TryGetValue(itemId, out var entry))
        {
            entry.Amount++;
            entry.EntityUids.Add(GetNetEntity(entityUid));
            return true;
        }

        sortedInventory.Add(itemId,
            new VendingMachineInventoryEntry(InventoryType.Regular, itemId, 1, GetNetEntity(entityUid))
        );

        return true;
    }

    private bool TryGetItemCode(EntityUid entityUid, out string code)
    {
        var metadata = IoCManager.Resolve<IEntityManager>().GetComponentOrNull<MetaDataComponent>(entityUid);
        code = metadata?.EntityPrototype?.ID ?? "";
        return !string.IsNullOrEmpty(code);
    }
}
