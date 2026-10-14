// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.Client.SS220.Silicons.StationAi.UI;
using Content.Server.Power.Components;
using System.Linq;
using Content.Shared.Maps;
using Content.Server.Station.Systems;
using Content.Shared.Inventory;
using Robust.Shared.Prototypes;
using Content.Shared.Medical.SuitSensor;
using Content.Shared.Medical.SuitSensors;
using Content.Server.Silicons.StationAi;
using Content.Server.SS220.Silicons.StationAi;
using Content.Server.SurveillanceCamera;
using Content.Shared.Silicons.StationAi;
using Content.Shared.StationAi;
using Content.Shared.SurveillanceCamera.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Content.Shared.SS220.Silicons.StationAi;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Tests.SS220.Silicons;

public sealed class StationAiCameraMapTest : GameTest
{
    private static readonly ProtoId<GameMapPrototype> BoxMap = "Box";

    [Test]
    public async Task TeleportCooldownExpiresAfterFiveSeconds()
    {
        var map = await Pair.CreateTestMap();
        EntityUid brain = default;
        EntityUid beacon = default;
        await Server.WaitAssertion(() =>
        {
            var em = Server.EntMan;
            var core = em.SpawnEntity("PlayerStationAiEmpty", map.GridCoords);
            brain = em.SpawnInContainerOrDrop("StationAiBrain", core, StationAiCoreComponent.Container);
            beacon = em.SpawnEntity("DefaultStationBeacon", map.GridCoords);
            // Beacons are only destinations next to a working camera.
            var camera = em.SpawnEntity("SurveillanceCameraEngineering", map.GridCoords);
            em.RemoveComponent<ApcPowerReceiverComponent>(camera);
            Assert.That(em.System<StationAiCameraMapSystem>().TryTeleport(brain, beacon), Is.True);
        });
        await Pair.RunSeconds(4);
        await Server.WaitAssertion(() =>
        {
            Assert.That(Server.EntMan.System<StationAiCameraMapSystem>().TryTeleport(brain, beacon), Is.False);
        });
        await Pair.RunSeconds(1.1f);
        await Server.WaitAssertion(() =>
        {
            var system = Server.EntMan.System<StationAiCameraMapSystem>();
            Assert.That(system.TryTeleport(brain, beacon), Is.True);
            Assert.That(system.TryTeleport(brain, beacon), Is.False, "Successful jump restarts the cooldown");
        });
    }

    [Test]
    public async Task ReturnToCoreButtonReplacesJumpAction()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var em = Server.EntMan;
            var system = em.System<StationAiCameraMapSystem>();
            var transform = em.System<SharedTransformSystem>();
            var core = em.SpawnEntity("PlayerStationAiEmpty", map.GridCoords);
            var brain = em.SpawnInContainerOrDrop("StationAiBrain", core, StationAiCoreComponent.Container);
            var eye = em.GetComponent<StationAiCoreComponent>(core).RemoteEntity!.Value;
            transform.SetCoordinates(eye, new EntityCoordinates(map.Grid, new Vector2(3, 3)));

            var now = Server.ResolveDependency<IGameTiming>().CurTime;
            Assert.That(system.TryJumpToCore(brain), Is.True);
            Assert.That(transform.GetMapCoordinates(eye).Position,
                Is.EqualTo(transform.GetMapCoordinates(core).Position), "Eye returns to the core");
            Assert.That(em.GetComponent<StationAiCameraMapComponent>(brain).NextTeleport, Is.GreaterThan(now),
                "Returning to the core shares the camera cooldown");
            Assert.That(system.TryJumpToCore(brain), Is.False, "Second return during the cooldown is refused");
            Assert.That(system.TryJumpToCore(core), Is.False, "Only the AI itself can use it");
        });
    }

    [Test]
    public async Task CrewInViewOnlyNearWorkingCameras()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var em = Server.EntMan;
            var system = em.System<StationAiCameraMapSystem>();
            var camera = em.SpawnEntity("SurveillanceCameraEngineering", map.GridCoords);
            em.RemoveComponent<ApcPowerReceiverComponent>(camera);
            var here = map.GridCoords.Position + new Vector2(0.5f, 0.5f);
            Assert.That(system.IsInView(map.Grid, here), Is.True, "Tile next to a working camera is in view");
            Assert.That(system.IsInView(map.Grid, here + new Vector2(40, 40)), Is.False,
                "Far away, in the static zone, the jump is refused");
            em.System<SurveillanceCameraSystem>().SetActive(camera, false);
            em.System<StationAiSystem>().SetVisionEnabled((camera, em.GetComponent<StationAiVisionComponent>(camera)), false);
            Assert.That(system.IsInView(map.Grid, here), Is.False, "Without vision sources the tile is static");
        });
    }

    [Test]
    public async Task BeaconNeedsWorkingCamera()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var em = Server.EntMan;
            var system = em.System<StationAiCameraMapSystem>();
            var core = em.SpawnEntity("PlayerStationAiEmpty", map.GridCoords);
            var brain = em.SpawnInContainerOrDrop("StationAiBrain", core, StationAiCoreComponent.Container);
            var beacon = em.SpawnEntity("DefaultStationBeacon", map.GridCoords);

            Assert.That(system.TryGetTarget(beacon, map.Grid, out var alone), Is.True, "Beacon stays on the map");
            Assert.That(alone.Active, Is.False, "No camera nearby");
            Assert.That(system.TryTeleport(brain, beacon, out var failure), Is.False);
            Assert.That(failure, Is.EqualTo(StationAiCameraMapFailure.OutOfView));

            var camera = em.SpawnEntity("SurveillanceCameraEngineering", map.GridCoords);
            em.RemoveComponent<ApcPowerReceiverComponent>(camera);
            em.System<SurveillanceCameraSystem>().SetActive(camera, false);
            Assert.That(system.TryGetTarget(beacon, map.Grid, out var offCamera), Is.True);
            Assert.That(offCamera.Active, Is.False, "Disabled camera does not cover the beacon");
            Assert.That(system.TryTeleport(brain, camera, out failure), Is.False);
            Assert.That(failure, Is.EqualTo(StationAiCameraMapFailure.CameraInactive));

            em.System<SurveillanceCameraSystem>().SetActive(camera, true);
            Assert.That(system.TryGetTarget(beacon, map.Grid, out var covered), Is.True);
            Assert.That(covered.Active, Is.True, "Working camera in range covers the beacon");
            Assert.That(system.TryTeleport(brain, beacon), Is.True);
        });
    }

    [Test]
    public async Task CrewListFollowsSensorMode()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var em = Server.EntMan;
            var system = em.System<StationAiCameraMapSystem>();
            var sensors = em.System<SharedSuitSensorSystem>();
            var mob = em.SpawnEntity("MobHuman", map.GridCoords);
            var suit = em.SpawnEntity("ClothingUniformJumpsuitColorGrey", map.GridCoords);
            Assert.That(em.System<InventorySystem>().TryEquip(mob, suit, "jumpsuit", force: true));
            var sensor = em.GetComponent<SuitSensorComponent>(suit);
            // Test maps have no station; turn the test grid into one so the sensor gets assigned.
            var config = Server.ResolveDependency<IPrototypeManager>().Index(BoxMap).Stations.Values.First();
            var station = em.System<StationSystem>().InitializeNewStation(config, new[] { map.Grid.Owner });
            Assert.That(sensors.CheckSensorAssignedStation((suit, sensor)));

            sensors.SetSensor((suit, sensor), SuitSensorMode.SensorOff);
            Assert.That(system.TryGetCrew(suit, map.Grid, station, out _), Is.False, "Sensors off are hidden");
            sensors.SetSensor((suit, sensor), SuitSensorMode.SensorVitals);
            Assert.That(system.TryGetCrew(suit, map.Grid, station, out var vitals), Is.True);
            Assert.That(vitals.Position, Is.Null, "Vitals mode lists them without a location");
            sensors.SetSensor((suit, sensor), SuitSensorMode.SensorCords);
            Assert.That(system.TryGetCrew(suit, map.Grid, station, out var located), Is.True);
            Assert.That(located.Position, Is.Not.Null, "Coordinates mode reports where they are");
            Assert.That(system.TryGetCrew(suit, map.Grid, EntityUid.Invalid, out _), Is.False,
                "Sensors of another station are hidden");
        });
    }

    [Test]
    public async Task TeleportValidatesActorAndLiveTarget()
    {
        var map = await Pair.CreateTestMap();
        var otherMap = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var em = Server.EntMan;
            var system = em.System<StationAiCameraMapSystem>();
            var transform = em.System<SharedTransformSystem>();
            var cameras = em.System<SurveillanceCameraMapSystem>();
            var aiSystem = em.System<StationAiSystem>();
            var core = em.SpawnEntity("PlayerStationAiEmpty", map.GridCoords);
            var brain = em.SpawnInContainerOrDrop("StationAiBrain", core, StationAiCoreComponent.Container);
            var coreComp = em.GetComponent<StationAiCoreComponent>(core);
            Assert.That(coreComp.RemoteEntity, Is.Not.Null);
            var eye = coreComp.RemoteEntity!.Value;
            var beacon = em.SpawnEntity("DefaultStationBeacon", map.GridCoords);
            var foreignBeacon = em.SpawnEntity("DefaultStationBeacon", otherMap.GridCoords);
            var camera = em.SpawnEntity("SurveillanceCameraEngineering", map.GridCoords);
            // The test map has no power network. Remove the receiver for the powered-camera case.
            em.RemoveComponent<ApcPowerReceiverComponent>(camera);
            cameras.UpdateCameraMarker((camera, em.GetComponent<SurveillanceCameraComponent>(camera)));
            var origin = new EntityCoordinates(map.Grid, new Vector2(2, 2));
            transform.SetCoordinates(eye, origin);

            Assert.That(system.TryTeleport(brain, beacon), Is.True, "Enabled station beacon");
            Assert.That(em.GetComponent<TransformComponent>(eye).Coordinates.Position,
                Is.EqualTo(em.GetComponent<TransformComponent>(beacon).Coordinates.Position));
            transform.SetCoordinates(eye, origin);
            var cooldown = em.GetComponent<StationAiCameraMapComponent>(brain);
            var now = Server.ResolveDependency<IGameTiming>().CurTime;
            Assert.That(cooldown.NextTeleport - now, Is.EqualTo(TimeSpan.FromSeconds(5)));
            Assert.That(system.TryTeleport(brain, camera), Is.False, "Beacon and camera share the cooldown");
            Assert.That(em.GetComponent<TransformComponent>(eye).Coordinates, Is.EqualTo(origin));
            // Exercise the exact expiry boundary without advancing unrelated station simulation.
            cooldown.NextTeleport = now;
            Assert.That(system.TryTeleport(brain, camera), Is.True, "Active station camera");
            transform.SetCoordinates(eye, origin);

            cooldown.NextTeleport = now;
            Assert.That(system.TryTeleport(beacon, camera), Is.False, "Non-AI actor");
            Assert.That(system.TryTeleport(brain, foreignBeacon), Is.False, "Foreign grid");
            Assert.That(system.TryTeleport(brain, core), Is.False, "Arbitrary entity");
            cameras.SetCameraVisibility(camera, false);
            Assert.That(system.TryGetTarget(camera, map.Grid, out _), Is.False, "Hidden cameras must not be listed");
            Assert.That(system.TryTeleport(brain, camera), Is.False, "Hidden camera");
            cameras.SetCameraVisibility(camera, true);
            aiSystem.SetVisionEnabled((camera, em.GetComponent<StationAiVisionComponent>(camera)), false);
            Assert.That(system.TryGetTarget(camera, map.Grid, out var offline), Is.True);
            Assert.That(offline.Active, Is.False);
            Assert.That(system.TryTeleport(brain, camera), Is.False, "Cut AI vision wire");
            aiSystem.SetVisionEnabled((camera, em.GetComponent<StationAiVisionComponent>(camera)), true);
            em.System<SurveillanceCameraSystem>().SetActive(camera, false);
            Assert.That(system.TryGetTarget(camera, map.Grid, out offline), Is.True, "Disabled cameras remain listed");
            Assert.That(offline.Active, Is.False);
            Assert.That(system.TryTeleport(brain, camera), Is.False, "Deactivated camera");
            em.System<SurveillanceCameraSystem>().SetActive(camera, true);
            Assert.That(system.TryGetTarget(camera, map.Grid, out var restored), Is.True);
            Assert.That(restored.Active, Is.True);
            Assert.That(restored.Entity, Is.EqualTo(offline.Entity), "Favorites retain the same camera identity");
            em.AddComponent<ApcPowerReceiverComponent>(camera);
            Assert.That(system.TryGetTarget(camera, map.Grid, out offline), Is.True, "Power loss must not remove the camera");
            Assert.That(offline.Active, Is.False);
            Assert.That(system.TryTeleport(brain, camera), Is.False, "Unpowered camera");
            coreComp.Remote = false;
            Assert.That(system.TryTeleport(brain, beacon), Is.False, "Physical projection");
            coreComp.Remote = true;
            em.DeleteEntity(beacon);
            Assert.That(system.TryTeleport(brain, beacon), Is.False, "Deleted target from a stale map");
            Assert.That(cooldown.NextTeleport, Is.EqualTo(now), "Rejected requests do not consume cooldown");
            Assert.That(em.GetComponent<TransformComponent>(eye).Coordinates, Is.EqualTo(origin),
                "Rejected requests must not move the eye");
        });
    }

    [Test]
    [TestCase(1020, 740, false)]
    [TestCase(880, 740, true)]
    [TestCase(900, 1000, true)]
    public void CameraMapLayoutAdaptsToWindowSize(float width, float height, bool portrait)
    {
        var layout = StationAiCameraMapLayout.Calculate(width, height);
        Assert.That(layout.Portrait, Is.EqualTo(portrait));
        if (portrait)
        {
            Assert.That(layout.MapHeight, Is.InRange(200f, height * 0.6f), "Map leaves room for the camera list");
            Assert.That(float.IsNaN(layout.SidebarWidth), "Camera list spans the full width");
        }
        else
        {
            Assert.That(layout.SidebarWidth, Is.InRange(240f, width * 0.45f), "Map keeps most of the width");
            Assert.That(float.IsNaN(layout.MapHeight), "Map fills the full height");
        }
    }
}
