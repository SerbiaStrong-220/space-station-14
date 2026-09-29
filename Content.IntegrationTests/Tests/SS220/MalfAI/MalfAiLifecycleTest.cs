// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.IntegrationTests.Fixtures.Attributes;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Pair;
using Content.Server.SS220.MalfAI;
using Content.Server.Station.Systems;
using Content.Shared.Actions.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.FixedPoint;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.SS220.MalfAI;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Station.Components;
using Content.Shared.Store.Components;
using Content.Shared.Store;
using Robust.Shared.EntitySerialization;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.SS220.MalfAI;

[TestFixture]
public sealed class MalfAiLifecycleTest : GameTest
{
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

    private static EntityUid FindGrantedAction(TestPair pair, EntityUid performer, string proto)
    {
        var entMan = pair.Server.EntMan;
        foreach (var actionId in entMan.GetComponent<ActionsComponent>(performer).Actions)
        {
            if (entMan.GetComponent<MetaDataComponent>(actionId).EntityPrototype?.ID == proto)
                return actionId;
        }

        Assert.Fail($"Action {proto} was not granted.");
        return EntityUid.Invalid;
    }

    [Test]
    [PairConfig(nameof(PsDisconnected))]
    public async Task MalfStorePinnedToRoleAndRecoveredAfterDestroy()
    {
        var pair = Pair;
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        await server.WaitIdleAsync();

        EntityUid store = EntityUid.Invalid;
        EntityUid mindId = EntityUid.Invalid;

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            MakeStation(pair, map);

            var body = SpawnRoleBody(pair, map);
            server.EntMan.System<SharedMindSystem>().TryGetMind(body, out mindId, out _);
            store = FindStore(pair, mindId);

            var roleSys = entMan.System<SharedRoleSystem>();
            Assert.That(roleSys.MindHasRole<MalfAiRoleComponent>(mindId, out var role), Is.True);
            Assert.That(role.Value.Comp2.Store, Is.EqualTo(store),
                "Store entity is not pinned to the Malf role component.");

            var malf = entMan.System<MalfAiSystem>();
            Assert.That(malf.EnsureStore(mindId).Owner, Is.EqualTo(store),
                "EnsureStore spawned a duplicate store instead of reusing the pinned one.");

            entMan.DeleteEntity(store);
        });

        await server.WaitRunTicks(5);
        await server.WaitIdleAsync();

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            Assert.That(entMan.Deleted(store), Is.True, "Deleted store entity lingered.");

            var malf = entMan.System<MalfAiSystem>();
            Assert.That(malf.TryGetOwnedStore(mindId, out _), Is.False,
                "Destroyed store is still reported as owned.");

            var fresh = malf.EnsureStore(mindId);
            Assert.That(fresh.Owner, Is.Not.EqualTo(store),
                "EnsureStore reused the destroyed store entity.");

            var roleSys = entMan.System<SharedRoleSystem>();
            Assert.That(roleSys.MindHasRole<MalfAiRoleComponent>(mindId, out var role), Is.True);
            Assert.That(role.Value.Comp2.Store, Is.EqualTo(fresh.Owner),
                "Replacement store was not pinned to the Malf role component.");
        });
    }

    [Test]
    [PairConfig(nameof(PsDisconnected))]
    public async Task MalfRoleRemovalDeletesStoreAndActions()
    {
        var pair = Pair;
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        await server.WaitIdleAsync();

        EntityUid store = EntityUid.Invalid;
        EntityUid mindId = EntityUid.Invalid;
        EntityUid action = EntityUid.Invalid;

        await server.WaitAssertion(() =>
        {
            MakeStation(pair, map);

            var body = SpawnRoleBody(pair, map);
            server.EntMan.System<SharedMindSystem>().TryGetMind(body, out mindId, out _);
            store = FindStore(pair, mindId);
            TopUp(pair, store, 200);

            Buy(pair, store, body, MalfAiConstants.DeployTurretListing);
            action = FindGrantedAction(pair, body, "ActionMalfAiDeployTurret");

            server.EntMan.System<SharedRoleSystem>().MindRemoveRole<MalfAiRoleComponent>(mindId);
        });

        await server.WaitRunTicks(5);
        await server.WaitIdleAsync();

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            Assert.That(entMan.Deleted(store), Is.True,
                "Malf store survived role removal.");
            Assert.That(entMan.Deleted(action), Is.True,
                "Purchased action survived role removal.");
            Assert.That(entMan.System<SharedRoleSystem>().MindHasRole<MalfAiRoleComponent>(mindId), Is.False,
                "Malf role survived removal.");
        });
    }

    [Test]
    [PairConfig(nameof(PsDisconnected))]
    public async Task MalfRoleReaddedAfterRemovalGetsFreshStore()
    {
        var pair = Pair;
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        await server.WaitIdleAsync();

        EntityUid oldStore = EntityUid.Invalid;
        EntityUid mindId = EntityUid.Invalid;

        await server.WaitAssertion(() =>
        {
            MakeStation(pair, map);

            var body = SpawnRoleBody(pair, map);
            server.EntMan.System<SharedMindSystem>().TryGetMind(body, out mindId, out _);
            oldStore = FindStore(pair, mindId);

            server.EntMan.System<SharedRoleSystem>().MindRemoveRole<MalfAiRoleComponent>(mindId);
        });

        await server.WaitRunTicks(5);
        await server.WaitIdleAsync();

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            Assert.That(entMan.Deleted(oldStore), Is.True,
                "Old Malf store survived role removal.");

            entMan.System<SharedRoleSystem>().MindAddRole(mindId, "MindRoleMalfAi");

            var fresh = FindStore(pair, mindId);
            Assert.That(fresh, Is.Not.EqualTo(oldStore),
                "Re-added role reused the deleted store.");

            var malf = entMan.System<MalfAiSystem>();
            Assert.That(malf.TryGetOwnedStore(mindId, out var owned), Is.True,
                "Fresh store is not reported as owned.");
            Assert.That(owned.Value.Owner, Is.EqualTo(fresh),
                "Owned store does not match the freshly spawned one.");
        });
    }

    [Test]
    [PairConfig(nameof(PsDisconnected))]
    public async Task MalfMindDeleteLeavesNoRoleOrphans()
    {
        var pair = Pair;
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        await server.WaitIdleAsync();

        await server.WaitAssertion(() =>
        {
            MakeStation(pair, map);

            var body = SpawnRoleBody(pair, map);
            var entMan = server.EntMan;
            entMan.System<SharedMindSystem>().TryGetMind(body, out var mindId, out _);
            var store = FindStore(pair, mindId);
            TopUp(pair, store, 200);
            Buy(pair, store, body, MalfAiConstants.DeployTurretListing);
            FindGrantedAction(pair, body, "ActionMalfAiDeployTurret");

            entMan.DeleteEntity(mindId);
        });

        await server.WaitRunTicks(5);
        await server.WaitIdleAsync();

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;

            var roles = 0;
            var roleQuery = entMan.AllEntityQueryEnumerator<MalfAiRoleComponent>();
            while (roleQuery.MoveNext(out _, out _))
                roles++;
            Assert.That(roles, Is.EqualTo(0),
                "Malf role entity orphaned by mind deletion.");

            var actors = 0;
            var actorQuery = entMan.AllEntityQueryEnumerator<MalfAiActorComponent>();
            while (actorQuery.MoveNext(out _, out _))
                actors++;
            Assert.That(actors, Is.EqualTo(0),
                "Malf actor component orphaned by mind deletion.");

            var stores = 0;
            var storeQuery = entMan.AllEntityQueryEnumerator<StoreComponent>();
            while (storeQuery.MoveNext(out _, out var comp))
            {
                if (comp.CurrencyWhitelist.Contains(MalfAiConstants.CpuCurrency))
                    stores++;
            }
            Assert.That(stores, Is.EqualTo(0),
                "Malf store orphaned by mind deletion.");
        });
    }

    [Test]
    [PairConfig(nameof(PsDisconnected))]
    public async Task MalfRoleShutdownSurvivesDeletedObjective()
    {
        var pair = Pair;
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        await server.WaitIdleAsync();

        await server.WaitAssertion(() =>
        {
            MakeStation(pair, map);

            var body = SpawnRoleBody(pair, map);
            var entMan = server.EntMan;
            var mindSys = entMan.System<SharedMindSystem>();
            mindSys.TryGetMind(body, out var mindId, out var mind);
            var store = FindStore(pair, mindId);
            TopUp(pair, store, 200);

            Assert.That(
                mindSys.TryAddObjective(mindId, mind, "MalfAiHijackShuttleObjective", out var objective),
                Is.True, "Could not add the hijack objective.");
            // During a flush objectives can die before the role; shutdown must tolerate it.
            entMan.DeleteEntity(objective!.Value);

            Assert.DoesNotThrow(() => entMan.DeleteEntity(mindId),
                "Role shutdown threw on an already-deleted objective (breaks restartround flush).");
        });

        await server.WaitRunTicks(5);
        await server.WaitIdleAsync();

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            var roles = 0;
            var roleQuery = entMan.AllEntityQueryEnumerator<MalfAiRoleComponent>();
            while (roleQuery.MoveNext(out _, out _))
                roles++;
            Assert.That(roles, Is.EqualTo(0),
                "Malf role entity orphaned by mind deletion.");
        });
    }

    [Test]
    [PairConfig(nameof(PsDisconnected))]
    public async Task MalfDirectRoleDeleteCleansBody()
    {
        var pair = Pair;
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        await server.WaitIdleAsync();

        await server.WaitAssertion(() =>
        {
            MakeStation(pair, map);

            var body = SpawnRoleBody(pair, map);
            var entMan = server.EntMan;
            entMan.System<SharedMindSystem>().TryGetMind(body, out var mindId, out _);
            var store = FindStore(pair, mindId);
            TopUp(pair, store, 200);
            Buy(pair, store, body, MalfAiConstants.DeployTurretListing);
            FindGrantedAction(pair, body, "ActionMalfAiDeployTurret");

            var roleSys = entMan.System<SharedRoleSystem>();
            Assert.That(roleSys.MindHasRole<MalfAiRoleComponent>(mindId, out var role), Is.True);
            entMan.DeleteEntity(role.Value.Owner);
        });

        await server.WaitRunTicks(5);
        await server.WaitIdleAsync();

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            var actorQuery = entMan.AllEntityQueryEnumerator<MalfAiActorComponent>();
            while (actorQuery.MoveNext(out _, out _))
                Assert.Fail("Malf actor survived direct role deletion.");

            var storeQuery = entMan.AllEntityQueryEnumerator<StoreComponent>();
            while (storeQuery.MoveNext(out _, out var comp))
            {
                if (comp.CurrencyWhitelist.Contains(MalfAiConstants.CpuCurrency))
                    Assert.Fail("Malf store survived direct role deletion.");
            }
        });
    }

    [Test]
    [PairConfig(nameof(PsDisconnected))]
    public async Task MalfSameTickReaddGetsFreshStore()
    {
        var pair = Pair;
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        await server.WaitIdleAsync();

        EntityUid oldStore = EntityUid.Invalid;

        await server.WaitAssertion(() =>
        {
            MakeStation(pair, map);

            var body = SpawnRoleBody(pair, map);
            var entMan = server.EntMan;
            entMan.System<SharedMindSystem>().TryGetMind(body, out var mindId, out _);
            oldStore = FindStore(pair, mindId);

            var roleSys = entMan.System<SharedRoleSystem>();
            roleSys.MindRemoveRole<MalfAiRoleComponent>(mindId);
            roleSys.MindAddRole(mindId, "MindRoleMalfAi");

            // NOTE: FindStore helper scans raw components and still sees the
            // queued old store; the system lookup below is dead-aware.
            var malf = entMan.System<MalfAiSystem>();
            Assert.That(malf.TryGetOwnedStore(mindId, out var owned), Is.True,
                "Re-added role has no owned store.");
            Assert.That(owned.Value.Owner, Is.Not.EqualTo(oldStore),
                "Same-tick re-add resurrected the queued-for-deletion store.");
        });

        await server.WaitRunTicks(5);
        await server.WaitIdleAsync();

        await server.WaitAssertion(() =>
        {
            Assert.That(server.EntMan.Deleted(oldStore), Is.True,
                "Old store lingered after same-tick re-add.");
        });
    }
}
