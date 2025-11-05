using Content.Server.SS220.Storage.SpawnOnStorageOpen.Components;
using Content.Server.Storage.Components;
using Content.Shared.EntityTable;
using Content.Shared.Storage.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

using static Content.Shared.Storage.EntitySpawnCollection;

namespace Content.Server.SS220.Storage.SpawnOnStorageOpen.Systems;

public sealed class SpawnOnStorageOpenSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpawnOnStorageOpenComponent, StorageAfterOpenEvent>(OnOpen);

    }

    private void OnOpen(Entity<SpawnOnStorageOpenComponent> ent, StorageAfterOpenEvent args)
    {
        if (component.Uses <= 0)
            return;

        var coords = Transform(uid).Coordinates;

        foreach (var item in component.Selector.GetSpawns(new Random(),_entManager,_protoManager,new EntityTableContext()))
        {
            Spawn(item, coords);
        }

        component.Uses--;
    }
}
