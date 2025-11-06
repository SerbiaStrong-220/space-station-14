using Content.Server.SS220.Storage.SpawnOnStorageOpen.Components;
using Content.Server.Storage.Components;
using Content.Shared.EntityTable;
using Content.Shared.Jaunt;
using Content.Shared.Storage.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

using static Content.Shared.Storage.EntitySpawnCollection;

namespace Content.Server.SS220.Storage.SpawnOnStorageOpen.Systems;

public sealed class SpawnOnStorageOpenSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpawnOnStorageOpenComponent, StorageAfterOpenEvent>(OnOpen);
        SubscribeLocalEvent<SpawnOnStorageOpenComponent, MapInitEvent>(OnMapInit);

    }

    private void SpawnTriggered(Entity<SpawnOnStorageOpenComponent> ent)
    {
        if (LifeStage(ent.Owner) != EntityLifeStage.MapInitialized)
            return;
        if (ent.Comp.Uses <= 0)
            return;

        var coords = Transform(ent.Owner).Coordinates;

        foreach (var item in ent.Comp.Selector.GetSpawns(_random.GetRandom(), _entManager, _protoManager, new EntityTableContext()))
        {
            Spawn(item, coords);
        }

        ent.Comp.Uses--;
    }
    private void OnOpen(Entity<SpawnOnStorageOpenComponent> ent, ref StorageAfterOpenEvent args)
    {
        SpawnTriggered(ent);
    }
    private void OnMapInit(Entity<SpawnOnStorageOpenComponent> ent, ref MapInitEvent args)
    {
        if(!EntityManager.TryGetComponent(ent.Owner,out EntityStorageComponent? cmp))
        {
            return;
        }
        if(cmp.Open==true)
        {
            SpawnTriggered(ent);
        }
    }
}
