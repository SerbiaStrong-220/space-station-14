// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Administration.Managers;
using Content.Shared.Mind;
using Content.Shared.Storage;

namespace Content.Shared.SS220.RoleItem;

public sealed partial class SharedRoleStorageSystem : EntitySystem
{
    [Dependency] private readonly SharedRoleItemSystem _roleItem = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoleStorageComponent, StorageInteractAttemptEvent>(OnInsertAttempt);
    }

    private void OnInsertAttempt(Entity<RoleStorageComponent> entity, ref StorageInteractAttemptEvent args)
    {
        if (!StorageCheck(entity, Transform(entity).ParentUid))
            args.Cancelled = true;
    }

    private bool StorageCheck(Entity<RoleStorageComponent> storageEnt, EntityUid user)
    {
        if (!_mind.TryGetMind(user, out var mindId, out _))
            return false;

        if (!_roleItem.IsValidRolePrototype(mindId, storageEnt.Comp.RoleId))
            return false;

        return true;
    }

}
