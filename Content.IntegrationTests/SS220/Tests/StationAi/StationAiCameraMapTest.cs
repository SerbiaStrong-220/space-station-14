// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.Server.Power.Components;
using Content.Server.Silicons.StationAi;
using Content.Server.Station.Systems;
using Content.Server.SS220.Silicons.StationAi.CameraMap;
using Content.Server.SurveillanceCamera;
using Content.Shared.Inventory;
using Content.Shared.Maps;
using Content.Shared.Medical.SuitSensor;
using Content.Shared.Medical.SuitSensors;
using Content.Shared.Silicons.StationAi;
using Content.Shared.SS220.Silicons.StationAi.CameraMap;
using Content.Shared.StationAi;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.SS220.Tests.StationAi;

public sealed class StationAiCameraMapTest : GameTest
{
    private const string TestStation = "Box";

    private EntityUid _ai;
    private EntityUid _core;
    private EntityUid _eye;

    private void SpawnAi(EntityCoordinates coordinates)
    {
        var entMan = Server.EntMan;
        _core = entMan.SpawnEntity("PlayerStationAiEmpty", coordinates);
        _ai = entMan.SpawnInContainerOrDrop("StationAiBrain", _core, StationAiCoreComponent.Container);
        _eye = entMan.GetComponent<StationAiCoreComponent>(_core).RemoteEntity!.Value;
    }

    /// <summary>
    ///     Test maps have no power network, so a camera without a power receiver counts as powered.
    /// </summary>
    private EntityUid SpawnCamera(EntityCoordinates coordinates)
    {
        var camera = Server.EntMan.SpawnEntity("SurveillanceCameraEngineering", coordinates);
        Server.EntMan.RemoveComponent<ApcPowerReceiverComponent>(camera);
        return camera;
    }

    [Test]
    public async Task JumpStartsCooldown()
    {
        var map = await Pair.CreateTestMap();
        EntityUid camera = default;
        await Server.WaitAssertion(() =>
        {
            SpawnAi(map.GridCoords);
            camera = SpawnCamera(map.GridCoords);
            var system = Server.System<StationAiCameraMapSystem>();
            Assert.That(system.TryJump(_ai, camera, out _), Is.True);
            Assert.That(system.TryJump(_ai, camera, out _), Is.False, "Second jump during the cooldown");
        });

        await Pair.RunSeconds(5.1f);
        await Server.WaitAssertion(() =>
        {
            Assert.That(Server.System<StationAiCameraMapSystem>().TryJump(_ai, camera, out _), Is.True);
        });
    }

    [Test]
    public async Task JumpMovesEyeOnlyToValidTargets()
    {
        var map = await Pair.CreateTestMap();
        var otherMap = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var entMan = Server.EntMan;
            var system = Server.System<StationAiCameraMapSystem>();
            SpawnAi(map.GridCoords);
            var camera = SpawnCamera(map.GridCoords);
            var foreignCamera = SpawnCamera(otherMap.GridCoords);
            var origin = new EntityCoordinates(map.Grid, new Vector2(2, 2));
            Server.System<SharedTransformSystem>().SetCoordinates(_eye, origin);

            Assert.That(system.TryJump(_core, camera, out _), Is.False, "Not an AI");
            Assert.That(system.TryJump(_ai, foreignCamera, out _), Is.False, "Camera on another grid");
            Assert.That(system.TryJump(_ai, _core, out _), Is.False, "Not a camera or beacon");

            Server.System<SurveillanceCameraMapSystem>().SetCameraVisibility(camera, false);
            Assert.That(system.TryGetTarget(camera, map.Grid, out _), Is.False, "Hidden cameras are not listed");
            Server.System<SurveillanceCameraMapSystem>().SetCameraVisibility(camera, true);

            var vision = entMan.GetComponent<StationAiVisionComponent>(camera);
            Server.System<StationAiSystem>().SetVisionEnabled((camera, vision), false);
            Assert.That(system.TryGetTarget(camera, map.Grid, out var cut), Is.True, "Cut cameras stay listed");
            Assert.That(cut.Active, Is.False);
            Assert.That(system.TryJump(_ai, camera, out var failure), Is.False);
            Assert.That(failure, Is.EqualTo(StationAiCameraMapFailure.CameraInactive));
            Assert.That(entMan.GetComponent<TransformComponent>(_eye).Coordinates, Is.EqualTo(origin),
                "Refused jumps do not move the eye");

            Server.System<StationAiSystem>().SetVisionEnabled((camera, vision), true);
            Assert.That(system.TryJump(_ai, camera, out _), Is.True, "Refused jumps do not start the cooldown");
            Assert.That(entMan.GetComponent<TransformComponent>(_eye).Coordinates.Position,
                Is.EqualTo(entMan.GetComponent<TransformComponent>(camera).Coordinates.Position));
        });
    }

    [Test]
    public async Task BeaconNeedsWorkingCameraNearby()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var system = Server.System<StationAiCameraMapSystem>();
            SpawnAi(map.GridCoords);
            var beacon = Server.EntMan.SpawnEntity("DefaultStationBeacon", map.GridCoords);

            Assert.That(system.TryGetTarget(beacon, map.Grid, out var alone), Is.True, "Beacon stays on the map");
            Assert.That(alone.Active, Is.False);
            Assert.That(system.TryJump(_ai, beacon, out var failure), Is.False);
            Assert.That(failure, Is.EqualTo(StationAiCameraMapFailure.NoCameraNearby));

            var camera = SpawnCamera(map.GridCoords);
            Server.System<SurveillanceCameraSystem>().SetActive(camera, false);
            Assert.That(system.TryGetTarget(beacon, map.Grid, out var off), Is.True);
            Assert.That(off.Active, Is.False, "A disabled camera does not cover the beacon");

            Server.System<SurveillanceCameraSystem>().SetActive(camera, true);
            Assert.That(system.TryJump(_ai, beacon, out _), Is.True);
        });
    }

    [Test]
    public async Task CrewJumpNeedsCoordinatesAndView()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var entMan = Server.EntMan;
            var system = Server.System<StationAiCameraMapSystem>();
            var sensors = Server.System<SharedSuitSensorSystem>();
            var config = Server.ProtoMan.Index<GameMapPrototype>(TestStation).Stations.Values.First();
            var station = Server.System<StationSystem>().InitializeNewStation(config, new[] { map.Grid.Owner });
            SpawnAi(map.GridCoords);
            SpawnCamera(map.GridCoords);
            var mob = entMan.SpawnEntity("MobHuman", map.GridCoords);
            var suit = entMan.SpawnEntity("ClothingUniformJumpsuitColorGrey", map.GridCoords);
            Assert.That(Server.System<InventorySystem>().TryEquip(mob, suit, "jumpsuit", force: true));
            var sensor = entMan.GetComponent<SuitSensorComponent>(suit);
            Assert.That(sensors.CheckSensorAssignedStation((suit, sensor)));

            sensors.SetSensor((suit, sensor), SuitSensorMode.SensorOff);
            Assert.That(system.TryGetCrew(suit, map.Grid, station, out _, out _), Is.False, "Sensors off");

            sensors.SetSensor((suit, sensor), SuitSensorMode.SensorVitals);
            Assert.That(system.TryGetCrew(suit, map.Grid, station, out var vitals, out _), Is.True);
            Assert.That(vitals.Located, Is.False);
            Assert.That(system.TryJumpToCrew(_ai, suit, out var failure), Is.False);
            Assert.That(failure, Is.EqualTo(StationAiCameraMapFailure.NoCoordinates));

            sensors.SetSensor((suit, sensor), SuitSensorMode.SensorCords);
            Assert.That(system.TryJumpToCrew(_ai, suit, out _), Is.True, "Next to the camera and the core");

            var here = map.GridCoords.Position + new Vector2(0.5f, 0.5f);
            Assert.That(system.IsInView(map.Grid, here), Is.True);
            Assert.That(system.IsInView(map.Grid, here + new Vector2(40, 40)), Is.False, "Static zone");
            Assert.That(system.TryGetCrew(suit, map.Grid, EntityUid.Invalid, out _, out _), Is.False,
                "Sensors of another station are hidden");
        });
    }
}
