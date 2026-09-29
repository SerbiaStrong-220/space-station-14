// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Linq;
using System.Numerics;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Pair;
using Content.Server.GameTicking;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.SS220.MalfAI;
using Content.Server.Station.Systems;
using Content.Server.Store.Systems;
using Content.Shared.Actions.Components;
using Content.Shared.Actions;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Doors;
using Content.Shared.Electrocution;
using Content.Shared.FixedPoint;
using Content.Shared.Maps;
using Content.Shared.Mind;
using Content.Shared.Roles.Components;
using Content.Shared.Roles;
using Content.Shared.SS220.CCVars;
using Content.Shared.SS220.MalfAI;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Station.Components;
using Content.Shared.Station;
using Content.Shared.Store.Components;
using Content.Shared.Store;
using Content.Shared.Turrets;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Configuration;
using Robust.Shared.EntitySerialization;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.SS220.MalfAI;

[TestFixture]
public sealed class MalfAiDefenseTest : GameTest
{
    private const string BoxMap = "Box";

    private static void MakeStation(TestPair pair, Robust.UnitTesting.Pool.TestMapData map)
    {
        var entMan = pair.Server.EntMan;
        entMan.EnsureComponent<StationDataComponent>(map.MapUid);
        entMan.System<StationSystem>()
            .AddGridToStation(map.MapUid, map.GridCoords.EntityId);
    }

    private static EntityUid SpawnRoleBody(TestPair pair, Robust.UnitTesting.Pool.TestMapData map)
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

    private static void Buy(TestPair pair, EntityUid store, EntityUid buyer, string listing)
    {
        var msg = new StoreBuyListingMessage(listing, null) { Actor = buyer };
        pair.Server.EntMan.EventBus.RaiseLocalEvent(store, msg);
    }

    private static void TopUp(TestPair pair, EntityUid store, int amount)
    {
        var entMan = pair.Server.EntMan;
        entMan.System<SharedStoreSystem>().TryAddCurrency(
            new System.Collections.Generic.Dictionary<string, FixedPoint2> { { MalfAiConstants.CpuCurrency, amount } },
            store, entMan.GetComponent<StoreComponent>(store));
    }

    private static EntityCoordinates FreeDeployTarget(TestPair pair, Robust.UnitTesting.Pool.TestMapData map)
    {
        var entMan = pair.Server.EntMan;
        var mapSys = entMan.System<SharedMapSystem>();
        var tileMan = pair.Server.ResolveDependency<Robust.Shared.Map.ITileDefinitionManager>();
        var floorTile = new Robust.Shared.Map.Tile(tileMan["FloorSteel"].TileId);
        var gridEnt = new Entity<Robust.Shared.Map.Components.MapGridComponent>(
            map.GridCoords.EntityId,
            entMan.GetComponent<Robust.Shared.Map.Components.MapGridComponent>(map.GridCoords.EntityId));
        var originTile = mapSys.LocalToTile(gridEnt.Owner, gridEnt.Comp,
            entMan.GetComponent<TransformComponent>(map.GridCoords.EntityId).Coordinates);
        var target = originTile + new Robust.Shared.Maths.Vector2i(1, 0);
        mapSys.SetTile(gridEnt, target, floorTile);
        return map.GridCoords.Offset(new Vector2(1, 0));
    }

    [Test]
    [PairConfig(nameof(PsDisconnected))]
    public async Task MalfLockdownAppliesAndReverts()
    {
        var pair = Pair;
        var server = pair.Server;

        EntityUid? station = null;
        await server.WaitAssertion(() =>
        {
            Assert.That(server.ProtoMan.TryIndex<GameMapPrototype>(BoxMap, out var mapProto));
            var grids = server.EntMan.System<GameTicker>().LoadGameMap(mapProto,
                out _,
                DeserializationOptions.Default with { InitializeMaps = true });
            var stationSys = server.EntMan.System<SharedStationSystem>();
            station = grids.Select(grid => stationSys.GetOwningStation(grid)).FirstOrDefault(uid => uid != null);
            Assert.That(station, Is.Not.Null);
        });
        await server.WaitRunTicks(5);

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            var stationSys = entMan.System<SharedStationSystem>();

            EntityUid? door = null;
            var doorQuery = entMan.EntityQueryEnumerator<AirlockComponent, DoorBoltComponent>();
            while (doorQuery.MoveNext(out var uid, out _, out var bolts))
            {
                if (bolts.BoltsDown || bolts.BoltWireCut)
                    continue;
                if (!entMan.TryGetComponent<DoorComponent>(uid, out var doorComp)
                    || !entMan.TryGetComponent<ElectrifiedComponent>(uid, out var electrified)
                    || electrified.Enabled
                    || !entMan.TryGetComponent<TransformComponent>(uid, out var xform))
                    continue;
                if (doorComp.State != DoorState.Closed)
                    continue;
                if (stationSys.GetOwningStation(uid, xform) != station)
                    continue;
                if (!entMan.TryGetComponent<ApcPowerReceiverComponent>(uid, out var recv) || !recv.Powered)
                    continue;
                door = uid;
                break;
            }
            Assert.That(door, Is.Not.Null, "No powered closed airlock found on Box.");

            var mindSys = entMan.System<SharedMindSystem>();
            var roleSys = entMan.System<SharedRoleSystem>();

            var core = entMan.SpawnEntity("PlayerStationAiEmpty",
                entMan.GetComponent<TransformComponent>(door.Value).Coordinates);
            var body = entMan.SpawnEntity("StationAiBrain",
                entMan.GetComponent<TransformComponent>(door.Value).Coordinates);
            var slotSys = entMan.System<ItemSlotsSystem>();
            var slots = entMan.GetComponent<ItemSlotsComponent>(core);
            slotSys.TryInsert(core, "station_ai_mind_slot", body, null, slots);

            var mind = mindSys.CreateMind(null);
            mindSys.TransferTo(mind, body, mind: mind);
            roleSys.MindAddRole(mind, "MindRoleMalfAi");
            mindSys.TryGetMind(body, out var mindId, out _);
            var store = FindStore(pair, mindId);
            TopUp(pair, store, 200);

            EntityUid? otherDoor = null;
            var otherQuery = entMan.EntityQueryEnumerator<AirlockComponent, DoorBoltComponent>();
            while (otherQuery.MoveNext(out var uid, out _, out _))
            {
                if (uid == door.Value)
                    continue;
                if (!entMan.TryGetComponent<DoorComponent>(uid, out var dc)
                    || !entMan.TryGetComponent<TransformComponent>(uid, out var xf))
                    continue;
                if (dc.State != DoorState.Closed)
                    continue;
                if (stationSys.GetOwningStation(uid, xf) != station)
                    continue;
                if (!entMan.TryGetComponent<ApcPowerReceiverComponent>(uid, out var recv) || !recv.Powered)
                    continue;
                otherDoor = uid;
                break;
            }
            if (otherDoor is {} other)
            {
                entMan.System<SharedElectrocutionSystem>()
                    .SetElectrified((other, entMan.GetComponent<ElectrifiedComponent>(other)), true);
            }

            Buy(pair, store, body, MalfAiConstants.LockdownListing);

            Assert.That(entMan.GetComponent<DoorBoltComponent>(door.Value).BoltsDown, Is.True);
            Assert.That(entMan.GetComponent<ElectrifiedComponent>(door.Value).Enabled, Is.True);

            EntityUid? rule = null;
            var ruleQuery = entMan.AllEntityQueryEnumerator<MalfAiLockdownComponent>();
            while (ruleQuery.MoveNext(out var uid, out var lockdown))
            {
                if (lockdown.Station == station)
                    rule = uid;
            }
            Assert.That(rule, Is.Not.Null);
            entMan.System<GameTicker>().EndGameRule(rule.Value);

            Assert.That(entMan.GetComponent<DoorBoltComponent>(door.Value).BoltsDown, Is.False);
            Assert.That(entMan.GetComponent<ElectrifiedComponent>(door.Value).Enabled, Is.False);

            if (otherDoor is {} otherKept)
            {
                Assert.That(entMan.GetComponent<ElectrifiedComponent>(otherKept).Enabled, Is.True);
            }
        });
    }

    [Test]
    [PairConfig(nameof(PsDisconnected))]
    public async Task MalfLockdownRepurchasable()
    {
        var pair = Pair;
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        await server.WaitIdleAsync();

        int CountLockdownRules(out EntityUid? activeRule)
        {
            var entMan = server.EntMan;
            var ticker = entMan.System<GameTicker>();
            var count = 0;
            activeRule = null;
            var query = entMan.AllEntityQueryEnumerator<MalfAiLockdownComponent>();
            while (query.MoveNext(out var uid, out _))
            {
                count++;
                if (ticker.IsGameRuleActive(uid))
                    activeRule = uid;
            }
            return count;
        }

        await server.WaitAssertion(() =>
        {
            MakeStation(pair, map);

            var body = SpawnRoleBody(pair, map);
            server.EntMan.System<SharedMindSystem>().TryGetMind(body, out var mindId, out _);
            var store = FindStore(pair, mindId);
            TopUp(pair, store, 200);

            Buy(pair, store, body, MalfAiConstants.LockdownListing);
            var firstCount = CountLockdownRules(out var firstActive);
            Assert.That(firstCount, Is.EqualTo(1),
                "First lockdown purchase did not start the rule.");
            Assert.That(firstActive, Is.Not.Null,
                "First lockdown rule is not active.");

            Buy(pair, store, body, MalfAiConstants.LockdownListing);
            var secondCount = CountLockdownRules(out var secondActive);
            Assert.That(secondCount, Is.EqualTo(1),
                "Second purchase stacked a second lockdown instead of refunding.");
            Assert.That(secondActive, Is.EqualTo(firstActive),
                "Second purchase replaced the active lockdown.");
        });

        await server.WaitRunTicks(5);
        await server.WaitIdleAsync();

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            var query = entMan.AllEntityQueryEnumerator<MalfAiLockdownComponent>();
            while (query.MoveNext(out var uid, out _))
                entMan.System<GameTicker>().EndGameRule(uid);
        });

        await server.WaitRunTicks(5);
        await server.WaitIdleAsync();

        EntityUid? endedRule = null;
        await server.WaitAssertion(() =>
        {
            CountLockdownRules(out endedRule);
            Assert.That(endedRule, Is.Null,
                "Ended lockdown rule is still active.");

            var body = SpawnRoleBody(pair, map);
            server.EntMan.System<SharedMindSystem>().TryGetMind(body, out var mindId, out _);
            var store = FindStore(pair, mindId);
            TopUp(pair, store, 200);

            Buy(pair, store, body, MalfAiConstants.LockdownListing);
            CountLockdownRules(out var repurchased);
            Assert.That(repurchased, Is.Not.Null,
                "Lockdown could not be repurchased after the previous one ended.");
            Assert.That(repurchased, Is.Not.EqualTo(endedRule),
                "Repurchase reactivated the ended rule instead of starting a new one.");
        });
    }

    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(true, true)]
    [PairConfig(nameof(PsDisconnected))]
    public async Task MalfLockdownClosesOpenDoorAndRestoresAfterPowerReturns(bool deleteRule, bool intervene)
    {
        var pair = Pair;
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        EntityUid door = default;
        EntityUid rule = default;

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            MakeStation(pair, map);
            door = entMan.SpawnEntity("Airlock", map.GridCoords);
            entMan.System<SharedAirlockSystem>().SetAutoCloseDelayModifier(
                entMan.GetComponent<AirlockComponent>(door), 1000f);
            entMan.System<PowerReceiverSystem>().SetNeedsPower(door, false);
        });
        await server.WaitRunTicks(60);

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            Assert.That(entMan.GetComponent<ApcPowerReceiverComponent>(door).Powered, Is.True);
            Assert.That(entMan.System<SharedDoorSystem>().TryOpen(door), Is.True);
        });
        await server.WaitRunTicks(90);

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            Assert.That(entMan.GetComponent<DoorComponent>(door).State, Is.EqualTo(DoorState.Open));
            var ticker = entMan.System<GameTicker>();
            rule = ticker.AddGameRule("MalfAiLockdownRule");
            entMan.GetComponent<MalfAiLockdownComponent>(rule).Station = map.MapUid;
            ticker.StartGameRule(rule);
            Assert.That(entMan.GetComponent<DoorComponent>(door).State, Is.EqualTo(DoorState.Closing));
            Assert.That(entMan.GetComponent<DoorBoltComponent>(door).BoltsDown, Is.False);
        });
        await server.WaitRunTicks(90);

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            Assert.That(entMan.GetComponent<DoorComponent>(door).State, Is.EqualTo(DoorState.Closed));
            Assert.That(entMan.GetComponent<DoorBoltComponent>(door).BoltsDown, Is.True);
            Assert.That(entMan.GetComponent<ElectrifiedComponent>(door).Enabled, Is.True);
            entMan.System<PowerReceiverSystem>().SetPowerDisabled(door, true);
        });
        await server.WaitRunTicks(60);

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            Assert.That(entMan.GetComponent<ApcPowerReceiverComponent>(door).Powered, Is.False);
            if (deleteRule)
                entMan.DeleteEntity(rule);
            else
                entMan.System<GameTicker>().EndGameRule(rule);

            Assert.That(entMan.GetComponent<DoorBoltComponent>(door).BoltsDown, Is.True);
            Assert.That(entMan.GetComponent<DoorComponent>(door).State, Is.EqualTo(DoorState.Closed));
            Assert.That(entMan.GetComponent<ElectrifiedComponent>(door).Enabled, Is.False);
            var restore = entMan.GetComponent<MalfAiLockdownRestoreComponent>(door);
            Assert.That(restore.Unbolt, Is.True);
            Assert.That(restore.Open, Is.True);
            if (!deleteRule)
                entMan.DeleteEntity(rule);
        });
        await server.WaitRunTicks(60);

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            Assert.That(entMan.HasComponent<MalfAiLockdownRestoreComponent>(door), Is.True);
            if (intervene)
            {
                var doors = entMan.System<SharedDoorSystem>();
                doors.SetState(door, DoorState.Welded);
                doors.SetState(door, DoorState.Closed);
                Assert.That(entMan.HasComponent<MalfAiLockdownRestoreComponent>(door), Is.False);
            }
            entMan.System<PowerReceiverSystem>().SetPowerDisabled(door, false);
        });
        await server.WaitRunTicks(120);

        if (intervene)
        {
            await server.WaitAssertion(() =>
            {
                var entMan = server.EntMan;
                Assert.That(entMan.GetComponent<ApcPowerReceiverComponent>(door).Powered, Is.True);
                Assert.That(entMan.GetComponent<DoorBoltComponent>(door).BoltsDown, Is.True);
                Assert.That(entMan.GetComponent<DoorComponent>(door).State, Is.EqualTo(DoorState.Closed));
                Assert.That(entMan.HasComponent<MalfAiLockdownRestoreComponent>(door), Is.False);
            });
            return;
        }

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            Assert.That(entMan.GetComponent<ApcPowerReceiverComponent>(door).Powered, Is.True);
            Assert.That(entMan.GetComponent<DoorBoltComponent>(door).BoltsDown, Is.False);
            Assert.That(entMan.GetComponent<DoorComponent>(door).State, Is.EqualTo(DoorState.Open));
            Assert.That(entMan.HasComponent<MalfAiLockdownRestoreComponent>(door), Is.False);
            Assert.That(entMan.System<SharedDoorSystem>().TryClose(door), Is.True);
        });
        await server.WaitRunTicks(90);

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            Assert.That(entMan.GetComponent<DoorComponent>(door).State, Is.EqualTo(DoorState.Closed));
            Assert.That(entMan.System<SharedDoorSystem>().TrySetBoltDown(
                (door, entMan.GetComponent<DoorBoltComponent>(door)), true), Is.True);
            entMan.System<PowerReceiverSystem>().SetPowerDisabled(door, true);
        });
        await server.WaitRunTicks(60);
        await server.WaitAssertion(() =>
        {
            server.EntMan.System<PowerReceiverSystem>().SetPowerDisabled(door, false);
        });
        await server.WaitRunTicks(60);

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            Assert.That(entMan.GetComponent<DoorBoltComponent>(door).BoltsDown, Is.True);
            Assert.That(entMan.GetComponent<DoorComponent>(door).State, Is.EqualTo(DoorState.Closed));
            Assert.That(entMan.HasComponent<MalfAiLockdownRestoreComponent>(door), Is.False);
        });
    }

    [Test]
    [PairConfig(nameof(PsDisconnected))]
    public async Task MalfTurretUpgradeApplies()
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
            TopUp(pair, store, 200);

            var turret = entMan.SpawnEntity("WeaponEnergyTurretAI", map.GridCoords);
            var gunBefore = entMan.GetComponent<GunComponent>(turret).FireRateModified;
            Assert.That(gunBefore, Is.GreaterThan(0));

            Buy(pair, store, body, MalfAiConstants.TurretUpgradeListing);

            var station = entMan.System<SharedStationSystem>().GetOwningStation(turret);
            Assert.That(station, Is.Not.Null);
            Assert.That(entMan.HasComponent<MalfAiTurretUpgradeComponent>(station.Value), Is.True);
            Assert.That(entMan.HasComponent<MalfAiTurretBuffedComponent>(turret), Is.True);

            var gunAfter = entMan.GetComponent<GunComponent>(turret).FireRateModified;
            Assert.That(gunAfter, Is.EqualTo(gunBefore * 2).Within(0.01));

            var turret2 = entMan.SpawnEntity("WeaponEnergyTurretAI", map.GridCoords);
            Assert.That(entMan.HasComponent<MalfAiTurretBuffedComponent>(turret2), Is.True);
            var turret3 = entMan.SpawnEntity("MalfAiDeployableTurret", map.GridCoords);
            Assert.That(entMan.HasComponent<MalfAiTurretBuffedComponent>(turret3), Is.True);
            Assert.That(entMan.GetComponent<BatteryAmmoProviderComponent>(turret3).Prototype.Id,
                Is.EqualTo("BulletEnergyTurretLaser"),
                "Deployable turret did not spawn lethal (FireModes[0] still Disabler).");
        });
    }

    [Test]
    [PairConfig(nameof(PsDisconnected))]
    public async Task MalfDeployTurretPlacesAndSpends()
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
            TopUp(pair, store, 200);

            Buy(pair, store, body, MalfAiConstants.DeployTurretListing);

            EntityUid? deployAction = null;
            foreach (var actionId in entMan.GetComponent<ActionsComponent>(body).Actions)
            {
                if (entMan.GetComponent<MetaDataComponent>(actionId).EntityPrototype?.ID == "ActionMalfAiDeployTurret")
                    deployAction = actionId;
            }
            Assert.That(deployAction, Is.Not.Null, "Deploy action was not granted.");

            var before = 0;
            var turretQuery = entMan.AllEntityQueryEnumerator<DeployableTurretComponent>();
            while (turretQuery.MoveNext(out _, out _))
            {
                before++;
            }

            var deployActionComp = entMan.GetComponent<ActionComponent>(deployAction.Value);
            var deployTarget = FreeDeployTarget(pair, map);
            var ev = new MalfAiDeployTurretEvent
            {
                Performer = body,
                Target = deployTarget,
                Action = new Entity<ActionComponent>(deployAction.Value, deployActionComp),
            };
            entMan.EventBus.RaiseLocalEvent(body, ev);
            Assert.That(ev.Handled, Is.True);

            var after = 0;
            var turretQuery2 = entMan.AllEntityQueryEnumerator<DeployableTurretComponent>();
            EntityUid? spawnedTurret = null;
            while (turretQuery2.MoveNext(out var turretUid, out var turretComp))
            {
                after++;
                if (turretComp.Enabled)
                    spawnedTurret = turretUid;
            }
            Assert.That(after, Is.EqualTo(before + 1));

            Assert.That(spawnedTurret, Is.Not.Null,
                "Deployed turret did not activate (Enabled == false: HTN stays off, turret never fires).");

            Assert.That(entMan.GetComponent<ActionsComponent>(body).Actions, Does.Not.Contain(deployAction.Value));

            var spaceEv = new MalfAiDeployTurretEvent
            {
                Performer = body,
                Target = map.GridCoords.Offset(new Vector2(500, 500)),
            };
            entMan.EventBus.RaiseLocalEvent(body, spaceEv);
            Assert.That(spaceEv.Handled, Is.False);
        });
    }

    [Test]
    [PairConfig(nameof(PsDisconnected))]
    public async Task MalfDeployTurretDeniedOnOccupiedTile()
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
            TopUp(pair, store, 200);

            EntityUid Deploy(EntityUid performer)
            {
                Buy(pair, store, performer, MalfAiConstants.DeployTurretListing);
                EntityUid? granted = null;
                foreach (var actionId in entMan.GetComponent<ActionsComponent>(performer).Actions)
                {
                    if (entMan.GetComponent<MetaDataComponent>(actionId).EntityPrototype?.ID == "ActionMalfAiDeployTurret")
                        granted = actionId;
                }

                Assert.That(granted, Is.Not.Null, "Deploy action was not granted.");
                var ev = new MalfAiDeployTurretEvent
                {
                    Performer = performer,
                    Target = FreeDeployTarget(pair, map),
                    Action = new Entity<ActionComponent>(granted.Value,
                        entMan.GetComponent<ActionComponent>(granted.Value)),
                };
                entMan.EventBus.RaiseLocalEvent(performer, ev);
                return ev.Handled ? granted.Value : EntityUid.Invalid;
            }

            Assert.That(Deploy(body), Is.Not.EqualTo(EntityUid.Invalid),
                "First turret deploy was denied on a free tile.");

            Assert.That(Deploy(body), Is.EqualTo(EntityUid.Invalid),
                "Second turret deploy was allowed on an occupied tile.");
        });

        await server.WaitRunTicks(5);
        await server.WaitIdleAsync();

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            var count = 0;
            var query = entMan.AllEntityQueryEnumerator<DeployableTurretComponent>();
            while (query.MoveNext(out _, out _))
                count++;
            Assert.That(count, Is.EqualTo(1),
                "Second turret was placed on an occupied tile.");
        });
    }

    [Test]
    [PairConfig(nameof(PsDisconnected))]
    public async Task MalfDeployTurretDeniedWhenDisabled()
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
            TopUp(pair, store, 200);
            Buy(pair, store, body, MalfAiConstants.DeployTurretListing);

            server.ResolveDependency<IConfigurationManager>().SetCVar(CCVars220.MalfAiEnabled, false);

            var ev = new MalfAiDeployTurretEvent
            {
                Performer = body,
                Target = FreeDeployTarget(pair, map),
            };
            entMan.EventBus.RaiseLocalEvent(body, ev);

            Assert.That(ev.Handled, Is.False,
                "Turret deployed while MalfAI was disabled by CVar.");

            var count = 0;
            var query = entMan.AllEntityQueryEnumerator<DeployableTurretComponent>();
            while (query.MoveNext(out _, out _))
                count++;
            Assert.That(count, Is.EqualTo(0),
                "Turret entity spawned while MalfAI was disabled by CVar.");
        });
    }
}
