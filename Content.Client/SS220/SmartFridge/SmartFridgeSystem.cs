using Content.Shared.Storage;
using Content.Shared.VendingMachines;

namespace Content.Client.SS220.SmartFridge;

public sealed class SmartFridgeSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entity = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    //перевод storage контейнера в List<VendingMachineInventoryEntry>
    public List<VendingMachineInventoryEntry> GetAllInventory(EntityUid uid, StorageComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return new();

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
