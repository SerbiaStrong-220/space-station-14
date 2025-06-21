using Content.Shared.GameTicking;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Inventory;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Loadouts.SpeciesLoadout;

public sealed class SpeciesLoadoutSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawn);
    }

    private void OnPlayerSpawn(PlayerSpawnCompleteEvent args)
    {
        if (args.Profile is not { } profile)
            return;

        var speciesId = profile.Species;

        foreach (var prototype in _proto.EnumeratePrototypes<SpeciesPrototype>())
        {
            if (prototype.ID != speciesId)
                continue;

            if (args.JobId == null || prototype.BlacklistJobsForStartingGear.Contains(args.JobId))
                continue;

            foreach (var (slot, items) in prototype.StartingGear)
            {
                if (!_inventory.TryGetSlotEntity(args.Mob, slot, out var slotEntity))
                    continue;

                foreach (var entProtoId in items)
                {
                    var item = Spawn(entProtoId, Transform(slotEntity.Value).Coordinates);

                    _storage.Insert(slotEntity.Value, item, out _, args.Mob);
                }
            }
        }
    }
}
