// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Collections.Generic;
using System.Linq;
using Content.Server.Atmos.EntitySystems;
using Content.Server.CombatMode;
using Content.Server.Weapons.Melee;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.SS220.MartialArts;
using Content.Shared.Weapons.Melee;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.SS220.Tests.MartialArts;

[TestFixture]
public sealed class MartialArtsTest
{
    [TestPrototypes]
    private const string Prototypes = @"
- type: martialArt
  id: DummyMartialArt
  name: martial-art-debug
  sequences:
  - name: martial-art-debug-consequtive-slap
    steps: [ Harm, Grab, Push ]
    entry:
      conditions:
      - !type:IsDownCombatCondition
        invert: true
      effects:
      - !type:ApplyDamageCombatEffect
        damage:
          types:
            Blunt: 8
      - !type:ApplyStaminaCombatEffect
        damage: 50
        ignoreResist: true
      - !type:ApplyStatusEffectCombatEffect
        effect: SlowedDown
        time: 5
        timeLimit: 30
        refresh: false

- type: entity
  id: MobMartialArtsDummy
  components:
  - type: Sprite
  - type: MartialArtist
  - type: MartialArtsTarget
  - type: Physics
    bodyType: KinematicController
  - type: MovementSpeedModifier
  - type: Damageable
    damageContainer: Biological
  - type: StatusEffects
    allowed:
    - SlowedDown
  - type: MobState
  - type: MobThresholds
    thresholds:
      0: Alive
      100: Critical
      200: Dead
  - type: Stamina
  - type: Pullable
  - type: Puller
    pullCooldown: 0
  - type: Fixtures
    fixtures:
      fix1:
        shape:
          !type:PhysShapeCircle
          radius: 0.35
        density: 185
        restitution: 0.0
        mask:
        - MobMask
        layer:
        - MobLayer
  - type: Hands
  - type: Body
    prototype: Human
    requiredLegs: 2
  - type: ComplexInteraction
  - type: CombatMode
    canDisarm: true
  - type: MeleeWeapon
    soundHit:
      collection: Punch
    angle: 30
    animation: WeaponArcFist
    attackRate: 1
    damage:
      types:
        Blunt: 5
";

    [Test]
    public async Task PerformSequenceTest()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = true,
            DummyTicker = true
        });

        var server = pair.Server;
        var client = pair.Client;

        var protoMng = server.ResolveDependency<IPrototypeManager>();

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
        var (artist, mob) = await PrepareMobs(pair, centerEntCoords);
        await pair.RunTicksSync(1);

        var meleeSys = server.EntMan.System<MeleeWeaponSystem>();
        var pullingSys = server.EntMan.System<PullingSystem>();
        var artsSys = server.EntMan.System<MartialArtsSystem>();

        // Test sequence
        // Harm
        await server.WaitPost(() =>
        {
            meleeSys.AttemptLightAttack(artist, artist, artist.Comp2, mob);
        });

        await pair.RunSeconds(2);

        Assert.That(artist.Comp1.CurrentSteps, Is.EquivalentTo([CombatSequenceStep.Harm]), "Attempted to light attack mob and expected \"HARM\" step to appear");

        // Grab
        await server.WaitPost(() =>
        {
            pullingSys.TryStartPull(artist, mob);
        });

        await pair.RunSeconds(2);

        Assert.That(artist.Comp1.CurrentSteps, Is.EquivalentTo([CombatSequenceStep.Harm, CombatSequenceStep.Grab]), "Attempted to pull mob and expected \"GRAB\" step to appear");

        // Push (disarm)
        await server.WaitPost(() =>
        {
            meleeSys.AttemptDisarmAttack(artist, artist, artist.Comp2, mob);
        });

        Assert.Multiple(() =>
        {
            Assert.That(artist.Comp1.CurrentSteps, Is.EquivalentTo(new List<CombatSequenceStep>()), "Current steps expected to be empty after finishing combo");
            // idk how to properly check that combo was successfull, gonna rely on cooldown for now
            Assert.That(artsSys.IsInCooldown(artist, artist.Comp1), "Expected artist to be in cooldown after successfull combo");
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task AllSequencesReachableTest()
    {
        await using var pair = await PoolManager.Instance.GetPair();
        var server = pair.Server;

        var artsSys = server.EntMan.System<MartialArtsSystem>();
        var protoMng = server.ResolveDependency<IPrototypeManager>();

        var arts = protoMng.EnumeratePrototypes<MartialArtPrototype>();

        foreach (var art in arts)
        {
            foreach (var sequence in art.Sequences)
            {
                Assert.Multiple(() =>
                {
                    Assert.That(artsSys.TryGetSequence(sequence.Steps, art.Sequences.ToList(), out var found, out var complete), $"Failed to get sequence \"{sequence.Name}\" of art \"{art.ID}\" when it's defined in prototype, probably something is broke in MartialArtsSystem.TryGetSequence");
                    Assert.That(complete, $"Sequence \"{sequence.Name}\" of art \"{art.ID}\" isn't complete after passing full sequence");
                    Assert.That(found, Is.EqualTo(sequence), $"Performed steps of sequence \"{sequence.Name}\" of art \"{art.ID}\" but got sequence \"{found.Value.Name}\"");
                });

                // now check it step-by-step
                List<CombatSequenceStep> currentSteps = [];

                foreach (var step in sequence.Steps)
                {
                    currentSteps.Add(step);

                    Assert.Multiple(() =>
                    {
                        Assert.That(artsSys.TryGetSequence(currentSteps, art.Sequences.ToList(), out var found, out var complete), $"While performing sequence \"{sequence.Name}\" of art \"{art.ID}\" step-by-step failed to get any match; Current Steps: [{string.Join(", ", currentSteps)}]");
                        Assert.That(currentSteps.SequenceEqual(sequence.Steps) || !currentSteps.SequenceEqual(sequence.Steps) && !complete, $"While performing sequence \"{sequence.Name}\" of art \"{art.ID}\" step-by-step got complete sequence \"{found.Value.Name}\"; Current Steps: [{string.Join(", ", currentSteps)}]");
                    });
                }
            }
        }
    }

    private static async Task<(Entity<MartialArtistComponent, MeleeWeaponComponent> Artist, EntityUid Mob)> PrepareMobs(Pair.TestPair pair, EntityCoordinates spawnCoords)
    {
        const string artistId = "MobMartialArtsDummy";
        const string mobId = "MobMartialArtsDummy";
        const string artId = "DummyMartialArt";

        var server = pair.Server;

        var artsSys = server.EntMan.System<MartialArtsSystem>();
        var combatModeSys = server.EntMan.System<CombatModeSystem>();
        var playerMan = server.PlayerMan;

        Entity<MartialArtistComponent, MeleeWeaponComponent> artist = default;
        EntityUid mob = default;
        await server.WaitPost(() =>
        {
            var artistUid = server.EntMan.SpawnAtPosition(artistId, spawnCoords);
            var artistComp = server.EntMan.GetComponent<MartialArtistComponent>(artistUid);
            var meleeComp = server.EntMan.GetComponent<MeleeWeaponComponent>(artistUid);
            combatModeSys.SetInCombatMode(artistUid, true);
            playerMan.SetAttachedEntity(playerMan.Sessions.First(), artistUid, true);
            artist = (artistUid, artistComp, meleeComp);

            mob = server.EntMan.SpawnAtPosition(mobId, spawnCoords);
        });

        await server.WaitRunTicks(1);

        await server.WaitPost(() =>
        {
            Assert.That(artsSys.TryGrantMartialArt(artist, artId, artist: artist.Comp1), "Cannot give martial art to dummy artist");
        });

        await server.WaitRunTicks(1);

        return (artist, mob);
    }

}
