// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Storage;
using Content.Shared.Whitelist;

namespace Content.Shared.SS220.RestrictedItem;

public sealed partial class SharedRestrictedStorageSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RestrictedStorageComponent, StorageInteractAttemptEvent>(OnInsertAttempt);
    }

    private void OnInsertAttempt(Entity<RestrictedStorageComponent> entity, ref StorageInteractAttemptEvent args)
    {
        if (!StorageCheck(entity, Transform(entity).ParentUid))
            args.Cancelled = true;
    }

    private bool StorageCheck(Entity<RestrictedStorageComponent> storageEnt, EntityUid user)
    {

        if (_whitelistSystem.IsWhitelistFail(storageEnt.Comp.Whitelist, user))
            return false;

        return true;
    }

}
