// SS220 Changeling
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Shared.Changeling;
using Content.Shared.Changeling.Mutations;
using Content.Shared.Changeling.Systems;
using Content.Shared.Mind;
using Content.Shared.Store;
using Content.Shared.Store.Events;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.Changeling;

[TestFixture]
public sealed class ChangelingBuiAuthorityTests : GameTest
{
    [Test]
    [PairConfig(nameof(PsDisconnected))]
    public async Task ForeignActorCannotOpenPrivateChangelingInterfaces()
    {
        var pair = Pair;
        var server = pair.Server;
        var entMan = server.EntMan;
        var session = await server.AddDummySession();
        var testMap = await pair.CreateTestMap();
        EntityUid ling = default;
        EntityUid intruder = default;

        await server.WaitAssertion(() =>
        {
            ling = entMan.SpawnEntity("MobLing", testMap.GridCoords);
            intruder = entMan.SpawnEntity("MobHuman", testMap.GridCoords);

            var mindSystem = entMan.System<SharedMindSystem>();
            var mind = mindSystem.CreateMind(null);
            mindSystem.TransferTo(mind, ling, mind: mind.Comp);

            var intruderMind = mindSystem.CreateMind(null);
            mindSystem.TransferTo(intruderMind, intruder, mind: intruderMind.Comp);

            // Store actions require a controlled actor because opening a BUI is session-scoped.
            server.PlayerMan.SetAttachedEntity(session, ling);
        });
        await pair.RunTicksSync(1);

        await server.WaitAssertion(() =>
        {
            var protectedKeys = new Enum[]
            {
                StoreUiKey.Key,
                ChangelingApexTrackerUiKey.Key,
                ChangelingTransformUiKey.Key,
                ChangelingTransformUiKey.TransformationSting,
            };

            foreach (var key in protectedKeys)
            {
                var foreignAttempt = new BoundUserInterfaceMessageAttempt(
                    intruder,
                    ling,
                    key,
                    new OpenBoundInterfaceMessage());
                entMan.EventBus.RaiseLocalEvent(ling, foreignAttempt);
                Assert.That(foreignAttempt.Cancelled, Is.True, $"Foreign actor opened private changeling UI {key}.");

                var ownerAttempt = new BoundUserInterfaceMessageAttempt(
                    ling,
                    ling,
                    key,
                    new OpenBoundInterfaceMessage());
                entMan.EventBus.RaiseLocalEvent(ling, ownerAttempt);
                Assert.That(ownerAttempt.Cancelled, Is.False, $"Owner was denied private changeling UI {key}.");
            }

            var openStore = new IntrinsicStoreActionEvent
            {
                Performer = ling,
            };
            entMan.EventBus.RaiseLocalEvent(ling, openStore);
            var userInterface = entMan.GetComponent<UserInterfaceComponent>(ling);
            var uiSystem = entMan.System<SharedUserInterfaceSystem>();
            Assert.That(
                uiSystem.IsUiOpen((ling, userInterface), StoreUiKey.Key, ling),
                Is.True,
                "The intrinsic store action must still open the owner's store after authority hardening.");
        });
    }

    [Test]
    [PairConfig(nameof(PsDisconnected))]
    public async Task ApexSelectionTokensExpireOnCloseAndReset()
    {
        var pair = Pair;
        var server = pair.Server;
        var entMan = server.EntMan;
        var testMap = await pair.CreateTestMap();
        EntityUid ling = default;
        EntityUid staleTarget = default;

        await server.WaitAssertion(() =>
        {
            ling = entMan.SpawnEntity("MobLing", testMap.GridCoords);
            staleTarget = entMan.SpawnEntity("MobHuman", testMap.GridCoords);
        });
        await pair.RunTicksSync(1);

        await server.WaitAssertion(() =>
        {
            var tracker = entMan.EnsureComponent<ChangelingApexTrackerComponent>(ling);
            var userInterface = entMan.EnsureComponent<UserInterfaceComponent>(ling);
            var uiSystem = entMan.System<SharedUserInterfaceSystem>();
            uiSystem.SetUi(
                (ling, userInterface),
                ChangelingApexTrackerUiKey.Key,
                new InterfaceData("ChangelingApexTrackerBoundUserInterface"));
            uiSystem.OpenUi((ling, userInterface), ChangelingApexTrackerUiKey.Key, ling);

            const uint staleToken = 44;
            tracker.TargetSelectionTokens.Add(staleToken, staleTarget);
            uiSystem.CloseUi((ling, userInterface), ChangelingApexTrackerUiKey.Key, ling);
            Assert.That(tracker.TargetSelectionTokens, Is.Empty, "Closing Apex UI must invalidate its tokens.");

            var staleSelection = new ChangelingApexTargetSelectedMessage(staleToken)
            {
                Actor = ling,
            };
            entMan.EventBus.RaiseLocalEvent(ling, staleSelection);
            Assert.That(tracker.Target, Is.Null, "A stale Apex token must fail closed.");

            tracker.TargetSelectionTokens.Add(45, staleTarget);
            var reset = new ChangelingEvolutionResetEvent(0, 0);
            entMan.EventBus.RaiseLocalEvent(ling, ref reset);
            Assert.Multiple(() =>
            {
                Assert.That(entMan.HasComponent<ChangelingApexTrackerComponent>(ling), Is.False);
                Assert.That(tracker.TargetSelectionTokens, Is.Empty,
                    "Evolution reset/body transfer cleanup must invalidate Apex tokens.");
            });
        });
    }
}
