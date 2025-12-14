// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.Atmos.EntitySystems;
using Content.Server.SS220.DarkReaper;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using Content.Shared.SS220.DarkReaper;
using Content.Shared.Temperature.Components;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Content.IntegrationTests.SS220.Tests.DarkReaper;

[TestFixture]
public sealed class MobConsumedByDarkReaperTest
{
    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  id: DarkReaperDummy
  components:
  - type: DarkReaper
  - type: AntiRottingContainer
  - type: ContainerContainer
    containers:
      consumed: !type:Container
        ents: []
        showEnts: false
        occludes: true

- type: entity
  id: MobDummy
  components:
  - type: Damageable
    damageContainer: Biological
  - type: MobState
  - type: MobThresholds
    thresholds:
      0: Alive
      100: Critical
      200: Dead
  - type: Temperature
    heatDamageThreshold: 325
    coldDamageThreshold: 260
    currentTemperature: 310.15
    specificHeat: 42
    coldDamage:
      types:
        Cold: 1
    heatDamage:
      types:
        Heat: 1.5
  - type: Barotrauma
    damage:
      types:
        Blunt: 0.5
  - type: Appearance
  - type: Flammable
    fireSpread: true
    canResistFire: true
    damage:
      types:
        Heat: 1.5
";

    [Test]
    public async Task AtmosphericsDamageImmunityTest()
    {
        await using var pair = await PoolManager.GetServerClient();

        var server = pair.Server;

        var defMan = server.ResolveDependency<ITileDefinitionManager>();
        var mapSys = server.EntMan.System<MapSystem>();
        var atmosphereSys = server.EntMan.System<AtmosphereSystem>();


        // Create a 3x3 box with walls on the border
        const string tileId = "FloorSteel";
        const string wallId = "WallReinforced";
        const int gridRadius = 1;

        Assert.That(defMan.TryGetDefinition(tileId, out var tileDef));
        var mapData = await pair.CreateTestMap(false, tileDef.TileId);

        await server.WaitPost(() =>
        {
            var tile = new Tile(tileDef.TileId);
            var grid = mapData.Grid;

            var xMin = -gridRadius;
            var xMax = gridRadius;
            var yMin = -gridRadius;
            var yMax = gridRadius;

            var pos = new Vector2i(xMin, yMin);
            while (pos.Y <= yMax)
            {
                while (pos.X <= xMax)
                {
                    mapSys.SetTile(grid, pos, tile);

                    // if the tile is on the border
                    if (pos.X == xMin || pos.X == xMax || pos.Y == yMin || pos.Y == xMax)
                    {
                        var coords = new EntityCoordinates(grid, mapSys.TileCenterToVector(grid, pos));
                        server.EntMan.SpawnAtPosition(wallId, coords);
                    }

                    pos.X++;
                }

                pos.Y++;
                pos.X = xMin;
            }
        });

        mapSys.InitializeMap(mapData.MapId);
        await pair.RunSeconds(1);

        var centerTileCoords = mapData.Tile.GridIndices;
        var centerEntCoords = new EntityCoordinates(mapData.Grid, mapSys.TileCenterToVector(mapData.Grid, centerTileCoords));


        // Spawn dummies
        var (reaper, mob) = await PreapareConsumedMob(pair, centerEntCoords);
        await server.WaitRunTicks(1);

        DamageableComponent mobDamageable = default;
        FixedPoint2 mobDeathDamage = default;
        await server.WaitPost(() =>
        {
            mobDamageable = server.EntMan.GetComponent<DamageableComponent>(mob);
            mobDeathDamage = mobDamageable.TotalDamage;
        });


        // Test low pressure damage immunity
        await server.WaitAssertion(() =>
        {
            var mixture = atmosphereSys.GetTileMixture(mapData.Grid.Owner, null, centerTileCoords);
            Assert.That(mixture.Pressure, Is.LessThan(Atmospherics.HazardLowPressure));
        });
        await pair.RunSeconds(5);
        Assert.That(mobDamageable.TotalDamage, Is.EqualTo(mobDeathDamage));


        // Test high pressure damage immunity
        await server.WaitPost(() =>
        {
            var mixture = atmosphereSys.GetTileMixture(mapData.Grid.Owner, null, centerTileCoords);
            while (mixture.Pressure < Atmospherics.HazardHighPressure)
                mixture.AdjustMoles(Gas.Oxygen, 100f);

            Assert.That(mixture.Pressure, Is.GreaterThan(Atmospherics.HazardHighPressure));
        });
        await pair.RunSeconds(5);
        Assert.That(mobDamageable.TotalDamage, Is.EqualTo(mobDeathDamage));


        // Test temperature damage immunity
        await server.WaitPost(() =>
        {
            var temperatureComp = server.EntMan.GetComponent<TemperatureComponent>(mob);

            var mixture = atmosphereSys.GetTileMixture(mapData.Grid.Owner, null, centerTileCoords);
            if (mixture.Pressure <= 0)
                mixture.AdjustMoles(Gas.Oxygen, 100f);

            var tileTemperature = temperatureComp.HeatDamageThreshold * 2f;
            mixture.Temperature = tileTemperature;
            Assert.That(mixture.Temperature, Is.GreaterThanOrEqualTo(tileTemperature));
        });
        await pair.RunSeconds(5);
        Assert.That(mobDamageable.TotalDamage, Is.EqualTo(mobDeathDamage));

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task FireImmunityTest()
    {
        await using var pair = await PoolManager.GetServerClient();

        var server = pair.Server;

        var defMan = server.ResolveDependency<ITileDefinitionManager>();
        var mapSys = server.EntMan.System<MapSystem>();
        var flammableSys = server.EntMan.System<FlammableSystem>();


        // Create test map & spawn dummies
        const string tileId = "FloorSteel";

        Assert.That(defMan.TryGetDefinition(tileId, out var tileDef));
        var mapData = await pair.CreateTestMap(false, tileDef.TileId);
        mapSys.InitializeMap(mapData.MapId);
        await pair.RunSeconds(1);

        var center = new EntityCoordinates(mapData.Grid, mapSys.TileCenterToVector(mapData.Grid, mapData.Tile.GridIndices));
        var (reaper, mob) = await PreapareConsumedMob(pair, center);
        await server.WaitRunTicks(1);

        DamageableComponent mobDamageable = default;
        FixedPoint2 mobDeathDamage = default;
        await server.WaitPost(() =>
        {
            mobDamageable = server.EntMan.GetComponent<DamageableComponent>(mob);
            mobDeathDamage = mobDamageable.TotalDamage;
        });


        // Test fire immunity
        await server.WaitPost(() =>
        {
            var flammable = server.EntMan.GetComponent<FlammableComponent>(mob);

            flammableSys.SetFireStacks(mob, 10);
            flammableSys.Ignite(mob, reaper);

            Assert.Multiple(() =>
            {
                Assert.That(flammable.FireStacks, Is.EqualTo(0));
                Assert.That(flammable.OnFire, Is.False);
            });
        });
        await pair.RunSeconds(5);
        Assert.That(mobDamageable.TotalDamage, Is.EqualTo(mobDeathDamage));

        await pair.CleanReturnAsync();
    }

    private static async Task<(Entity<DarkReaperComponent> Reaper, EntityUid Mob)> PreapareConsumedMob(Pair.TestPair pair, EntityCoordinates spawnCoords)
    {
        const string reaperId = "DarkReaperDummy";
        const string mobId = "MobDummy";
        const string deathDamageType = "Piercing";

        var server = pair.Server;

        var reaperSys = server.EntMan.System<DarkReaperSystem>();
        var damageableSys = server.EntMan.System<DamageableSystem>();
        var mobStateSys = server.EntMan.System<MobStateSystem>();
        var mobThresholdsSys = server.EntMan.System<MobThresholdSystem>();
        var containerSys = server.EntMan.System<ContainerSystem>();

        Entity<DarkReaperComponent> reaper = default;
        EntityUid mob = default;
        await server.WaitPost(() =>
        {
            var reaperUid = server.EntMan.SpawnAtPosition(reaperId, spawnCoords);
            var reaperComp = server.EntMan.GetComponent<DarkReaperComponent>(reaperUid);
            reaper = (reaperUid, reaperComp);

            mob = server.EntMan.SpawnAtPosition(mobId, spawnCoords);
        });

        await server.WaitRunTicks(1);

        await server.WaitPost(() =>
        {
            Assert.That(mobThresholdsSys.TryGetThresholdForState(mob, Shared.Mobs.MobState.Dead, out var deadThreshold));
            var deathDamage = new DamageSpecifier()
            {
                DamageDict = new()
                {
                    { deathDamageType, deadThreshold.Value },
                }
            };

            var changedDamage = damageableSys.TryChangeDamage(mob, deathDamage, ignoreResistances: true, ignoreGlobalModifiers: true);
            Assert.That(changedDamage, Is.Not.Null);
            Assert.That(changedDamage.GetTotal(), Is.EqualTo(deathDamage.GetTotal()));
            Assert.That(mobStateSys.IsDead(mob));

            reaperSys.ChangeForm(reaper, true);
            Assert.That(reaper.Comp.PhysicalForm);
            Assert.That(reaperSys.TryConsumeTarget(reaper, mob));
            Assert.That(containerSys.TryGetContainer(reaper, DarkReaperComponent.ConsumedContainerId, out var container));
            Assert.That(container.Contains(mob));
        });

        await server.WaitRunTicks(1);

        await server.WaitPost(() => reaperSys.ChangeForm(reaper, false));
        await server.WaitRunTicks(1);

        return (reaper, mob);
    }
}
