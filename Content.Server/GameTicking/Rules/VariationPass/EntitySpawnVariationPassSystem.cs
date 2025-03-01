using Content.Server.GameTicking.Rules.VariationPass.Components;
using Content.Shared.Storage;
using Robust.Shared.Random;
using Content.Shared.Whitelist;

namespace Content.Server.GameTicking.Rules.VariationPass;

/// <inheritdoc cref="EntitySpawnVariationPassComponent"/>
public sealed class EntitySpawnVariationPassSystem : VariationPassSystem<EntitySpawnVariationPassComponent>
{
    // SS220 SM garbage fix begin
    [Dependency] private readonly EntityLookupSystem _lookupSystem = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;
    private const float Range = 3f; // Range to check SM nearby
    // SS220 SM garbage fix end
    protected override void ApplyVariation(Entity<EntitySpawnVariationPassComponent> ent, ref StationVariationPassEvent args)
    {
        var totalTiles = Stations.GetTileCount(args.Station);

        var dirtyMod = Random.NextGaussian(ent.Comp.TilesPerEntityAverage, ent.Comp.TilesPerEntityStdDev);
        var trashTiles = Math.Max((int) (totalTiles * (1 / dirtyMod)), 0);

        for (var i = 0; i < trashTiles; i++)
        {
            if (!TryFindRandomTileOnStation(args.Station, out _, out _, out var coords))
                continue;

            // SS220 SM garbage fix begin
            var listTakedEntites = _lookupSystem.GetEntitiesInRange(coords, Range);
            bool isCanSpawn = true;

            foreach (var checkedTakedEntities in listTakedEntites)
            {
                if (_whitelistSystem.IsBlacklistPass(ent.Comp.Blacklist, checkedTakedEntities))
                    isCanSpawn = false;
            }
            if (!isCanSpawn)
                continue;
            // SS220 SM garbage fix end

            var ents = EntitySpawnCollection.GetSpawns(ent.Comp.Entities, Random);
            foreach (var spawn in ents)
            {
                SpawnAtPosition(spawn, coords);
            }
        }
    }
}
