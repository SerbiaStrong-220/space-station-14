// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Pair;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Monitor.Components;
using Content.Server.Atmos.Monitor.Systems;
using Content.Server.Atmos.Piping.Unary.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Emp;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.SS220.MalfAI;
using Content.Server.Station.Systems;
using Content.Shared.Actions.Components;
using Content.Shared.Actions;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.Monitor.Components;
using Content.Shared.Atmos.Monitor;
using Content.Shared.Atmos.Piping.Unary.Components;
using Content.Shared.Atmos;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DeviceNetwork.Events;
using Content.Shared.DeviceNetwork;
using Content.Shared.Emp;
using Content.Shared.FixedPoint;
using Content.Shared.GameTicking.Components;
using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;
using Content.Shared.Mind;
using Content.Shared.Roles.Components;
using Content.Shared.Roles;
using Content.Shared.SS220.IgnoreLightVision.Components;
using Content.Shared.SS220.MalfAI;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Station.Components;
using Content.Shared.Station;
using Content.Shared.StationAi;
using Content.Shared.Store.Components;
using Content.Shared.Store;
using Content.Shared.SurveillanceCamera.Components;
using Content.Shared.Tag;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using Robust.UnitTesting.Pool;

namespace Content.IntegrationTests.Tests.SS220.MalfAI;

[TestFixture]
public sealed class MalfAiStationModulesTest : GameTest
{
    private static PoolSettings PsMalfAi => new() { Connected = false, Dirty = true };

    private static void MakeStation(TestPair pair, TestMapData map)
    {
        var server = pair.Server;
        var entMan = server.EntMan;
        entMan.EnsureComponent<StationDataComponent>(map.MapUid);
        entMan.System<StationSystem>().AddGridToStation(map.MapUid, map.GridCoords.EntityId);
    }

    private static EntityUid SpawnRoleBody(TestPair pair, TestMapData map)
    {
        var entMan = pair.Server.EntMan;
        var mindSys = entMan.System<SharedMindSystem>();
        var roleSys = entMan.System<SharedRoleSystem>();

        var core = entMan.SpawnEntity("PlayerStationAiEmpty", map.GridCoords);
        var brain = entMan.SpawnEntity("StationAiBrain", map.GridCoords);
        var slotSys = entMan.System<ItemSlotsSystem>();
        var slots = entMan.GetComponent<ItemSlotsComponent>(core);
        slotSys.TryInsert(core, "station_ai_mind_slot", brain, null, slots);

        var mind = mindSys.CreateMind(null);
        mindSys.TransferTo(mind, brain, mind: mind);
        roleSys.MindAddRole(mind, "MindRoleMalfAi");
        return brain;
    }

    private static EntityUid FindStore(TestPair pair, EntityUid mind)
    {
        var entMan = pair.Server.EntMan;
        var query = entMan.AllEntityQueryEnumerator<StoreComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.AccountOwner == mind && comp.CurrencyWhitelist.Contains(MalfAiConstants.CpuCurrency))
                return uid;
        }

        Assert.Fail("Malf store not found.");
        return EntityUid.Invalid;
    }

    private static void TopUp(TestPair pair, EntityUid store, int amount)
    {
        var entMan = pair.Server.EntMan;
        entMan.System<SharedStoreSystem>().TryAddCurrency(
            new System.Collections.Generic.Dictionary<string, FixedPoint2> { { MalfAiConstants.CpuCurrency, amount } },
            store, entMan.GetComponent<StoreComponent>(store));
    }

    private static void Buy(TestPair pair, EntityUid store, EntityUid buyer, string listing)
    {
        var msg = new StoreBuyListingMessage(listing, null) { Actor = buyer };
        pair.Server.EntMan.EventBus.RaiseLocalEvent(store, msg);
    }

    private static void ExpandGridForAtmos(TestPair pair, TestMapData map)
    {
        var entMan = pair.Server.EntMan;
        var mapSys = entMan.System<Robust.Shared.GameObjects.SharedMapSystem>();
        var grid = map.GridCoords.EntityId;
        var gridComp = entMan.GetComponent<Robust.Shared.Map.Components.MapGridComponent>(grid);
        var tileType = map.Tile.Tile.TypeId;
        for (var dx = -2; dx <= 2; dx++)
        {
            for (var dy = -2; dy <= 2; dy++)
            {
                mapSys.SetTile(grid, gridComp,
                    new Robust.Shared.Map.EntityCoordinates(grid, dx, dy),
                    new Robust.Shared.Map.Tile(tileType));
            }
        }
    }

    private static void RequireSimulatedAtmos(TestPair pair, TestMapData map)
    {
        var entMan = pair.Server.EntMan;
        Assert.That(
            entMan.HasComponent<GridAtmosphereComponent>(map.GridCoords.EntityId),
            Is.True, "Test grid did not register simulated atmosphere (mass gate).");
    }

    [Test]
    [PairConfig(nameof(PsMalfAi))]
    public async Task MalfBlackoutDirected()
    {
        var pair = Pair;
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        await server.WaitIdleAsync();

        await server.WaitAssertion(() =>
        {
            MakeStation(pair, map);
            ExpandGridForAtmos(pair, map);
        });
        await server.WaitRunTicks(60);
        await server.WaitIdleAsync();

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            RequireSimulatedAtmos(pair, map);

            var body = SpawnRoleBody(pair, map);
            server.EntMan.System<SharedMindSystem>().TryGetMind(body, out var mindId, out _);
            var store = FindStore(pair, mindId);
            TopUp(pair, store, 100);

            var apc = entMan.SpawnEntity("APCBasic", map.GridCoords);

            var light = entMan.SpawnEntity("Poweredlight", map.GridCoords);
            var lightSys = entMan.System<SharedPoweredLightSystem>();
            var bulb = lightSys.GetBulb(light);
            Assert.That(bulb, Is.Not.Null, "Poweredlight spawned without a bulb.");
            Assert.That(entMan.GetComponent<LightBulbComponent>(bulb.Value).State,
                Is.EqualTo(LightBulbState.Normal));

            Buy(pair, store, body, MalfAiConstants.BlackoutListing);

            EntityUid? granted = null;
            if (entMan.TryGetComponent<ActionsComponent>(body, out var actions))
            {
                foreach (var actionId in actions.Actions)
                {
                    if (entMan.GetComponent<MetaDataComponent>(actionId).EntityPrototype?.ID == "ActionMalfAiBlackout")
                        granted = actionId;
                }
            }

            Assert.That(granted, Is.Not.Null, "Blackout action was not granted on purchase.");
            Assert.That(entMan.GetComponent<ActionComponent>(granted.Value).UseDelay,
                Is.EqualTo(TimeSpan.FromSeconds(40)));

            bool Blackout(EntityUid performer, EntityUid target)
            {
                var ev = new MalfAiBlackoutEvent { Performer = performer, Target = target };
                entMan.EventBus.RaiseLocalEvent(performer, ev);
                return ev.Handled;
            }

            var atmosSys = entMan.System<AtmosphereSystem>();
            var mix = atmosSys.GetTileMixture(apc, true);
            Assert.That(mix, Is.Not.Null, "APC tile has no atmosphere even with a simulated grid.");
            mix.AdjustMoles(Gas.Plasma, 50f);
            mix.AdjustMoles(Gas.Oxygen, 100f);

            var actionsSys = entMan.System<SharedActionsSystem>();
            var actionComp = entMan.GetComponent<ActionComponent>(granted.Value);
            actionsSys.PerformAction((body, actions), (granted.Value, actionComp),
                new MalfAiBlackoutEvent { Performer = body, Target = apc }, predicted: false);

            if (entMan.TryGetComponent<GridAtmosphereComponent>(map.GridCoords.EntityId, out var gridAtmos))
                Assert.That(gridAtmos.HotspotTilesCount, Is.GreaterThan(0),
                    "Directed blackout did not ignite the plasma on the APC tile.");

            Assert.That(entMan.GetComponent<LightBulbComponent>(bulb.Value).State,
                Is.EqualTo(LightBulbState.Broken),
                "Directed blackout did not pop the nearby bulb.");

            var cooldown = entMan.GetComponent<ActionComponent>(granted.Value).Cooldown;
            Assert.That(cooldown, Is.Not.Null, "Directed blackout did not start its cooldown.");
            Assert.That(cooldown.Value.End - cooldown.Value.Start, Is.EqualTo(TimeSpan.FromSeconds(40)));

            var apcOff = entMan.SpawnEntity("APCBasic", map.GridCoords);
            entMan.GetComponent<ApcComponent>(apcOff).MainBreakerEnabled = false;
            Assert.That(Blackout(body, apcOff), Is.False,
                "Blackout fired on a powered-off APC.");

            var apcDeleted = entMan.SpawnEntity("APCBasic", map.GridCoords);
            entMan.DeleteEntity(apcDeleted);
            Assert.That(Blackout(body, apcDeleted), Is.False,
                "Blackout fired on a deleted APC.");
        });
    }

    [Test]
    [PairConfig(nameof(PsMalfAi))]
    public async Task MalfThermalJamsAutoButNotManual()
    {
        var pair = Pair;
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        await server.WaitIdleAsync();

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            MakeStation(pair, map);

            var body = SpawnRoleBody(pair, map);
            server.EntMan.System<SharedMindSystem>().TryGetMind(body, out var mindId, out _);
            var store = FindStore(pair, mindId);
            TopUp(pair, store, 100);

            var alarm = entMan.SpawnEntity("FireAlarm", map.GridCoords);
            var firelock = entMan.SpawnEntity("Firelock", map.GridCoords);
            var alarmableSys = entMan.System<AtmosAlarmableSystem>();

            void SendAutoDanger(EntityUid target, string sourceTag)
            {
                var payload = new NetworkPayload
                {
                    [DeviceNetworkConstants.Command] = AtmosAlarmableSystem.AlertCmd,
                    [DeviceNetworkConstants.CmdSetState] = AtmosAlarmType.Danger,
                    [AtmosAlarmableSystem.AlertSource] =
                        new System.Collections.Generic.HashSet<ProtoId<TagPrototype>> { sourceTag },
                    [AtmosAlarmableSystem.AlertTypes] = AtmosMonitorThresholdTypeFlags.Temperature,
                };
                var ev = new DeviceNetworkPacketEvent(0, null, 0, "sensor", target, payload);
                entMan.EventBus.RaiseLocalEvent(target, ev);
            }

            SendAutoDanger(alarm, "AirSensor");
            Assert.That(entMan.GetComponent<AtmosAlarmableComponent>(alarm).LastAlarmState,
                Is.EqualTo(AtmosAlarmType.Danger));
            alarmableSys.Reset(alarm);
            SendAutoDanger(firelock, "AirAlarm");
            Assert.That(entMan.GetComponent<AtmosAlarmableComponent>(firelock).LastAlarmState,
                Is.EqualTo(AtmosAlarmType.Danger));
            alarmableSys.Reset(firelock);

            Buy(pair, store, body, MalfAiConstants.ThermalListing);

            var station = entMan.System<SharedStationSystem>().GetOwningStation(alarm);
            Assert.That(station, Is.Not.Null);
            Assert.That(entMan.HasComponent<MalfAiThermalOverrideComponent>(station.Value), Is.True);

            var afterFirst = entMan.GetComponent<StoreComponent>(store).Balance[MalfAiConstants.CpuCurrency];
            Buy(pair, store, body, MalfAiConstants.ThermalListing);
            Assert.That(entMan.GetComponent<StoreComponent>(store).Balance[MalfAiConstants.CpuCurrency],
                Is.EqualTo(afterFirst));

            SendAutoDanger(alarm, "AirSensor");
            Assert.That(entMan.GetComponent<AtmosAlarmableComponent>(alarm).LastAlarmState,
                Is.EqualTo(AtmosAlarmType.Normal));
            SendAutoDanger(firelock, "AirAlarm");
            Assert.That(entMan.GetComponent<AtmosAlarmableComponent>(firelock).LastAlarmState,
                Is.EqualTo(AtmosAlarmType.Normal),
                "Jammed auto-network still closed a firelock.");

            alarmableSys.ForceAlert(alarm, AtmosAlarmType.Danger);
            Assert.That(entMan.GetComponent<AtmosAlarmableComponent>(alarm).LastAlarmState,
                Is.EqualTo(AtmosAlarmType.Danger));
            SendAutoDanger(firelock, "FireAlarm");
            Assert.That(entMan.GetComponent<AtmosAlarmableComponent>(firelock).LastAlarmState,
                Is.EqualTo(AtmosAlarmType.Danger),
                "Manual fire-alarm click did not reach the firelock.");

            SendAutoDanger(alarm, "AirSensor");
            Assert.That(entMan.GetComponent<AtmosAlarmableComponent>(alarm).LastAlarmState,
                Is.EqualTo(AtmosAlarmType.Danger),
                "A jammed network packet overwrote the manual alert.");

            alarmableSys.Reset(alarm);
            Assert.That(entMan.GetComponent<AtmosAlarmableComponent>(alarm).LastAlarmState,
                Is.EqualTo(AtmosAlarmType.Normal),
                "Manual reset did not clear a jammed fire alarm.");
        });
    }

    [Test]
    [PairConfig(nameof(PsMalfAi))]
    public async Task MalfFloodGate()
    {
        var pair = Pair;
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        await server.WaitIdleAsync();

        EntityUid alarm = default;
        EntityUid vent = default;
        EntityUid body = default;
        EntityUid store = default;
        EntityUid? floodAction = null;

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            MakeStation(pair, map);

            vent = entMan.SpawnEntity("GasVentPump", map.GridCoords);
            alarm = entMan.SpawnEntity("AirAlarm", map.GridCoords);
            var airSys = entMan.System<AirAlarmSystem>();

            airSys.SetMode(alarm, string.Empty, AirAlarmMode.Flood, false);
            Assert.That(entMan.GetComponent<AirAlarmComponent>(alarm).CurrentMode,
                Is.EqualTo(AirAlarmMode.Filtering));

            var powerSys = entMan.System<PowerReceiverSystem>();
            powerSys.SetNeedsPower(alarm, false);
            powerSys.SetNeedsPower(vent, false);
            entMan.System<DeviceListSystem>()
                .UpdateDeviceList(alarm, new[] { vent });
        });

        await server.WaitRunTicks(30);
        await server.WaitIdleAsync();

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            Assert.That(entMan.GetComponent<AirAlarmComponent>(alarm).VentData, Is.Not.Empty,
                "Test vent did not register on the air alarm device network.");

            body = SpawnRoleBody(pair, map);
            server.EntMan.System<SharedMindSystem>().TryGetMind(body, out var mindId, out _);
            store = FindStore(pair, mindId);
            TopUp(pair, store, 100);
            Buy(pair, store, body, MalfAiConstants.FloodListing);

            var actionsComp = entMan.GetComponent<ActionsComponent>(body);
            foreach (var actionId in actionsComp.Actions)
            {
                if (entMan.GetComponent<MetaDataComponent>(actionId).EntityPrototype?.ID == "ActionMalfAiAirFlood")
                    floodAction = actionId;
            }

            Assert.That(floodAction, Is.Not.Null, "Flood action was not granted on purchase.");
            Assert.That(entMan.GetComponent<ActionComponent>(floodAction.Value).UseDelay,
                Is.EqualTo(TimeSpan.FromMinutes(10)));

            var actionsSys = entMan.System<SharedActionsSystem>();
            var actionComp = entMan.GetComponent<ActionComponent>(floodAction.Value);
            actionsSys.PerformAction((body, actionsComp), (floodAction.Value, actionComp),
                new MalfAiAirFloodEvent { Performer = body }, predicted: false);

            Assert.That(entMan.GetComponent<AirAlarmComponent>(alarm).CurrentMode,
                Is.EqualTo(AirAlarmMode.Flood));
        });

        await server.WaitRunTicks(10);
        await server.WaitIdleAsync();

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            var floodedVent = entMan.GetComponent<GasVentPumpComponent>(vent);
            Assert.That(floodedVent.PressureLockoutOverride, Is.True,
                "Flood did not lift vent under-pressure lockout.");
            Assert.That(floodedVent.ExternalPressureBound, Is.EqualTo(500f),
                "Flood did not set vents to 500 kPa.");

            var cooldown = entMan.GetComponent<ActionComponent>(floodAction.Value).Cooldown;
            Assert.That(cooldown, Is.Not.Null, "Flood action did not start its cooldown.");
            Assert.That(cooldown.Value.End - cooldown.Value.Start, Is.EqualTo(TimeSpan.FromMinutes(10)));

            var afterFirst = entMan.GetComponent<StoreComponent>(store).Balance[MalfAiConstants.CpuCurrency];
            Buy(pair, store, body, MalfAiConstants.FloodListing);
            Assert.That(entMan.GetComponent<StoreComponent>(store).Balance[MalfAiConstants.CpuCurrency],
                Is.EqualTo(afterFirst));
        });
    }

    [Test]
    [PairConfig(nameof(PsMalfAi))]
    public async Task MalfSilentRecordsMutesRadio()
    {
        var pair = Pair;
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        await server.WaitIdleAsync();

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            MakeStation(pair, map);

            var body = SpawnRoleBody(pair, map);
            var mindSys = entMan.System<SharedMindSystem>();
            var roleSys = entMan.System<SharedRoleSystem>();
            Assert.That(mindSys.TryGetMind(body, out var mindId, out _));
            var store = FindStore(pair, mindId);
            TopUp(pair, store, 100);

            Assert.That(roleSys.MindHasRole<MalfAiRoleComponent>(mindId, out var role));
            Assert.That(role.Value.Comp2.SilentCriminalRecords, Is.False);

            Buy(pair, store, body, MalfAiConstants.SilentRecordsListing);
            Assert.That(role.Value.Comp2.SilentCriminalRecords, Is.True,
                "Silent Records purchase did not mute Security radio reports.");

            var afterFirst = entMan.GetComponent<StoreComponent>(store).Balance[MalfAiConstants.CpuCurrency];
            Buy(pair, store, body, MalfAiConstants.SilentRecordsListing);
            Assert.That(entMan.GetComponent<StoreComponent>(store).Balance[MalfAiConstants.CpuCurrency],
                Is.EqualTo(afterFirst));
        });
    }

    [Test]
    [PairConfig(nameof(PsMalfAi))]
    public async Task MalfCameraUpgradeOverclocksAndSees()
    {
        var pair = Pair;
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        await server.WaitIdleAsync();

        var bodyUid = EntityUid.Invalid;
        var mindUid = EntityUid.Invalid;
        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            MakeStation(pair, map);

            var body = SpawnRoleBody(pair, map);
            bodyUid = body;
            server.EntMan.System<SharedMindSystem>().TryGetMind(body, out var mindId, out _);
            mindUid = mindId;
            var store = FindStore(pair, mindId);
            TopUp(pair, store, 200);

            var cam = entMan.SpawnEntity("SurveillanceCameraSecurity", map.GridCoords);

            Buy(pair, store, body, MalfAiConstants.CameraUpgradeListing);

            var station = entMan.System<SharedStationSystem>().GetOwningStation(body);
            Assert.That(station, Is.Not.Null);
            Assert.That(entMan.HasComponent<MalfAiCameraUpgradeComponent>(station.Value), Is.True);

            Assert.That(
                entMan.TryGetComponent<StationAiVisionComponent>(cam, out var vision));
            Assert.That(vision!.NeedsPower, Is.False, "Camera still needs power.");
            Assert.That(vision!.Occluded, Is.False, "Camera is still occluded by walls.");
            Assert.That(vision!.Range, Is.GreaterThan(7.5f), "Camera range was not extended.");

            Assert.That(
                entMan.TryGetComponent<ThermalVisionComponent>(body, out var thermal));
            Assert.That(thermal!.State,
                Is.EqualTo(IgnoreLightVisionOverlayState.Half));

            var cam2 = entMan.SpawnEntity("SurveillanceCameraSecurity", map.GridCoords);
            Assert.That(
                entMan.TryGetComponent<StationAiVisionComponent>(cam2, out var vision2));
            Assert.That(vision2!.Occluded, Is.False,
                "Late-built camera was not overclocked.");

            Assert.That(entMan.TryGetComponent<ApcPowerReceiverComponent>(
                cam2, out var camRecv));
            Assert.That(camRecv!.NeedsPower, Is.False,
                "Overclocked camera still depends on APC power.");
            Assert.That(entMan.GetComponent<SurveillanceCameraComponent>(cam2).Active, Is.True,
                "Overclocked camera is not active.");

            var coreUid = EntityUid.Invalid;
            if (entMan.System<SharedStationAiSystem>().TryGetCore(body, out var core))
                coreUid = core.Owner;

            if (entMan.TryGetComponent<ApcPowerReceiverComponent>(
                    coreUid, out var coreRecv))
            {
                Assert.That(coreRecv.NeedsPower, Is.True,
                    "Camera Upgrade stripped the AI core's power dependency.");
            }

            var pad = entMan.SpawnEntity("Holopad", map.GridCoords);
            Assert.That(
                entMan.TryGetComponent<StationAiVisionComponent>(pad, out var padVision));
            Assert.That(padVision!.Occluded, Is.False,
                "Holopad vision was not overclocked.");
            Assert.That(padVision!.Range, Is.EqualTo(12f),
                "Holopad vision range was not extended.");

            var holo = entMan.SpawnEntity("StationAiHoloLocal", map.GridCoords);
            Assert.That(
                entMan.TryGetComponent<StationAiVisionComponent>(holo, out var holoVision));
            Assert.That(holoVision!.Occluded, Is.False,
                "AI hologram vision was not overclocked.");
            Assert.That(holoVision!.Range, Is.EqualTo(20f),
                "Camera Upgrade reduced the AI hologram vision range.");

            var afterFirst = entMan.GetComponent<StoreComponent>(store).Balance[MalfAiConstants.CpuCurrency];
            Buy(pair, store, body, MalfAiConstants.CameraUpgradeListing);
            Assert.That(entMan.GetComponent<StoreComponent>(store).Balance[MalfAiConstants.CpuCurrency],
                Is.EqualTo(afterFirst));

            entMan.System<SharedRoleSystem>().MindRemoveRole<MalfAiRoleComponent>(mindId);
        });

        await server.WaitIdleAsync();
        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            Assert.That(entMan.System<SharedRoleSystem>().MindHasRole<MalfAiRoleComponent>(mindUid),
                Is.False, "Malf role still on mind after MindRemoveRole.");
            Assert.That(entMan.HasComponent<MalfAiActorComponent>(bodyUid),
                Is.False, "Actor comp survived Malf role removal.");
            Assert.That(
                entMan.HasComponent<ThermalVisionComponent>(bodyUid),
                Is.False, "Thermal vision lingered on the body after Malf role removal.");
            Assert.That(mindUid, Is.Not.EqualTo(EntityUid.Invalid));
        });
    }

    [Test]
    [PairConfig(nameof(PsMalfAi))]
    public async Task MalfCameraUpgradeSeesOnGridOffsetFromOrigin()
    {
        var pair = Pair;
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        await server.WaitIdleAsync();

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            var xformSys = entMan.System<SharedTransformSystem>();

            xformSys.SetWorldPosition(map.Grid.Owner, new System.Numerics.Vector2(512f, -384f));

            MakeStation(pair, map);

            var body = SpawnRoleBody(pair, map);
            Assert.That(entMan.System<SharedMindSystem>().TryGetMind(body, out var mindId, out _));
            var store = FindStore(pair, mindId);
            TopUp(pair, store, 200);

            var cam = entMan.SpawnEntity("SurveillanceCameraSecurity", map.GridCoords);
            Buy(pair, store, body, MalfAiConstants.CameraUpgradeListing);

            Assert.That(entMan.System<SharedStationAiSystem>().TryGetCore(body, out var core), Is.True);
            entMan.RemoveComponent<StationAiVisionComponent>(core.Owner);

            Assert.That(entMan.GetComponent<StationAiVisionComponent>(cam).Occluded,
                Is.False, "Camera was not overclocked; the vision assertion would prove nothing.");

            var mapSys = entMan.System<SharedMapSystem>();
            var visionSystem = entMan.System<StationAiVisionSystem>();
            var camTile = mapSys.LocalToTile(map.Grid.Owner, map.Grid.Comp,
                entMan.GetComponent<TransformComponent>(cam).Coordinates);
            var visible = new HashSet<Vector2i>();
            visionSystem.GetView(
                (map.Grid.Owner,
                    entMan.GetComponent<Robust.Shared.Physics.BroadphaseComponent>(map.Grid.Owner),
                    map.Grid.Comp),
                new Box2Rotated(Box2.CenteredAround(xformSys.GetWorldPosition(cam),
                    new System.Numerics.Vector2(16f, 16f))),
                visible,
                expansionSize: visionSystem.GetExpansionSize(map.Grid.Owner));

            Assert.That(visible, Does.Contain(camTile),
                "Overclocked camera did not reveal its own tile on a grid offset from the world origin "
                + "(x-ray branch is resolving the seed in the wrong coordinate space).");

            Assert.That(entMan.System<EmpSystem>()
                .DoEmpEffects(cam, 1000f, TimeSpan.FromSeconds(10)), Is.True);
            Assert.That(entMan.HasComponent<EmpDisabledComponent>(cam), Is.True);

            visible.Clear();
            visionSystem.GetView(
                (map.Grid.Owner,
                    entMan.GetComponent<Robust.Shared.Physics.BroadphaseComponent>(map.Grid.Owner),
                    map.Grid.Comp),
                new Box2Rotated(Box2.CenteredAround(xformSys.GetWorldPosition(cam),
                    new System.Numerics.Vector2(16f, 16f))),
                visible,
                expansionSize: visionSystem.GetExpansionSize(map.Grid.Owner));
            Assert.That(visible, Does.Not.Contain(camTile),
                "EMP-disabled camera still grants AI vision after Camera Upgrade.");
        });
    }

    [Test]
    [PairConfig(nameof(PsMalfAi))]
    public async Task MalfChaosPulseStartsEvent()
    {
        var pair = Pair;
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        await server.WaitIdleAsync();

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            MakeStation(pair, map);

            var body = SpawnRoleBody(pair, map);
            server.EntMan.System<SharedMindSystem>().TryGetMind(body, out var mindId, out _);
            var store = FindStore(pair, mindId);
            TopUp(pair, store, 100);
            Buy(pair, store, body, MalfAiConstants.ChaosPulseListing);

            EntityUid? chaosAction = null;
            var actionsComp = entMan.GetComponent<ActionsComponent>(body);
            foreach (var actionId in actionsComp.Actions)
            {
                if (entMan.GetComponent<MetaDataComponent>(actionId).EntityPrototype?.ID == "ActionMalfAiChaosPulse")
                    chaosAction = actionId;
            }

            Assert.That(chaosAction, Is.Not.Null, "Chaos Pulse action was not granted on purchase.");
            Assert.That(entMan.GetComponent<ActionComponent>(chaosAction.Value).UseDelay,
                Is.EqualTo(TimeSpan.FromMinutes(3)));

            var pool = entMan.GetComponent<MalfAiChaosPulseComponent>(chaosAction.Value);
            Assert.That(pool.Events, Is.Not.Empty);
            foreach (var entry in pool.Events)
            {
                Assert.That(entry.Event.Id,
                    Does.Not.Contain("MeteorSwarm").And.Not.Contain("ImmovableRod").And.Not.Contain("MindShieldCombustion"),
                    "Chaos pool contains a lethal event by default.");
            }

            var before = 0;
            var existing = entMan.AllEntityQueryEnumerator<GameRuleComponent>();
            while (existing.MoveNext(out _, out _))
                before++;

            var actionsSys = entMan.System<SharedActionsSystem>();
            var actionComp = entMan.GetComponent<ActionComponent>(chaosAction.Value);
            actionsSys.PerformAction((body, actionsComp), (chaosAction.Value, actionComp),
                new MalfAiChaosPulseEvent { Performer = body }, predicted: false);

            var after = 0;
            var query = entMan.AllEntityQueryEnumerator<GameRuleComponent>();
            while (query.MoveNext(out _, out _))
                after++;

            Assert.That(after, Is.GreaterThan(before), "Chaos Pulse did not start a game rule.");

            var cooldown = entMan.GetComponent<ActionComponent>(chaosAction.Value).Cooldown;
            Assert.That(cooldown, Is.Not.Null, "Chaos Pulse did not start its cooldown.");
            Assert.That(cooldown.Value.End - cooldown.Value.Start, Is.EqualTo(TimeSpan.FromMinutes(3)));
        });
    }
}
