// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Reflection;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Pair;
using Content.Server.AlertLevel;
using Content.Server.Communications;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.RoundEnd;
using Content.Server.SS220.MalfAI;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Systems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.FixedPoint;
using Content.Shared.GameTicking.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Roles;
using Content.Shared.SS220.CCVars;
using Content.Shared.SS220.MalfAI;
using Content.Shared.Shuttles.Components;
using Content.Shared.Station.Components;
using Content.Shared.Store.Components;
using Content.Shared.Store;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.UnitTesting.Pool;

namespace Content.IntegrationTests.Tests.SS220.MalfAI;

[TestFixture]
public sealed class MalfAiDoomsdayTest : GameTest
{
    private static PoolSettings PsMalfAi => new() { Connected = false, Dirty = true };

    private const string StationAlerts = "stationAlerts";

    private static PoolSettings PsDisconnectedLiveRound => new() { Connected = false, DummyTicker = false, Dirty = true };

    private static void MakeStation(TestPair pair, TestMapData map)
    {
        AddStation(pair, map.MapUid, map.GridCoords.EntityId);
    }

    private static EntityUid AddStation(TestPair pair, EntityUid station, EntityUid grid)
    {
        var entMan = pair.Server.EntMan;
        entMan.EnsureComponent<StationDataComponent>(station);
        entMan.System<StationSystem>().AddGridToStation(station, grid);

        var alerts = entMan.EnsureComponent<AlertLevelComponent>(station);
        alerts.AlertLevelPrototype = "stationAlerts";
        alerts.AlertLevels = pair.Server.ProtoMan.Index<AlertLevelPrototype>(StationAlerts);
        alerts.CurrentLevel = "green";
        return station;
    }

    private static EntityUid SpawnRoleBody(TestPair pair, TestMapData map)
    {
        var entMan = pair.Server.EntMan;
        var mindSys = entMan.System<SharedMindSystem>();
        var roleSys = entMan.System<SharedRoleSystem>();

        var core = entMan.SpawnEntity("PlayerStationAiEmpty", map.GridCoords);
        if (entMan.TryGetComponent<ApcPowerReceiverComponent>(core, out var power))
        {
            entMan.System<PowerReceiverSystem>().SetNeedsPower(core, false);
            power.Powered = true;
        }

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

    private static EntityUid? FindArmed(IEntityManager entMan, EntityUid station)
    {
        var query = entMan.AllEntityQueryEnumerator<MalfAiDoomsdayComponent>();
        while (query.MoveNext(out var uid, out var doom))
        {
            if (doom.Station == station && doom.Phase == MalfAiDoomsdayPhase.Armed)
                return uid;
        }
        return null;
    }

    [Test]
    [PairConfig(nameof(PsMalfAi))]
    public async Task MalfDoomsdayArmsAndBlocksShuttle()
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
            entMan.System<SharedMindSystem>().TryGetMind(body, out var mindId, out _);
            var store = FindStore(pair, mindId);
            TopUp(pair, store, 200);

            var station = entMan.System<MalfAiSystem>().ResolvePerformerStation(body);
            Assert.That(station, Is.Not.Null);
            var alerts = entMan.EnsureComponent<AlertLevelComponent>(station.Value);
            if (alerts.AlertLevels == null)
            {
                alerts.AlertLevelPrototype = "stationAlerts";
                alerts.AlertLevels = pair.Server.ProtoMan.Index<AlertLevelPrototype>(StationAlerts);
                alerts.CurrentLevel = "green";
            }

            Assert.That(entMan.System<AlertLevelSystem>().GetLevel(station.Value), Is.EqualTo("green"));

            Buy(pair, store, body, MalfAiConstants.DoomsdayListing);

            var role = FindArmed(entMan, map.MapUid);
            Assert.That(role, Is.Not.Null);
            var doom = entMan.GetComponent<MalfAiDoomsdayComponent>(role.Value);
            Assert.That(doom.Station, Is.EqualTo(station.Value));
            Assert.That(entMan.System<AlertLevelSystem>().GetLevel(station.Value), Is.EqualTo("delta"));
            Assert.That(entMan.GetComponent<AlertLevelComponent>(station.Value).IsLevelLocked, Is.True);

            var afterFirst = entMan.GetComponent<StoreComponent>(store).Balance[MalfAiConstants.CpuCurrency];
            Buy(pair, store, body, MalfAiConstants.DoomsdayListing);
            Assert.That(entMan.GetComponent<StoreComponent>(store).Balance[MalfAiConstants.CpuCurrency],
                Is.EqualTo(afterFirst));

            var console = entMan.SpawnEntity("ComputerComms", map.GridCoords);
            var ev = new CommunicationConsoleCallShuttleAttemptEvent(console,
                entMan.GetComponent<CommunicationsConsoleComponent>(console), body);
            entMan.EventBus.RaiseLocalEvent(console, ref ev, broadcast: true);
            Assert.That(ev.Cancelled, Is.True, "Doomsday did not block the shuttle call.");
        });
    }

    [Test]
    [PairConfig(nameof(PsDisconnectedLiveRound))]
    public async Task MalfDoomsdayVetoesEarlyLaunch()
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
            Assert.That(entMan.System<SharedMindSystem>().TryGetMind(body, out var mindId, out _), Is.True);
            var store = FindStore(pair, mindId);
            TopUp(pair, store, 200);
            Buy(pair, store, body, MalfAiConstants.DoomsdayListing);

            Assert.That(FindArmed(entMan, map.MapUid), Is.Not.Null);

            var emergency = entMan.System<EmergencyShuttleSystem>();
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            typeof(EmergencyShuttleSystem).GetField("_authorizeTime", flags)!.SetValue(emergency, 10f);
            typeof(EmergencyShuttleSystem).GetField("_consoleAccumulator", flags)!.SetValue(emergency, 100f);
            typeof(EmergencyShuttleSystem).GetProperty("EmergencyShuttleArrived", flags)!.SetValue(emergency, true);

            Assert.That(emergency.EarlyLaunch(), Is.False, "Doomsday did not veto the early launch.");
            Assert.That(emergency.EarlyLaunchAuthorized, Is.False);
        });
    }

    [Test]
    [PairConfig(nameof(PsMalfAi))]
    public async Task MalfDoomsdayVetoRollsBackAuthorizationAndAllowsRetry()
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
            Assert.That(entMan.System<SharedMindSystem>().TryGetMind(body, out var mindId, out _), Is.True);
            var store = FindStore(pair, mindId);
            TopUp(pair, store, 200);
            Buy(pair, store, body, MalfAiConstants.DoomsdayListing);

            var rule = FindArmed(entMan, map.MapUid);
            Assert.That(rule, Is.Not.Null);
            var doom = entMan.GetComponent<MalfAiDoomsdayComponent>(rule!.Value);

            var console = entMan.SpawnEntity("ComputerComms", map.GridCoords);
            var consoleComp = entMan.EnsureComponent<EmergencyShuttleConsoleComponent>(console);
            consoleComp.AuthorizationsRequired = 1;

            var emergency = entMan.System<EmergencyShuttleSystem>();
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            typeof(EmergencyShuttleSystem).GetField("_authorizeTime", flags)!.SetValue(emergency, 10f);
            typeof(EmergencyShuttleSystem).GetField("_consoleAccumulator", flags)!.SetValue(emergency, 100f);
            typeof(EmergencyShuttleSystem).GetProperty("EmergencyShuttleArrived", flags)!.SetValue(emergency, true);

            var idCard = entMan.SpawnEntity(null, MapCoordinates.Nullspace);
            consoleComp.AuthorizedEntities[idCard] = "test";
            Assert.That(emergency.EarlyLaunch(), Is.False, "Doomsday did not veto the early launch.");
            Assert.That(consoleComp.AuthorizedEntities, Does.ContainKey(idCard),
                "A veto consumed the authorization.");
            Assert.That(emergency.EarlyLaunchAuthorized, Is.False);

            entMan.DeleteEntity(doom.Core);
            Assert.That(entMan.GetComponent<MalfAiDoomsdayComponent>(rule.Value).Phase,
                Is.EqualTo(MalfAiDoomsdayPhase.Cancelled));

            Assert.That(emergency.EarlyLaunch(), Is.True, "Early launch stayed blocked after doomsday was cancelled.");
            Assert.That(emergency.EarlyLaunchAuthorized, Is.True);
        });
    }

    [Test]
    [PairConfig(nameof(PsMalfAi))]
    public async Task MalfDoomsdayCancelOnCoreDestroyRestoresAlert()
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
            entMan.System<SharedMindSystem>().TryGetMind(body, out var mindId, out _);
            var store = FindStore(pair, mindId);
            TopUp(pair, store, 200);

            var station = entMan.System<MalfAiSystem>().ResolvePerformerStation(body);
            Assert.That(station, Is.Not.Null);
            var alerts = entMan.EnsureComponent<AlertLevelComponent>(station.Value);
            if (alerts.AlertLevels == null)
            {
                alerts.AlertLevelPrototype = "stationAlerts";
                alerts.AlertLevels = pair.Server.ProtoMan.Index<AlertLevelPrototype>(StationAlerts);
                alerts.CurrentLevel = "green";
            }

            Buy(pair, store, body, MalfAiConstants.DoomsdayListing);

            var role = FindArmed(entMan, map.MapUid);
            Assert.That(role, Is.Not.Null);
            var doom = entMan.GetComponent<MalfAiDoomsdayComponent>(role.Value);
            var core = doom.Core;
            var before = entMan.GetComponent<StoreComponent>(store).Balance[MalfAiConstants.CpuCurrency];

            entMan.DeleteEntity(core);

            Assert.That(entMan.GetComponent<MalfAiDoomsdayComponent>(role.Value).Phase, Is.EqualTo(MalfAiDoomsdayPhase.Cancelled),
                "Doomsday stayed armed after core destruction.");
            Assert.That(entMan.HasComponent<EndedGameRuleComponent>(role.Value), Is.True,
                "Cancelled Doomsday rule was not ended.");
            Assert.That(entMan.HasComponent<MalfAiDoomsdayWaveComponent>(role.Value), Is.False,
                "Cancelled Doomsday still spawned a kill-wave.");
            Assert.That(entMan.System<AlertLevelSystem>().GetLevel(station.Value), Is.EqualTo("green"),
                "Doomsday cancel restored Green blindly or left Delta.");
            Assert.That(entMan.GetComponent<StoreComponent>(store).Balance[MalfAiConstants.CpuCurrency],
                Is.EqualTo(before + 130), "Cancelled Doomsday did not refund CPU.");
        });
    }

    [Test]
    [PairConfig(nameof(PsMalfAi))]
    public async Task MalfDoomsdayCancelsWhenRoleRemoved()
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
            Assert.That(entMan.System<SharedMindSystem>().TryGetMind(body, out var mindId, out _));
            var store = FindStore(pair, mindId);
            TopUp(pair, store, 200);

            var station = entMan.System<MalfAiSystem>().ResolvePerformerStation(body);
            Assert.That(station, Is.Not.Null);
            var alerts = entMan.EnsureComponent<AlertLevelComponent>(station.Value);
            if (alerts.AlertLevels == null)
            {
                alerts.AlertLevelPrototype = "stationAlerts";
                alerts.AlertLevels = pair.Server.ProtoMan.Index<AlertLevelPrototype>(StationAlerts);
                alerts.CurrentLevel = "green";
            }

            Buy(pair, store, body, MalfAiConstants.DoomsdayListing);
            var rule = FindArmed(entMan, map.MapUid);
            Assert.That(rule, Is.Not.Null);
            Assert.That(entMan.System<AlertLevelSystem>().GetLevel(station.Value), Is.EqualTo("delta"));

            entMan.System<SharedRoleSystem>().MindRemoveRole<MalfAiRoleComponent>(mindId);

            Assert.That(entMan.GetComponent<MalfAiDoomsdayComponent>(rule.Value).Phase, Is.EqualTo(MalfAiDoomsdayPhase.Cancelled));

            Assert.That(entMan.HasComponent<EndedGameRuleComponent>(rule.Value), Is.True);
            Assert.That(entMan.HasComponent<MalfAiDoomsdayWaveComponent>(rule.Value), Is.False);
            Assert.That(entMan.System<AlertLevelSystem>().GetLevel(station.Value), Is.EqualTo("green"));
        });
    }

    [Test]
    [PairConfig(nameof(PsMalfAi))]
    public async Task MalfDoomsdayKillsOrganicsNotSilicons()
    {
        var pair = Pair;
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        await server.WaitIdleAsync();

        EntityUid? role = null;
        EntityUid? human = null;
        EntityUid? borg = null;
        EntityUid? foreignHuman = null;

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            var cfg = server.ResolveDependency<IConfigurationManager>();
            cfg.SetCVar(CCVars220.MalfAiDoomsdayDuration, 0.2f);
            cfg.SetCVar(CCVars220.MalfAiDoomsdayWaveSpeed, 100f);
            cfg.SetCVar(CCVars220.MalfAiDoomsdayWaveRadius, 4f);

            MakeStation(pair, map);
            var mapSystem = entMan.System<SharedMapSystem>();
            var tileType = map.Tile.Tile.TypeId;
            for (var x = -2; x <= 2; x++)
            {
                for (var y = -2; y <= 2; y++)
                {
                    mapSystem.SetTile(map.Grid.Owner, map.Grid.Comp,
                        new Robust.Shared.Map.EntityCoordinates(map.Grid.Owner, x, y),
                        new Robust.Shared.Map.Tile(tileType));
                }
            }

            var body = SpawnRoleBody(pair, map);
            entMan.System<SharedMindSystem>().TryGetMind(body, out var mindId, out _);
            var store = FindStore(pair, mindId);
            TopUp(pair, store, 200);

            var tileCenter = map.GridCoords.Offset(new System.Numerics.Vector2(0.5f, 0.5f));
            human = entMan.SpawnEntity("MobHuman", tileCenter);
            borg = entMan.SpawnEntity("BorgChassisGeneric",
                map.GridCoords.Offset(new System.Numerics.Vector2(1.5f, 0.5f)));
            var foreignGrid = server.ResolveDependency<IMapManager>().CreateGridEntity(map.MapId);
            foreignHuman = entMan.SpawnEntity("MobHuman", new EntityCoordinates(foreignGrid.Owner, 0, 0));
            entMan.System<SharedTransformSystem>().SetCoordinates(foreignHuman.Value,
                new EntityCoordinates(foreignGrid.Owner, 0, 0));
            Assert.That(entMan.GetComponent<TransformComponent>(foreignHuman.Value).MapID, Is.EqualTo(map.MapId));
            Assert.That(entMan.GetComponent<TransformComponent>(foreignHuman.Value).GridUid,
                Is.EqualTo(foreignGrid.Owner));

            Assert.That(entMan.HasComponent<HumanoidProfileComponent>(human.Value));
            Assert.That(entMan.HasComponent<MobStateComponent>(human.Value));
            Assert.That(entMan.System<MobStateSystem>().IsAlive(human.Value), Is.True,
                "Doomsday test victim must start alive.");
            Assert.That(entMan.GetComponent<TransformComponent>(human.Value).GridUid, Is.EqualTo(map.Grid.Owner));

            Buy(pair, store, body, MalfAiConstants.DoomsdayListing);
            role = FindArmed(entMan, map.MapUid);
            Assert.That(role, Is.Not.Null);
        });

        await server.WaitRunTicks(220);
        await server.WaitIdleAsync();

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            var cfg = server.ResolveDependency<IConfigurationManager>();
            try
            {
                var doom = entMan.GetComponent<MalfAiDoomsdayComponent>(role!.Value);
                Assert.That(doom.Phase, Is.EqualTo(MalfAiDoomsdayPhase.Completed), "Doomsday timer never completed.");
                Assert.That(doom.WaveKilled, Is.GreaterThan(0),

                    "Doomsday wave finished without killing anyone.");
                Assert.That(doom.WaveEntity, Is.EqualTo(EntityUid.Invalid),
                    "Doomsday kill-wave never finished.");

                var mob = entMan.System<MobStateSystem>();
                Assert.That(mob.IsDead(human!.Value), Is.True, "Organic was not killed by Doomsday.");
                Assert.That(mob.IsDead(borg!.Value), Is.False, "Silicon was killed by Doomsday.");
                Assert.That(mob.IsDead(foreignHuman!.Value), Is.True,
                    "A mob on a non-member grid in wave radius survived Doomsday.");
            }
            finally
            {
                cfg.SetCVar(CCVars220.MalfAiDoomsdayDuration, 10f);
                cfg.SetCVar(CCVars220.MalfAiDoomsdayWaveSpeed, 5f);
                cfg.SetCVar(CCVars220.MalfAiDoomsdayWaveRadius, 250f);
            }
        });
    }

    [Test]
    [PairConfig(nameof(PsMalfAi))]
    public async Task MalfDoomsdayHiddenWhenDisabled()
    {
        var pair = Pair;
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        await server.WaitIdleAsync();

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            var cfg = server.ResolveDependency<IConfigurationManager>();
            cfg.SetCVar(CCVars220.MalfAiDoomsdayEnabled, false);
            try
            {
                MakeStation(pair, map);
                var body = SpawnRoleBody(pair, map);
                entMan.System<SharedMindSystem>().TryGetMind(body, out var mindId, out _);
                var store = FindStore(pair, mindId);

                var present = false;
                foreach (var listing in entMan.GetComponent<StoreComponent>(store).FullListingsCatalog)
                {
                    if (listing.ID == MalfAiConstants.DoomsdayListing.Id)
                        present = true;
                }

                Assert.That(present, Is.False, "Doomsday listing stayed in the catalog with the CVar off.");
            }
            finally
            {
                cfg.SetCVar(CCVars220.MalfAiDoomsdayEnabled, true);
            }
        });
    }
}
