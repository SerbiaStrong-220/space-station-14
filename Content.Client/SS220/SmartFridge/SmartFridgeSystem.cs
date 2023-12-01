using Content.Shared.Storage;
using Robust.Client.GameObjects;
using Content.Shared.VendingMachines;
using Robust.Shared.GameObjects;
using Robust.Client.Player;
using Content.Shared.Hands;
using Content.Client.Animations;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using Robust.Client.Animations;
using Content.Shared.Storage.EntitySystems;

namespace Content.Client.SS220.SmartFridge;

public sealed class SmartFridgeSystem : EntitySystem
{
    //[Dependency] private readonly AnimationPlayerSystem _animationPlayer = default!;
    //[Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly IEntityManager _entity = default!;

    public override void Initialize()
    {
        base.Initialize();

        //SubscribeLocalEvent<VendingMachineComponent, AppearanceChangeEvent>(OnAppearanceChange);
        //SubscribeLocalEvent<VendingMachineComponent, AnimationCompletedEvent>(OnAnimationCompleted);
    }

    //перевод storage контейнера в List<VendingMachineInventoryEntry>
    public List<VendingMachineInventoryEntry> GetAllInventory(EntityUid uid, StorageComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return new();

        //VendingMachineComponent vendComponent;
        //Dictionary<string, List<EntityUid>> sortedInventory2 = new();

        Dictionary<string, VendingMachineInventoryEntry> sortedInventory = new();

        foreach (var item in component.Container.ContainedEntities)
        {
            if (!TryInsertItem(item, sortedInventory))
                continue;
        }

        var inventory = new List<VendingMachineInventoryEntry>(sortedInventory.Values);

        return inventory;
    }

    private bool TryInsertItem(EntityUid entityUid, Dictionary<string, VendingMachineInventoryEntry> sortedInventory)
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


        sortedInventory.Add(metadata.EntityName,
            new VendingMachineInventoryEntry(InventoryType.Regular, metadata.EntityName, 1, GetNetEntity(entityUid))
        );

        return true;
    }
}
