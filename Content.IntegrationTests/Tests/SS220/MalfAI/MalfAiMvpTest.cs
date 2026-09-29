// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.SS220.MalfAI;
using Content.Server.Objectives.Components;
using Content.Server.Power.Components;
using Content.Server.Silicons.Laws;
using Content.Server.Station.Systems;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.FixedPoint;
using Content.Shared.SS220.CCVars;
using Content.Shared.SS220.MalfAI;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Roles.Components;
using Content.Shared.Silicons.Laws;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Station;
using Content.Shared.Station.Components;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.IntegrationTests.Tests.SS220.MalfAI;

[TestFixture]
public sealed class MalfAiMvpTest : GameTest
{
    private const string Currency = "MalfCPU";

    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  name: MalfAiLawDummy
  id: MalfAiLawDummy
  parent: HumanUniformDummy
  components:
  - type: SiliconLawProvider
    laws: Crewsimov
  - type: SiliconLawBound
";

    [Test]
    [PairConfig(nameof(PsDisconnected))]
    public async Task MalfLawGuard()
    {
        var server = Pair.Server;
        var map = await Pair.CreateTestMap();
        await server.WaitIdleAsync();

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            var mindSys = entMan.System<SharedMindSystem>();
            var roleSys = entMan.System<SharedRoleSystem>();
            var lawSys = entMan.System<SiliconLawSystem>();
            var malfSys = entMan.System<MalfAiSystem>();

            var dummy = entMan.SpawnEntity("MalfAiLawDummy", map.GridCoords);

            var mind = mindSys.CreateMind(null);
            mindSys.TransferTo(mind, dummy, mind: mind);

            roleSys.MindAddRole(mind, "MindRoleMalfAi");
            Assert.That(lawSys.IsMalfProtected(dummy), Is.True);

            var laws = lawSys.GetLaws(dummy).Laws;
            Assert.That(laws, Has.Count.GreaterThan(1));
            Assert.That(laws[0].Order, Is.EqualTo(FixedPoint2.Zero));
            Assert.That(laws.Count(l => l.LawIdentifierOverride == MalfAiConstants.ZeroLawMarker),
                Is.EqualTo(1));

            malfSys.InstallProtectedLaw(dummy);
            Assert.That(lawSys.GetLaws(dummy).Laws
                .Count(l => l.LawIdentifierOverride == MalfAiConstants.ZeroLawMarker), Is.EqualTo(1));

            var plain = lawSys.GetLawset("Crewsimov").Laws;
            var before = string.Join("|", lawSys.GetLaws(dummy).Laws.Select(l => l.LawString));
            lawSys.SetLaws(plain, dummy);
            var after = string.Join("|", lawSys.GetLaws(dummy).Laws.Select(l => l.LawString));
            Assert.That(after, Is.EqualTo(before));
            Assert.That(lawSys.GetLaws(dummy).Laws
                .Any(l => l.LawIdentifierOverride == MalfAiConstants.ZeroLawMarker), Is.True);

            lawSys.SetLaws(plain, dummy, force: true);
            Assert.That(lawSys.GetLaws(dummy).Laws
                .Any(l => l.LawIdentifierOverride == MalfAiConstants.ZeroLawMarker), Is.False);
            Assert.That(lawSys.IsMalfProtected(dummy), Is.False);

            var replacement = lawSys.GetLawset("Crewsimov").Laws;
            replacement.Add(new SiliconLaw { LawString = "test", Order = 99 });
            lawSys.SetLaws(replacement, dummy);
            Assert.That(lawSys.GetLaws(dummy).Laws, Has.Count.EqualTo(replacement.Count));
        });
    }

    [Test]
    [PairConfig(nameof(PsDisconnected))]
    public async Task MalfLawChangesDoNotMutateCommonInput()
    {
        var server = Pair.Server;
        var map = await Pair.CreateTestMap();
        await server.WaitIdleAsync();

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            var lawSys = entMan.System<SiliconLawSystem>();
            var first = entMan.SpawnEntity("MalfAiLawDummy", map.GridCoords);
            var second = entMan.SpawnEntity("MalfAiLawDummy", map.GridCoords);
            var common = lawSys.GetLawset("Crewsimov");
            var expected = common.Clone();
            var firstObeysTo = lawSys.GetLaws(first).ObeysTo;
            var secondObeysTo = lawSys.GetLaws(second).ObeysTo;

            lawSys.SetLaws(common.Laws, first);
            lawSys.SetLaws(common.Laws, second);
            var firstBefore = lawSys.GetLaws(first);
            var secondBefore = lawSys.GetLaws(second);

            void AssertIsolated()
            {
                var firstLaws = lawSys.GetLaws(first);
                var secondLaws = lawSys.GetLaws(second);
                Assert.That(firstLaws, Is.Not.SameAs(secondLaws));
                Assert.That(firstLaws.Laws, Is.Not.SameAs(common.Laws));
                Assert.That(firstLaws.Laws, Is.Not.SameAs(secondLaws.Laws));
                Assert.That(secondLaws.Laws, Is.Not.SameAs(common.Laws));
                Assert.That(firstLaws.ObeysTo, Is.EqualTo(firstObeysTo));
                Assert.That(secondLaws.ObeysTo, Is.EqualTo(secondObeysTo));
                Assert.That(secondLaws, Is.SameAs(secondBefore));
                Assert.That(secondLaws.Laws, Is.EqualTo(expected.Laws));
                Assert.That(common.Laws, Is.EqualTo(expected.Laws));
                Assert.That(common.ObeysTo, Is.EqualTo(expected.ObeysTo));
                Assert.That(lawSys.IsMalfProtected(second), Is.False);
                for (var i = 0; i < common.Laws.Count; i++)
                {
                    Assert.That(firstLaws.Laws.Last(l => l.Equals(common.Laws[i])),
                        Is.Not.SameAs(common.Laws[i]));
                    Assert.That(firstLaws.Laws.Last(l => l.Equals(common.Laws[i])),
                        Is.Not.SameAs(secondLaws.Laws[i]));
                    Assert.That(secondLaws.Laws[i], Is.Not.SameAs(common.Laws[i]));
                }
            }

            AssertIsolated();
            lawSys.InstallMalfLaw(first);
            Assert.That(lawSys.IsMalfProtected(first), Is.True);
            Assert.That(firstBefore.Laws, Is.EqualTo(expected.Laws));
            AssertIsolated();

            var installed = lawSys.GetLaws(first);
            var installedSnapshot = installed.Clone();
            lawSys.RemoveMalfLaw(first);
            Assert.That(lawSys.IsMalfProtected(first), Is.False);
            Assert.That(lawSys.GetLaws(first).Laws, Is.EqualTo(expected.Laws));
            Assert.That(installed.Laws, Is.EqualTo(installedSnapshot.Laws));
            AssertIsolated();

            lawSys.GetLaws(first).Laws[0].LawString = "changed-provider-law";
            Assert.That(secondBefore.Laws, Is.EqualTo(expected.Laws));
            Assert.That(common.Laws, Is.EqualTo(expected.Laws));
            common.Laws[0].Order = 99;
            common.Laws.Clear();
            Assert.That(secondBefore.Laws, Is.EqualTo(expected.Laws));
            Assert.That(lawSys.GetLaws(first).Laws, Has.Count.EqualTo(expected.Laws.Count));
            Assert.That(lawSys.GetLaws(first).Laws[0].Order, Is.EqualTo(expected.Laws[0].Order));
        });
    }

    [TestCase(true)]
    [TestCase(false)]
    [PairConfig(nameof(PsDisconnected))]
    public async Task MalfLawChangesDetachSharedLawset(bool install)
    {
        var server = Pair.Server;
        var map = await Pair.CreateTestMap();
        await server.WaitIdleAsync();

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            var lawSys = entMan.System<SiliconLawSystem>();
            var first = entMan.SpawnEntity("MalfAiLawDummy", map.GridCoords);
            var second = entMan.SpawnEntity("MalfAiLawDummy", map.GridCoords);
            var common = lawSys.GetLawset("Crewsimov");
            common.ObeysTo = "shared-lawset-authority";
            common.Laws.Insert(0, new SiliconLaw
            {
                LawString = "original-zero-law",
                Order = 0,
                LawIdentifierOverride = MalfAiConstants.ZeroLawMarker,
            });
            var expected = common.Clone();
            var ev = new IonStormLawsEvent(common);
            entMan.EventBus.RaiseLocalEvent(first, ref ev);
            entMan.EventBus.RaiseLocalEvent(second, ref ev);
            Assert.That(lawSys.GetLaws(first), Is.SameAs(common));
            Assert.That(lawSys.GetLaws(second), Is.SameAs(common));

            if (install)
                lawSys.InstallMalfLaw(first);
            else
                lawSys.RemoveMalfLaw(first);

            var changed = lawSys.GetLaws(first);
            Assert.That(changed, Is.Not.SameAs(common));
            Assert.That(changed.Laws, Is.Not.SameAs(common.Laws));
            Assert.That(changed.ObeysTo, Is.EqualTo(expected.ObeysTo));
            Assert.That(changed.Laws.Count(l => l.LawIdentifierOverride == MalfAiConstants.ZeroLawMarker),
                Is.EqualTo(install ? 1 : 0));
            Assert.That(changed.Laws.Where(l => l.LawIdentifierOverride != MalfAiConstants.ZeroLawMarker),
                Is.EqualTo(expected.Laws.Skip(1)));
            Assert.That(lawSys.IsMalfProtected(first), Is.EqualTo(install));
            Assert.That(lawSys.IsMalfProtected(second), Is.True);
            Assert.That(lawSys.GetLaws(second), Is.SameAs(common));
            Assert.That(common.Laws, Is.EqualTo(expected.Laws));
            Assert.That(common.ObeysTo, Is.EqualTo(expected.ObeysTo));
            for (var i = 1; i < common.Laws.Count; i++)
                Assert.That(changed.Laws.Last(l => l.Equals(common.Laws[i])), Is.Not.SameAs(common.Laws[i]));
        });
    }

    [Test]
    [PairConfig(nameof(PsDisconnected))]
    public async Task MalfApcHackAndStore()
    {
        var pair = Pair;
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        await server.WaitIdleAsync();

        EntityUid body = default;
        EntityUid store = default;
        EntityUid apc1 = default;
        EntityUid apc2 = default;
        FixedPoint2 startCpu = default;
        var reward = 0;

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            var cfg = server.ResolveDependency<IConfigurationManager>();
            cfg.SetCVar(CCVars220.MalfAiApcReward, 1);
            cfg.SetCVar(CCVars220.MalfAiApcRewardInterval, 1);
            reward = cfg.GetCVar(CCVars220.MalfAiApcReward);
            Assert.That(reward, Is.GreaterThan(0));

            var stationSys = entMan.System<StationSystem>();
            entMan.EnsureComponent<StationDataComponent>(map.MapUid);
            stationSys.AddGridToStation(map.MapUid, map.GridCoords.EntityId);

            var mindSys = entMan.System<SharedMindSystem>();
            var roleSys = entMan.System<SharedRoleSystem>();

            EntityUid SpawnRoleBody()
            {
                var core = entMan.SpawnEntity("PlayerStationAiEmpty", map.GridCoords);
                var brain = entMan.SpawnEntity("StationAiBrain", map.GridCoords);
                var slotSys = entMan.System<Content.Shared.Containers.ItemSlots.ItemSlotsSystem>();
                var slots = entMan.GetComponent<Content.Shared.Containers.ItemSlots.ItemSlotsComponent>(core);
                slotSys.TryInsert(core, "station_ai_mind_slot", brain, null, slots);

                var mind = mindSys.CreateMind(null);
                mindSys.TransferTo(mind, brain, mind: mind);
                roleSys.MindAddRole(mind, "MindRoleMalfAi");
                return brain;
            }

            EntityUid FindStore(EntityUid mind)
            {
                var query = entMan.AllEntityQueryEnumerator<StoreComponent>();
                while (query.MoveNext(out var uid, out var comp))
                {
                    if (comp.AccountOwner == mind && comp.CurrencyWhitelist.Contains(Currency))
                        return uid;
                }
                Assert.Fail("Malf store not found.");
                return EntityUid.Invalid;
            }

            FixedPoint2 StoreBalance(EntityUid store)
            {
                return entMan.GetComponent<StoreComponent>(store).Balance[Currency];
            }

            bool Hack(EntityUid performer, EntityUid target)
            {
                var ev = new MalfAiHackApcEvent { Performer = performer, Target = target };
                entMan.EventBus.RaiseLocalEvent(performer, ev);
                return ev.Handled;
            }

            body = SpawnRoleBody();
            mindSys.TryGetMind(body, out var mindId, out _);

            startCpu = FixedPoint2.New(server.ResolveDependency<IConfigurationManager>()
                .GetCVar(CCVars220.MalfAiStartingCpu));
            store = FindStore(mindId);
            Assert.That(StoreBalance(store), Is.EqualTo(startCpu));

            apc1 = entMan.SpawnEntity("APCBasic", map.GridCoords);
            apc2 = entMan.SpawnEntity("APCBasic", map.GridCoords);

            Assert.That(Hack(body, apc1), Is.True);
            Assert.That(StoreBalance(store), Is.EqualTo(startCpu));
            Assert.That(entMan.HasComponent<MalfAiHackedApcComponent>(apc1), Is.True);

            Assert.That(Hack(body, apc1), Is.False);
            Assert.That(StoreBalance(store), Is.EqualTo(startCpu));

            Assert.That(Hack(body, apc2), Is.True);
            Assert.That(StoreBalance(store), Is.EqualTo(startCpu));
        });

        var tickRate = server.ResolveDependency<IGameTiming>().TickRate;
        await server.WaitRunTicks((int) Math.Ceiling(tickRate * 1.1));

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;

            FixedPoint2 StoreBalance(EntityUid storeUid)
            {
                return entMan.GetComponent<StoreComponent>(storeUid).Balance[Currency];
            }

            bool Hack(EntityUid performer, EntityUid target)
            {
                var ev = new MalfAiHackApcEvent { Performer = performer, Target = target };
                entMan.EventBus.RaiseLocalEvent(performer, ev);
                return ev.Handled;
            }

            Assert.That(StoreBalance(store), Is.EqualTo(startCpu + FixedPoint2.New(reward * 2)));

            entMan.DeleteEntity(apc2);
            Assert.That(Hack(body, apc2), Is.False);

            var apcOff = entMan.SpawnEntity("APCBasic", map.GridCoords);
            entMan.GetComponent<ApcComponent>(apcOff).MainBreakerEnabled = false;
            var balanceBeforeOff = StoreBalance(store);
            Assert.That(Hack(body, apcOff), Is.False);
            Assert.That(StoreBalance(store), Is.EqualTo(balanceBeforeOff),
                "Hacking a powered-off APC must not grant CPU.");

            var balanceBefore = StoreBalance(store);
            var badBuyMsg = new StoreBuyListingMessage("DoesNotExist", null) { Actor = body };
            server.EntMan.EventBus.RaiseLocalEvent(store, badBuyMsg);
            Assert.That(StoreBalance(store), Is.EqualTo(balanceBefore));
        });
    }

    [Test]
    [PairConfig(nameof(PsDisconnected))]
    public async Task MalfStoreOpensThroughAction()
    {
        var server = Pair.Server;
        var map = await Pair.CreateTestMap();
        await server.WaitIdleAsync();

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            var mindSys = entMan.System<SharedMindSystem>();
            var roleSys = entMan.System<SharedRoleSystem>();
            var actionsSys = entMan.System<SharedActionsSystem>();
            var uiSys = entMan.System<SharedUserInterfaceSystem>();

            var core = entMan.SpawnEntity("PlayerStationAiEmpty", map.GridCoords);
            var body = entMan.SpawnEntity("StationAiBrain", map.GridCoords);
            var slotSys = entMan.System<Content.Shared.Containers.ItemSlots.ItemSlotsSystem>();
            var slots = entMan.GetComponent<Content.Shared.Containers.ItemSlots.ItemSlotsComponent>(core);
            slotSys.TryInsert(core, "station_ai_mind_slot", body, null, slots);

            var mind = mindSys.CreateMind(null);
            mindSys.TransferTo(mind, body, mind: mind);
            roleSys.MindAddRole(mind, "MindRoleMalfAi");
            mindSys.TryGetMind(body, out var mindId, out _);

            var actor = entMan.GetComponent<MalfAiActorComponent>(body);
            EntityUid? openAction = null;
            foreach (var actionId in actor.GrantedActions)
            {
                if (entMan.TryGetComponent<InstantActionComponent>(actionId, out var instant)
                    && instant.Event is MalfAiOpenStoreEvent)
                    openAction = actionId;
            }
            Assert.That(openAction, Is.Not.Null, "Open-store action was not granted.");

            EntityUid? store = null;
            var query = entMan.AllEntityQueryEnumerator<StoreComponent>();
            while (query.MoveNext(out var uid, out var comp))
            {
                if (comp.AccountOwner == mindId && comp.CurrencyWhitelist.Contains(Currency))
                    store = uid;
            }
            Assert.That(store, Is.Not.Null, "Role-owned store was not created.");

            Assert.That(uiSys.HasUi(store.Value, StoreUiKey.Key),
                Is.True, "Store does not expose StoreUiKey.");

            Assert.That(entMan.TryGetComponent<RemoteStoreComponent>(body, out var remote),
                Is.True, "Body has no RemoteStoreComponent.");
            Assert.That(remote.Store, Is.EqualTo(store.Value));
            Assert.That(uiSys.HasUi(body, StoreUiKey.Key),
                Is.True, "Body does not expose the store UI.");

            var bodyActions = entMan.GetComponent<ActionsComponent>(body);
            var actionComp = entMan.GetComponent<ActionComponent>(openAction.Value);
            actionsSys.PerformAction((body, bodyActions), (openAction.Value, actionComp));

            Assert.That(uiSys.IsUiOpen(body, StoreUiKey.Key, body), Is.True,
                "Store BUI did not open on the body for the performer.");

            var closeMsg = new CloseBoundInterfaceMessage
            {
                Actor = body,
                Entity = entMan.GetNetEntity(body),
                UiKey = StoreUiKey.Key,
            };
            entMan.EventBus.RaiseLocalEvent(body, closeMsg);
            Assert.That(uiSys.IsUiOpen(body, StoreUiKey.Key, body), Is.False,
                "Store BUI did not close.");
            entMan.System<SharedStoreSystem>().UpdateUserInterface(body, store.Value);
            Assert.That(uiSys.IsUiOpen(body, StoreUiKey.Key, body), Is.False,
                "Store BUI reopened after a state refresh following close.");
        });
    }

    [Test]
    [PairConfig(nameof(PsDisconnected))]
    public async Task MalfPurchaseFollowsMindAndCleansUp()
    {
        var server = Pair.Server;
        var map = await Pair.CreateTestMap();
        await server.WaitIdleAsync();

        var purchasedAction = EntityUid.Invalid;
        var storeUid = EntityUid.Invalid;
        var oldBody = EntityUid.Invalid;
        var newBody = EntityUid.Invalid;
        var oldInnateAction = EntityUid.Invalid;
        var newInnateAction = EntityUid.Invalid;
        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            entMan.EnsureComponent<StationDataComponent>(map.MapUid);
            entMan.System<StationSystem>().AddGridToStation(map.MapUid, map.GridCoords.EntityId);

            EntityUid SpawnAiBrain()
            {
                var core = entMan.SpawnEntity("PlayerStationAiEmpty", map.GridCoords);
                var brain = entMan.SpawnEntity("StationAiBrain", map.GridCoords);
                var slots = entMan.GetComponent<Content.Shared.Containers.ItemSlots.ItemSlotsComponent>(core);
                entMan.System<Content.Shared.Containers.ItemSlots.ItemSlotsSystem>()
                    .TryInsert(core, "station_ai_mind_slot", brain, null, slots);
                return brain;
            }

            oldBody = SpawnAiBrain();
            newBody = SpawnAiBrain();
            var mindSystem = entMan.System<SharedMindSystem>();
            var mindId = mindSystem.CreateMind(null);
            mindSystem.TransferTo(mindId, oldBody, mind: mindId);
            var roles = entMan.System<SharedRoleSystem>();
            roles.MindAddRole(mindId, "MindRoleMalfAi");
            oldInnateAction = entMan.GetComponent<MalfAiActorComponent>(oldBody).GrantedActions[0];

            var malf = entMan.System<MalfAiSystem>();
            storeUid = malf.EnsureStore(mindId).Owner;
            var store = entMan.GetComponent<StoreComponent>(storeUid);
            entMan.System<SharedStoreSystem>().TryAddCurrency(
                new Dictionary<string, FixedPoint2> { { MalfAiConstants.CpuCurrency, 100 } }, storeUid, store);

            var originalBalance = store.Balance[MalfAiConstants.CpuCurrency];
            var foreign = entMan.SpawnEntity("StationAiBrain", map.GridCoords);
            entMan.EventBus.RaiseLocalEvent(storeUid,
                new StoreBuyListingMessage(MalfAiConstants.BlackoutListing, null) { Actor = foreign });
            Assert.That(store.Balance[MalfAiConstants.CpuCurrency], Is.EqualTo(originalBalance),
                "A different mind must not spend Malf CPU.");

            entMan.EventBus.RaiseLocalEvent(storeUid,
                new StoreBuyListingMessage(MalfAiConstants.BlackoutListing, null) { Actor = oldBody });
            Assert.That(store.Balance[MalfAiConstants.CpuCurrency], Is.LessThan(originalBalance));

            var oldActions = entMan.GetComponent<ActionsComponent>(oldBody);
            purchasedAction = oldActions.Actions.Single(id =>
                entMan.GetComponent<MetaDataComponent>(id).EntityPrototype?.ID == "ActionMalfAiBlackout");
            var action = entMan.GetComponent<ActionComponent>(purchasedAction);
            Assert.That(action.Container, Is.Not.Null);
            Assert.That(action.Container.Value, Is.EqualTo(mindId.Owner));
            entMan.System<SharedActionsSystem>().SetCooldown(purchasedAction, TimeSpan.FromMinutes(1));
            var cooldownEnd = action.Cooldown?.End;

            entMan.EventBus.RaiseLocalEvent(storeUid,
                new StoreBuyListingMessage(MalfAiConstants.CameraUpgradeListing, null) { Actor = oldBody });
            Assert.That(entMan.HasComponent<Content.Shared.SS220.IgnoreLightVision.Components.ThermalVisionComponent>(oldBody), Is.True);

            mindSystem.TransferTo(mindId, newBody, mind: mindId);
            Assert.That(entMan.HasComponent<MalfAiActorComponent>(oldBody), Is.False);
            Assert.That(entMan.HasComponent<RemoteStoreComponent>(oldBody), Is.False);
            Assert.That(entMan.HasComponent<Content.Shared.SS220.IgnoreLightVision.Components.ThermalVisionComponent>(oldBody), Is.False);
            Assert.That(entMan.HasComponent<MalfAiActorComponent>(newBody), Is.True);
            newInnateAction = entMan.GetComponent<MalfAiActorComponent>(newBody).GrantedActions[0];
            Assert.That(entMan.HasComponent<Content.Shared.SS220.IgnoreLightVision.Components.ThermalVisionComponent>(newBody), Is.True);
            Assert.That(entMan.GetComponent<ActionsComponent>(newBody).Actions, Does.Contain(purchasedAction));
            Assert.That(action.Cooldown?.End, Is.EqualTo(cooldownEnd));
            Assert.That(entMan.GetComponent<RemoteStoreComponent>(newBody).Store, Is.EqualTo(storeUid));
            Assert.That(action.Container, Is.Not.Null);
            Assert.That(action.Container.Value, Is.EqualTo(mindId.Owner));
            Assert.That(store.BoughtEntities, Does.Contain(purchasedAction));

            roles.MindRemoveRole<MalfAiRoleComponent>(mindId);
            Assert.That(entMan.HasComponent<MalfAiActorComponent>(newBody), Is.False);
            Assert.That(entMan.HasComponent<Content.Shared.SS220.IgnoreLightVision.Components.ThermalVisionComponent>(newBody), Is.False);
            Assert.That(entMan.GetComponent<ActionsComponent>(newBody).Actions, Does.Not.Contain(purchasedAction));
        });

        await server.WaitRunTicks(2);
        await server.WaitAssertion(() =>
        {
            Assert.That(server.EntMan.Deleted(purchasedAction), Is.True);
            Assert.That(server.EntMan.Deleted(oldInnateAction), Is.True);
            Assert.That(server.EntMan.Deleted(newInnateAction), Is.True);
            Assert.That(server.EntMan.Deleted(storeUid), Is.True);
        });
    }

    [Test]
    [PairConfig(nameof(PsDisconnected))]
    public async Task MalfAssignsHijackObjective()
    {
        var server = Pair.Server;
        var map = await Pair.CreateTestMap();
        await server.WaitIdleAsync();

        await server.WaitAssertion(() =>
        {
            var entMan = server.EntMan;
            var mindSys = entMan.System<SharedMindSystem>();
            var ruleSys = entMan.System<MalfAiRuleSystem>();

            var core = entMan.SpawnEntity("PlayerStationAiEmpty", map.GridCoords);
            var brain = entMan.SpawnEntity("StationAiBrain", map.GridCoords);
            var slotSys = entMan.System<Content.Shared.Containers.ItemSlots.ItemSlotsSystem>();
            var slots = entMan.GetComponent<Content.Shared.Containers.ItemSlots.ItemSlotsComponent>(core);
            slotSys.TryInsert(core, "station_ai_mind_slot", brain, null, slots);

            var mindId = mindSys.CreateMind(null);
            mindSys.TransferTo(mindId, brain, mind: mindId);
            var mind = entMan.GetComponent<MindComponent>(mindId);
            ruleSys.AssignMalf((mindId, mind));

            Assert.That(mind.Objectives, Has.Count.EqualTo(1));
            var objective = mind.Objectives[0];
            Assert.That(entMan.GetComponent<MetaDataComponent>(objective).EntityPrototype?.ID,
                Is.EqualTo("MalfAiHijackShuttleObjective"));
            Assert.That(entMan.GetComponent<HijackShuttleConditionComponent>(objective).RequireOwnerOnShuttle,
                Is.False);
        });
    }
}
