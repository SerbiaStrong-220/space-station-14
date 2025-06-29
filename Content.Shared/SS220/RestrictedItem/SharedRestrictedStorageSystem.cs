// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Inventory;
using Content.Shared.Storage;
using Content.Shared.Whitelist;

namespace Content.Shared.SS220.RestrictedItem;

public sealed partial class SharedRestrictedStorageSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RestrictedStorageComponent, StorageInteractAttemptEvent>(OnInteractAttempt);
    }

    private void OnInteractAttempt(Entity<RestrictedStorageComponent> entity, ref StorageInteractAttemptEvent args)
    {
        if (!StorageCheck(entity, args.User))
            args.Cancelled = true;
    }

    private bool StorageCheck(Entity<RestrictedStorageComponent> storageEnt, EntityUid user)
    {
        if (_whitelistSystem.IsWhitelistFail(storageEnt.Comp.Whitelist, user))
            return false;

        if (storageEnt.Comp.Slots is { } slots &&
            !_inventory.InSlotWithFlags(storageEnt.Owner, slots))
            return false;

        return true;
    }

}
