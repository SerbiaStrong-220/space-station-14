// SS220 Changeling
using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.Changeling.Components;
using Content.Server.Changeling.Systems;
using Content.Shared.Alert;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Body;
using Content.Shared.Changeling;
using Content.Shared.Changeling.Components;
using Content.Shared.Changeling.Mutations;
using Content.Shared.Clothing.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Execution;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.Kitchen.Components;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Prying.Components;
using Content.Shared.SS220.AltBlocking;
using Content.Shared.Store;
using Content.Shared.Store.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Changeling;

[TestFixture]
public sealed class ChangelingCoreTests : GameTest
{
    private static readonly ProtoId<CurrencyPrototype> EvolutionCurrency = "ChangelingEvolution";
    private static readonly EntProtoId SwapFormsAction = "ActionChangelingSwapForms";
    private static readonly EntProtoId LastResortAction = "ActionChangelingLastResort";
    private static readonly EntProtoId ArmBladeAction = "ActionChangelingArmBlade";
    private static readonly EntProtoId OrganicShieldAction = "ActionChangelingOrganicShield";
    private static readonly EntProtoId ChitinousArmorAction = "ActionChangelingChitinousArmor";
    private static readonly EntProtoId CryogenicStingAction = "ActionChangelingCryogenicSting";
    private static readonly ProtoId<OrganCategoryPrototype> LungsCategory = "Lungs";
    private static readonly ProtoId<OrganCategoryPrototype> HeartCategory = "Heart";

    [Test]
    [PairConfig(nameof(PsDisconnected))]
    public async Task MobLingStartsWithResourcesStoreAndCoreActions()
    {
        var pair = Pair;
        var server = pair.Server;
        var entMan = server.EntMan;
        var testMap = await pair.CreateTestMap();
        EntityUid ling = default;

        await server.WaitAssertion(() =>
        {
            ling = entMan.SpawnEntity("MobLing", testMap.GridCoords);
        });
        await pair.RunTicksSync(1);

        await server.WaitAssertion(() =>
        {
            var resources = entMan.GetComponent<ChangelingResourceComponent>(ling);
            Assert.Multiple(() =>
            {
                Assert.That(resources.Chemicals, Is.EqualTo(FixedPoint2.New(75)));
                Assert.That(resources.MaxChemicals, Is.EqualTo(FixedPoint2.New(75)));
                Assert.That(resources.EvolutionPoints, Is.EqualTo(20));
                Assert.That(resources.MaxEvolutionPoints, Is.EqualTo(20));
                Assert.That(resources.ChemicalRegenerationAmount, Is.EqualTo(FixedPoint2.New(1)));
                Assert.That(resources.ChemicalRegenerationInterval, Is.EqualTo(TimeSpan.FromSeconds(2)));
            });

            var store = entMan.GetComponent<StoreComponent>(ling);
            Assert.Multiple(() =>
            {
                Assert.That(store.CurrencyWhitelist, Does.Contain(EvolutionCurrency));
                Assert.That(store.Balance[EvolutionCurrency], Is.EqualTo(FixedPoint2.New(20)));
                Assert.That(store.FullListingsCatalog, Is.Not.Empty);
            });

            var actions = entMan.System<SharedActionsSystem>()
                .GetActions(ling)
                .Select(action => entMan.GetComponent<MetaDataComponent>(action).EntityPrototype?.ID)
                .Where(id => id != null)
                .ToHashSet();

            Assert.Multiple(() =>
            {
                Assert.That(actions, Does.Contain("ActionChangelingStore"));
                Assert.That(actions, Does.Contain("ActionChangelingDevour"));
                Assert.That(actions, Does.Contain("ActionChangelingTransform"));
                Assert.That(actions, Does.Contain("ActionChangelingExtractDna"));
                Assert.That(actions, Does.Contain("ActionChangelingRegenerativeStasis"));
                Assert.That(actions, Does.Contain("ActionChangelingRegenerate"));
            });

            var identity = entMan.GetComponent<ChangelingIdentityComponent>(ling);
            Assert.Multiple(() =>
            {
                Assert.That(identity.ConsumedIdentities, Has.Count.EqualTo(1));
                Assert.That(identity.StoredIdentities, Is.EquivalentTo(identity.ConsumedIdentities.Keys));
                Assert.That(identity.StoredGenomes, Has.Count.EqualTo(1));
                Assert.That(identity.AbsorbedGenomes, Is.Empty);
            });
        });
    }

    [Test]
    [PairConfig(nameof(PsDisconnected))]
    public async Task FailedDevourIdentityStorageDoesNotDamageOrConsumeVictim()
    {
        var pair = Pair;
        var server = pair.Server;
        var entMan = server.EntMan;
        var testMap = await pair.CreateTestMap();
        EntityUid ling = default;
        EntityUid victim = default;

        await server.WaitAssertion(() =>
        {
            ling = entMan.SpawnEntity("MobLing", testMap.GridCoords);
            victim = entMan.SpawnEntity("MobHuman", testMap.GridCoords);
            entMan.System<MobStateSystem>().ChangeMobState(victim, MobState.Dead);
        });
        await pair.RunTicksSync(1);

        bool AllowExpectedInvalidCloningSettings(string sawmill, Serilog.Events.LogEvent message)
        {
            return sawmill == "proto" && message.RenderMessage().Contains("ChangelingMissingCloningSettings");
        }

        pair.ServerLogHandler.JudgeLog += AllowExpectedInvalidCloningSettings;
        try
        {
            await server.WaitAssertion(() =>
            {
                var identities = entMan.GetComponent<ChangelingIdentityComponent>(ling);
                identities.IdentityCloningSettings = "ChangelingMissingCloningSettings";

                var damageable = entMan.System<DamageableSystem>();
#pragma warning disable CS0618 // Exact damage comparison is intentional in this regression assertion.
                var damageBefore = damageable.GetAllDamage(victim).GetTotal();
#pragma warning restore CS0618
                var consume = new ChangelingDevourConsumeDoAfterEvent();
                var doAfterArgs = new DoAfterArgs(
                    entMan,
                    ling,
                    TimeSpan.Zero,
                    consume,
                    ling,
                    target: victim,
                    used: ling);
                consume.DoAfter = new Content.Shared.DoAfter.DoAfter(0, doAfterArgs, TimeSpan.Zero);

                entMan.EventBus.RaiseLocalEvent(ling, consume);

                Assert.Multiple(() =>
                {
                    Assert.That(consume.Handled, Is.True);
#pragma warning disable CS0618 // Exact damage comparison is intentional in this regression assertion.
                    Assert.That(damageable.GetAllDamage(victim).GetTotal(), Is.EqualTo(damageBefore),
                        "A devour that cannot preserve the identity must not apply its final damage.");
#pragma warning restore CS0618
                    Assert.That(entMan.HasComponent<ChangelingDevouredComponent>(victim), Is.False,
                        "A failed identity commit must not mark the victim as consumed.");
                });
            });
        }
        finally
        {
            pair.ServerLogHandler.JudgeLog -= AllowExpectedInvalidCloningSettings;
        }
    }

    [Test]
    [PairConfig(nameof(PsDisconnected))]
    public async Task RepeatedDevourDuringConsumeCannotCancelOrRestartIt()
    {
        var pair = Pair;
        var server = pair.Server;
        var entMan = server.EntMan;
        var testMap = await pair.CreateTestMap();
        EntityUid ling = default;
        EntityUid victim = default;
        FixedPoint2 slashAfterInitialWindup = default;
        FixedPoint2 piercingAfterInitialWindup = default;

        await server.WaitAssertion(() =>
        {
            ling = entMan.SpawnEntity("MobLing", testMap.GridCoords);
            victim = entMan.SpawnEntity("MobHuman", testMap.GridCoords);
            entMan.System<MobStateSystem>().ChangeMobState(victim, MobState.Dead);
        });
        await pair.RunTicksSync(1);

        await server.WaitAssertion(() =>
        {
            var windup = new ChangelingDevourWindupDoAfterEvent();
            var doAfterArgs = new DoAfterArgs(
                entMan,
                ling,
                TimeSpan.Zero,
                windup,
                ling,
                target: victim,
                used: ling);
            windup.DoAfter = new Content.Shared.DoAfter.DoAfter(0, doAfterArgs, TimeSpan.Zero);
            entMan.EventBus.RaiseLocalEvent(ling, windup);

            var damageable = entMan.System<DamageableSystem>();
#pragma warning disable CS0618 // Exact damage comparison is intentional in this regression assertion.
            var damageAfterInitialWindup = damageable.GetAllDamage(victim).DamageDict;
#pragma warning restore CS0618
            damageAfterInitialWindup.TryGetValue("Slash", out slashAfterInitialWindup);
            damageAfterInitialWindup.TryGetValue("Piercing", out piercingAfterInitialWindup);

            for (var i = 0; i < 2; i++)
            {
                var repeated = new ChangelingDevourActionEvent
                {
                    Performer = ling,
                    Target = victim,
                };
                entMan.EventBus.RaiseLocalEvent(ling, repeated);
            }

            var activeDoAfters = entMan.GetComponent<DoAfterComponent>(ling)
                .DoAfters.Values
                .Where(doAfter => !doAfter.Cancelled && !doAfter.Completed)
                .ToArray();
            Assert.That(activeDoAfters, Has.Length.EqualTo(1),
                "Repeated activation must neither cancel the consume nor create a parallel windup.");
            Assert.That(activeDoAfters[0].Args.Event, Is.TypeOf<ChangelingDevourConsumeDoAfterEvent>(),
                "The sole active Devour DoAfter must remain the original consume phase.");
        });

        await pair.RunSeconds(2.1f);
        await server.WaitAssertion(() =>
        {
#pragma warning disable CS0618 // Exact damage comparison is intentional in this regression assertion.
            var damageAfterRepeatedActivation = entMan.System<DamageableSystem>().GetAllDamage(victim).DamageDict;
#pragma warning restore CS0618
            Assert.Multiple(() =>
            {
                Assert.That(damageAfterRepeatedActivation["Slash"], Is.EqualTo(slashAfterInitialWindup),
                    "Repeated activation during consume must not apply slash windup damage a second time.");
                Assert.That(damageAfterRepeatedActivation["Piercing"], Is.EqualTo(piercingAfterInitialWindup),
                    "Repeated activation during consume must not apply piercing windup damage a second time.");
            });
            Assert.That(entMan.HasComponent<ChangelingDevouredComponent>(victim), Is.False,
                "The original consume phase should still be running before its configured duration elapses.");
        });

        await pair.RunSeconds(8.1f);
        await server.WaitAssertion(() =>
        {
            Assert.That(entMan.GetComponent<ChangelingDevouredComponent>(victim).DevouredBy,
                Does.Contain(ling),
                "Repeated activation must leave the original consume phase alive through completion.");
        });
    }

    [Test]
    [PairConfig(nameof(PsDisconnected))]
    public async Task RetractingHeldMutationsClearsTheirActionToggle()
    {
        var pair = Pair;
        var server = pair.Server;
        var entMan = server.EntMan;
        var testMap = await pair.CreateTestMap();
        EntityUid bladeLing = default;
        EntityUid shieldLing = default;

        await server.WaitAssertion(() =>
        {
            bladeLing = entMan.SpawnEntity("MobLing", testMap.GridCoords);
            shieldLing = entMan.SpawnEntity("MobLing", testMap.GridCoords);
        });
        await pair.RunTicksSync(1);

        await server.WaitAssertion(() =>
        {
            var actions = entMan.System<SharedActionsSystem>();
            var bladeAction = actions.AddAction(bladeLing, ArmBladeAction.Id);
            var shieldAction = actions.AddAction(shieldLing, OrganicShieldAction.Id);
            Assert.That(bladeAction, Is.Not.Null);
            Assert.That(shieldAction, Is.Not.Null);

            var blade = (bladeAction!.Value, entMan.GetComponent<ActionComponent>(bladeAction.Value));
            var shield = (shieldAction!.Value, entMan.GetComponent<ActionComponent>(shieldAction.Value));
            actions.PerformAction(bladeLing, blade);
            actions.PerformAction(shieldLing, shield);

            Assert.Multiple(() =>
            {
                Assert.That(blade.Item2.Toggled, Is.True);
                Assert.That(shield.Item2.Toggled, Is.True);
                Assert.That(entMan.GetComponent<ChangelingMutationStateComponent>(bladeLing).ArmBlade, Is.Not.Null);
                Assert.That(entMan.GetComponent<ChangelingMutationStateComponent>(shieldLing).OrganicShield, Is.Not.Null);
            });

            actions.PerformAction(bladeLing, blade);
            actions.PerformAction(shieldLing, shield);

            Assert.Multiple(() =>
            {
                Assert.That(blade.Item2.Toggled, Is.False,
                    "Retracting the arm blade must clear the action toggle instead of inverting it twice.");
                Assert.That(shield.Item2.Toggled, Is.False,
                    "Retracting the organic shield must clear the action toggle instead of inverting it twice.");
                Assert.That(entMan.GetComponent<ChangelingMutationStateComponent>(bladeLing).ArmBlade, Is.Null);
                Assert.That(entMan.GetComponent<ChangelingMutationStateComponent>(shieldLing).OrganicShield, Is.Null);
            });
        });
    }

    [Test]
    [PairConfig(nameof(PsDisconnected))]
    public async Task DnaStorageIsCappedWhileUniqueProgressIsCumulative()
    {
        var pair = Pair;
        var server = pair.Server;
        var entMan = server.EntMan;
        var testMap = await pair.CreateTestMap();
        EntityUid ling = default;
        EntityUid victim = default;

        await server.WaitAssertion(() =>
        {
            ling = entMan.SpawnEntity("MobLing", testMap.GridCoords);
            victim = entMan.SpawnEntity("MobHuman", testMap.GridCoords);
        });
        await pair.RunTicksSync(1);

        await server.WaitAssertion(() =>
        {
            var identity = entMan.GetComponent<ChangelingIdentityComponent>(ling);
            var identitySystem = entMan.System<ChangelingIdentitySystem>();
            var lingEntity = new Entity<ChangelingIdentityComponent>(ling, identity);

            Assert.That(identitySystem.TryStoreIdentity(
                lingEntity,
                victim,
                "genome-alpha",
                victim,
                countForObjective: true,
                out _),
                Is.True);

            Assert.That(identitySystem.TryStoreIdentity(
                lingEntity,
                victim,
                "genome-alpha",
                victim,
                countForObjective: true,
                out _),
                Is.False,
                "An actively stored genome must not be duplicated.");

            foreach (var genome in new[] { "genome-beta", "genome-gamma", "genome-delta" })
            {
                Assert.That(identitySystem.TryStoreIdentity(
                    lingEntity,
                    victim,
                    genome,
                    victim,
                    countForObjective: true,
                    out _),
                    Is.True);
            }

            Assert.Multiple(() =>
            {
                Assert.That(identity.ConsumedIdentities, Has.Count.EqualTo(identity.MaxStoredIdentities));
                Assert.That(identity.StoredIdentities, Is.EquivalentTo(identity.ConsumedIdentities.Keys));
                Assert.That(identity.StoredGenomes, Has.Count.EqualTo(identity.MaxStoredIdentities));
                Assert.That(entMan.Count<ChangelingIdentityStorageMapComponent>(), Is.EqualTo(1),
                    "All paused DNA snapshots must share one ECS-owned storage map.");
                Assert.That(identity.AbsorbedGenomes,
                    Is.EquivalentTo(new[] { "genome-alpha", "genome-beta", "genome-gamma", "genome-delta" }));
            });

            Assert.That(identitySystem.TryStoreIdentity(
                lingEntity,
                victim,
                "genome-overflow",
                victim,
                countForObjective: true,
                out _),
                Is.False,
                "A sixth active identity must be rejected.");
            Assert.That(identity.AbsorbedGenomes, Does.Not.Contain("genome-overflow"));

            var alphaIdentity = identity.StoredGenomes.Single(pair => pair.Value == "genome-alpha").Key;
            Assert.That(identitySystem.TryDropStoredIdentity(lingEntity, alphaIdentity), Is.True);

            Assert.That(identitySystem.TryStoreIdentity(
                lingEntity,
                victim,
                "genome-alpha",
                victim,
                countForObjective: true,
                out _),
                Is.True,
                "A consumed or discarded sample may be acquired again.");

            Assert.Multiple(() =>
            {
                Assert.That(identity.StoredGenomes, Has.Count.EqualTo(identity.MaxStoredIdentities));
                Assert.That(identity.StoredIdentities, Is.EquivalentTo(identity.ConsumedIdentities.Keys));
                Assert.That(identity.AbsorbedGenomes, Has.Count.EqualTo(4),
                    "Reacquiring an old genome must not increment cumulative unique progress.");
                Assert.That(identity.AbsorbedGenomes, Does.Contain("genome-alpha"));
            });
        });
    }

    [Test]
    [PairConfig(nameof(PsDisconnected))]
    public async Task SwapFormsMovesPersistentChangelingStateAndCoreAbilities()
    {
        var pair = Pair;
        var server = pair.Server;
        var entMan = server.EntMan;
        var testMap = await pair.CreateTestMap();
        EntityUid ling = default;
        EntityUid target = default;
        EntityUid storedIdentity = default;
        EntityUid mindId = default;
        EntityUid targetMindId = default;
        var storedCloneCountBeforeSwap = 0;
        HashSet<EntityUid> originalCoreActions = [];
        TimeSpan expectedDevourWindup = default;
        TimeSpan expectedDevourConsume = default;
        TimeSpan expectedTransformWindup = default;
        FixedPoint2 expectedTransformCost = default;
        string targetGenome = null!;

        await server.WaitAssertion(() =>
        {
            ling = entMan.SpawnEntity("MobLing", testMap.GridCoords);
            target = entMan.SpawnEntity("MobHuman", testMap.GridCoords);
        });
        await pair.RunTicksSync(1);

        await server.WaitAssertion(() =>
        {
            var mindSystem = entMan.System<SharedMindSystem>();
            var mind = mindSystem.CreateMind(null);
            mindId = mind.Owner;
            mindSystem.TransferTo(mind, ling, mind: mind.Comp);

            var targetMind = mindSystem.CreateMind(null);
            targetMindId = targetMind.Owner;
            targetMind.Comp.PreventGhosting = true;
            mindSystem.TransferTo(targetMind, target, mind: targetMind.Comp);

            var identity = entMan.GetComponent<ChangelingIdentityComponent>(ling);
            var identitySystem = entMan.System<ChangelingIdentitySystem>();
            targetGenome = identitySystem.GetGenomeId(target);
            Assert.That(targetGenome, Is.Not.Null);
            Assert.That(identitySystem.TryStoreIdentity(
                (ling, identity),
                target,
                "persistent-transfer-genome",
                target,
                countForObjective: true,
                out var stored),
                Is.True);
            storedIdentity = stored!.Value;
            storedCloneCountBeforeSwap = entMan.EntityQuery<ChangelingStoredIdentityComponent>().Count();

            var devour = entMan.GetComponent<ChangelingDevourComponent>(ling);
            expectedDevourWindup = devour.DevourWindupTime;
            expectedDevourConsume = devour.DevourConsumeTime;
            originalCoreActions.Add(devour.ChangelingDevourActionEntity!.Value);

            var transformAbility = entMan.GetComponent<ChangelingTransformComponent>(ling);
            expectedTransformWindup = transformAbility.TransformWindup;
            expectedTransformCost = transformAbility.ChemicalCost;
            originalCoreActions.Add(transformAbility.ChangelingTransformActionEntity!.Value);

            var extract = entMan.GetComponent<ChangelingExtractDnaComponent>(ling);
            extract.ChemicalCost = 13;
            originalCoreActions.Add(extract.ActionEntity!.Value);

            var resources = entMan.GetComponent<ChangelingResourceComponent>(ling);
            resources.RegenerativeStasisChemicalCost = FixedPoint2.New(17);
            resources.RegenerativeStasisDuration = TimeSpan.FromSeconds(33);
            originalCoreActions.Add(resources.RegenerativeStasisActionEntity!.Value);
            originalCoreActions.Add(resources.RegenerateActionEntity!.Value);

            var actionSystem = entMan.System<SharedActionsSystem>();
            var swapAction = actionSystem.AddAction(ling, SwapFormsAction.Id);
            Assert.That(swapAction, Is.Not.Null);
            entMan.GetComponent<StoreComponent>(ling).BoughtEntities.Add(swapAction!.Value);

            var action = entMan.GetComponent<ActionComponent>(swapAction.Value);
            var swap = new ChangelingSwapFormsActionEvent
            {
                Performer = ling,
                Target = target,
                Action = (swapAction.Value, action),
                ChemicalCost = 0,
                StunTime = TimeSpan.Zero,
            };
            entMan.EventBus.RaiseLocalEvent(ling, swap);
            Assert.That(swap.Handled, Is.True);
        });
        await pair.RunTicksSync(1);

        await server.WaitAssertion(() =>
        {
            var mind = entMan.GetComponent<MindComponent>(mindId);
            var targetMind = entMan.GetComponent<MindComponent>(targetMindId);
            Assert.That(mind.OwnedEntity, Is.EqualTo(target));
            Assert.That(targetMind.OwnedEntity, Is.EqualTo(ling),
                "Swap Forms must move the target mind even when ghosting is forbidden.");
            Assert.That(entMan.EntityQuery<ChangelingStoredIdentityComponent>().Count(),
                Is.EqualTo(storedCloneCountBeforeSwap),
                "Transferring identity state must not create an orphan identity clone.");

            Assert.Multiple(() =>
            {
                Assert.That(entMan.HasComponent<ChangelingIdentityComponent>(target), Is.True);
                Assert.That(entMan.HasComponent<ChangelingResourceComponent>(target), Is.True);
                Assert.That(entMan.HasComponent<ChangelingDevourComponent>(target), Is.True);
                Assert.That(entMan.HasComponent<ChangelingTransformComponent>(target), Is.True);
                Assert.That(entMan.HasComponent<ChangelingExtractDnaComponent>(target), Is.True);
                Assert.That(entMan.HasComponent<StoreComponent>(target), Is.True);

                Assert.That(entMan.HasComponent<ChangelingIdentityComponent>(ling), Is.False);
                Assert.That(entMan.HasComponent<ChangelingResourceComponent>(ling), Is.False);
                Assert.That(entMan.HasComponent<ChangelingDevourComponent>(ling), Is.False);
                Assert.That(entMan.HasComponent<ChangelingTransformComponent>(ling), Is.False);
                Assert.That(entMan.HasComponent<ChangelingExtractDnaComponent>(ling), Is.False);
                Assert.That(entMan.HasComponent<StoreComponent>(ling), Is.False);
            });

            var identity = entMan.GetComponent<ChangelingIdentityComponent>(target);
            Assert.Multiple(() =>
            {
                Assert.That(identity.AbsorbedGenomes, Does.Contain("persistent-transfer-genome"));
                Assert.That(identity.StoredIdentities, Does.Contain(storedIdentity));
                Assert.That(identity.StoredIdentities, Is.EquivalentTo(identity.ConsumedIdentities.Keys));
                Assert.That(identity.StoredGenomes.ContainsKey(storedIdentity), Is.True);
                Assert.That(identity.CurrentGenome, Is.EqualTo(targetGenome));
                Assert.That(entMan.Deleted(storedIdentity), Is.False,
                    "Moving the identity component must not delete paused identity clones.");
            });

            var transferredDevour = entMan.GetComponent<ChangelingDevourComponent>(target);
            var transferredTransform = entMan.GetComponent<ChangelingTransformComponent>(target);
            var transferredExtract = entMan.GetComponent<ChangelingExtractDnaComponent>(target);
            var transferredResources = entMan.GetComponent<ChangelingResourceComponent>(target);
            Assert.Multiple(() =>
            {
                Assert.That(transferredDevour.DevourWindupTime, Is.EqualTo(expectedDevourWindup));
                Assert.That(transferredDevour.DevourConsumeTime, Is.EqualTo(expectedDevourConsume));
                Assert.That(transferredTransform.TransformWindup, Is.EqualTo(expectedTransformWindup));
                Assert.That(transferredTransform.ChemicalCost, Is.EqualTo(expectedTransformCost));
                Assert.That(transferredExtract.ChemicalCost, Is.EqualTo(13));
                Assert.That(transferredResources.RegenerativeStasisChemicalCost, Is.EqualTo(FixedPoint2.New(17)));
                Assert.That(transferredResources.RegenerativeStasisDuration, Is.EqualTo(TimeSpan.FromSeconds(33)));
                Assert.That(originalCoreActions.All(action => !entMan.EntityExists(action)), Is.True,
                    "Component-owned actions from the abandoned body must not survive as orphan entities.");
            });

            var targetActions = entMan.System<SharedActionsSystem>()
                .GetActions(target)
                .Select(action => entMan.GetComponent<MetaDataComponent>(action).EntityPrototype?.ID)
                .Where(id => id != null)
                .ToHashSet();
            Assert.Multiple(() =>
            {
                Assert.That(targetActions, Does.Contain("ActionChangelingStore"));
                Assert.That(targetActions, Does.Contain("ActionChangelingDevour"));
                Assert.That(targetActions, Does.Contain("ActionChangelingTransform"));
                Assert.That(targetActions, Does.Contain("ActionChangelingExtractDna"));
                Assert.That(targetActions, Does.Contain("ActionChangelingRegenerativeStasis"));
                Assert.That(targetActions, Does.Contain("ActionChangelingRegenerate"));
                Assert.That(targetActions, Does.Contain(SwapFormsAction.Id));
            });

            var store = entMan.GetComponent<StoreComponent>(target);
            Assert.That(store.BoughtEntities.Select(uid => entMan.GetComponent<MetaDataComponent>(uid).EntityPrototype?.ID),
                Does.Contain(SwapFormsAction.Id));
        });
    }

    [Test]
    [PairConfig(nameof(PsDisconnected))]
    public async Task LastResortPreservesChangelingStateThroughHatching()
    {
        var pair = Pair;
        var server = pair.Server;
        var entMan = server.EntMan;
        var testMap = await pair.CreateTestMap();
        EntityUid ling = default;
        EntityUid corpse = default;
        EntityUid mindId = default;
        EntityUid storedState = default;

        await server.WaitAssertion(() =>
        {
            ling = entMan.SpawnEntity("MobLing", testMap.GridCoords);
            corpse = entMan.SpawnEntity("MobHuman", testMap.GridCoords);
            entMan.System<MobStateSystem>().ChangeMobState(corpse, MobState.Dead);
        });
        await pair.RunTicksSync(1);

        await server.WaitAssertion(() =>
        {
            var mindSystem = entMan.System<SharedMindSystem>();
            var mind = mindSystem.CreateMind(null);
            mindId = mind.Owner;
            mindSystem.TransferTo(mind, ling, mind: mind.Comp);

            var identity = entMan.GetComponent<ChangelingIdentityComponent>(ling);
            Assert.That(entMan.System<ChangelingIdentitySystem>().TryStoreIdentity(
                (ling, identity),
                corpse,
                "last-resort-persistent-genome",
                corpse,
                countForObjective: true,
                out _),
                Is.True);

            var actions = entMan.System<SharedActionsSystem>();
            var lastResortAction = actions.AddAction(ling, LastResortAction.Id);
            Assert.That(lastResortAction, Is.Not.Null);
            entMan.GetComponent<StoreComponent>(ling).BoughtEntities.Add(lastResortAction!.Value);

            var action = entMan.GetComponent<ActionComponent>(lastResortAction.Value);
            var lastResort = new ChangelingLastResortActionEvent
            {
                Performer = ling,
                Action = (lastResortAction.Value, action),
                ChemicalCost = 0,
            };
            entMan.EventBus.RaiseLocalEvent(ling, lastResort);
            Assert.That(lastResort.Handled, Is.True);
        });
        await pair.RunTicksSync(1);

        await server.WaitAssertion(() =>
        {
            var mind = entMan.GetComponent<MindComponent>(mindId);
            Assert.That(mind.OwnedEntity, Is.Not.Null);
            var headslug = mind.OwnedEntity!.Value;
            Assert.That(entMan.GetComponent<MetaDataComponent>(headslug).EntityPrototype?.ID,
                Is.EqualTo("MobChangelingHeadslug"));

            var headslugComp = entMan.GetComponent<ChangelingHeadslugComponent>(headslug);
            Assert.That(headslugComp.StoredState, Is.Not.Null);
            storedState = headslugComp.StoredState!.Value;

            Assert.Multiple(() =>
            {
                Assert.That(entMan.HasComponent<ChangelingIdentityComponent>(headslug), Is.False);
                Assert.That(entMan.HasComponent<ChangelingResourceComponent>(headslug), Is.False);
                Assert.That(entMan.HasComponent<ChangelingDevourComponent>(headslug), Is.False);
                Assert.That(entMan.HasComponent<ChangelingTransformComponent>(headslug), Is.False);
                Assert.That(entMan.HasComponent<ChangelingExtractDnaComponent>(headslug), Is.False);
                Assert.That(entMan.HasComponent<StoreComponent>(headslug), Is.False);
                Assert.That(entMan.HasComponent<ChangelingLastResortStorageComponent>(storedState), Is.True);
                Assert.That(entMan.HasComponent<ChangelingIdentityComponent>(storedState), Is.True);
                Assert.That(entMan.HasComponent<ChangelingResourceComponent>(storedState), Is.True);
                Assert.That(entMan.HasComponent<StoreComponent>(storedState), Is.True);
            });

            var headslugActionIds = entMan.System<SharedActionsSystem>()
                .GetActions(headslug)
                .Select(uid => entMan.GetComponent<MetaDataComponent>(uid).EntityPrototype?.ID)
                .Where(id => id != null)
                .ToHashSet();
            Assert.Multiple(() =>
            {
                Assert.That(headslugActionIds, Does.Contain("ActionChangelingLayEgg"));
                Assert.That(headslugActionIds, Does.Not.Contain("ActionChangelingStore"));
                Assert.That(headslugActionIds, Does.Not.Contain("ActionChangelingDevour"));
                Assert.That(headslugActionIds, Does.Not.Contain("ActionChangelingTransform"));
                Assert.That(headslugActionIds, Does.Not.Contain("ActionChangelingExtractDna"));
                Assert.That(headslugActionIds, Does.Not.Contain(LastResortAction.Id),
                    "The vulnerable headslug must not receive the store or any changeling mutation.");
            });

            var layEggAction = entMan.System<SharedActionsSystem>()
                .GetActions(headslug)
                .Single(uid => entMan.GetComponent<MetaDataComponent>(uid).EntityPrototype?.ID ==
                               "ActionChangelingLayEgg");
            var action = entMan.GetComponent<ActionComponent>(layEggAction);
            var layEgg = new ChangelingLayEggActionEvent
            {
                Performer = headslug,
                Target = corpse,
                Action = (layEggAction, action),
                HatchDelay = TimeSpan.Zero,
            };
            entMan.EventBus.RaiseLocalEvent(headslug, layEgg);
            Assert.That(layEgg.Handled, Is.True);
        });
        await pair.RunTicksSync(2);

        await server.WaitAssertion(() =>
        {
            var mind = entMan.GetComponent<MindComponent>(mindId);
            Assert.That(mind.OwnedEntity, Is.Not.Null);
            var hatchling = mind.OwnedEntity!.Value;
            Assert.That(entMan.GetComponent<MetaDataComponent>(hatchling).EntityPrototype?.ID,
                Is.EqualTo("MobMonkey"));

            Assert.Multiple(() =>
            {
                Assert.That(entMan.HasComponent<ChangelingIdentityComponent>(hatchling), Is.True);
                Assert.That(entMan.HasComponent<ChangelingResourceComponent>(hatchling), Is.True);
                Assert.That(entMan.HasComponent<ChangelingDevourComponent>(hatchling), Is.True);
                Assert.That(entMan.HasComponent<ChangelingTransformComponent>(hatchling), Is.True);
                Assert.That(entMan.HasComponent<ChangelingExtractDnaComponent>(hatchling), Is.True);
                Assert.That(entMan.HasComponent<StoreComponent>(hatchling), Is.True);
            });

            var identity = entMan.GetComponent<ChangelingIdentityComponent>(hatchling);
            Assert.Multiple(() =>
            {
                Assert.That(identity.AbsorbedGenomes, Does.Contain("last-resort-persistent-genome"));
                Assert.That(identity.CurrentGenome, Is.Null,
                    "A newly hatched monkey must not count as an old humanoid disguise.");
            });

            var actionIds = entMan.System<SharedActionsSystem>()
                .GetActions(hatchling)
                .Select(uid => entMan.GetComponent<MetaDataComponent>(uid).EntityPrototype?.ID)
                .Where(id => id != null)
                .ToHashSet();
            Assert.Multiple(() =>
            {
                Assert.That(actionIds, Does.Contain("ActionChangelingStore"));
                Assert.That(actionIds, Does.Contain("ActionChangelingDevour"));
                Assert.That(actionIds, Does.Contain("ActionChangelingTransform"));
                Assert.That(actionIds, Does.Contain("ActionChangelingExtractDna"));
                Assert.That(actionIds, Does.Contain(LastResortAction.Id));
                Assert.That(entMan.Deleted(storedState), Is.True,
                    "The temporary Last Resort storage entity must be deleted after hatching.");
            });
        });
    }

    [Test]
    [PairConfig(nameof(PsDisconnected))]
    public async Task RemovingChangelingResourcesClearsChemicalAlert()
    {
        var pair = Pair;
        var server = pair.Server;
        var entMan = server.EntMan;
        var testMap = await pair.CreateTestMap();
        EntityUid ling = default;
        ProtoId<AlertPrototype> chemicalAlert = default;

        await server.WaitAssertion(() =>
        {
            ling = entMan.SpawnEntity("MobLing", testMap.GridCoords);
        });
        await pair.RunTicksSync(1);

        await server.WaitAssertion(() =>
        {
            chemicalAlert = entMan.GetComponent<ChangelingResourceComponent>(ling).ChemicalsAlert;
            var alerts = entMan.GetComponent<AlertsComponent>(ling);
            Assert.That(alerts.Alerts.Keys.Any(key => key.AlertType == chemicalAlert), Is.True);

            entMan.RemoveComponent<ChangelingResourceComponent>(ling);
            Assert.That(alerts.Alerts.Keys.Any(key => key.AlertType == chemicalAlert), Is.False,
                "Removing the resource component must not leave a role-revealing stale alert.");
        });
    }

    [Test]
    [PairConfig(nameof(PsDisconnected))]
    public async Task ChemicalCatchUpCannotOverflowFixedPointPool()
    {
        var pair = Pair;
        var server = pair.Server;
        var entMan = server.EntMan;
        var testMap = await pair.CreateTestMap();
        EntityUid ling = default;

        await server.WaitAssertion(() =>
        {
            ling = entMan.SpawnEntity("MobLing", testMap.GridCoords);
        });
        await pair.RunTicksSync(1);

        await server.WaitAssertion(() =>
        {
            var resources = entMan.GetComponent<ChangelingResourceComponent>(ling);
            resources.Chemicals = FixedPoint2.Zero;
            resources.ChemicalRegenerationAmount = FixedPoint2.New(1);
            resources.ChemicalRegenerationInterval = TimeSpan.FromTicks(1);
            resources.NextChemicalRegeneration = TimeSpan.FromTicks(1);
        });
        await pair.RunTicksSync(1);

        await server.WaitAssertion(() =>
        {
            var resources = entMan.GetComponent<ChangelingResourceComponent>(ling);
            Assert.That(resources.Chemicals, Is.EqualTo(resources.MaxChemicals),
                "An extreme catch-up count should saturate the pool without overflowing FixedPoint2.");
        });
    }

    [Test]
    [PairConfig(nameof(PsDisconnected))]
    public async Task FakeArmBladeDoesNotInheritRealArmBladeTools()
    {
        var pair = Pair;
        var server = pair.Server;
        var entMan = server.EntMan;
        var testMap = await pair.CreateTestMap();
        EntityUid blade = default;

        await server.WaitAssertion(() =>
        {
            blade = entMan.SpawnEntity("ChangelingFakeArmBlade", testMap.GridCoords);
            Assert.Multiple(() =>
            {
                Assert.That(entMan.HasComponent<ExecutionComponent>(blade), Is.False);
                Assert.That(entMan.HasComponent<PryingComponent>(blade), Is.False);
                Assert.That(entMan.HasComponent<SharpComponent>(blade), Is.False);
                Assert.That(entMan.HasComponent<AltBlockingComponent>(blade), Is.False);
            });
        });
    }

    [Test]
    [PairConfig(nameof(PsDisconnected))]
    public async Task ChitinousArmorRestoresOriginalEquipmentImmediately()
    {
        var pair = Pair;
        var server = pair.Server;
        var entMan = server.EntMan;
        var testMap = await pair.CreateTestMap();
        EntityUid ling = default;
        EntityUid outer = default;
        EntityUid helmet = default;
        EntityUid chitinousArmor = default;
        EntityUid chitinousHelmet = default;
        EntityUid chitinousArmorAction = default;

        await server.WaitAssertion(() =>
        {
            ling = entMan.SpawnEntity("MobLing", testMap.GridCoords);
            outer = entMan.SpawnEntity("ClothingOuterArmorBasic", testMap.GridCoords);
            helmet = entMan.SpawnEntity("ClothingHeadHelmetBasic", testMap.GridCoords);
        });
        await pair.RunTicksSync(1);

        await server.WaitAssertion(() =>
        {
            var inventory = entMan.System<InventorySystem>();
            Assert.That(inventory.TryEquip(ling, outer, "outerClothing", silent: true, force: true), Is.True);
            Assert.That(inventory.TryEquip(ling, helmet, "head", silent: true, force: true), Is.True);

            var actions = entMan.System<SharedActionsSystem>();
            var actionUid = actions.AddAction(ling, ChitinousArmorAction.Id)!.Value;
            var action = entMan.GetComponent<ActionComponent>(actionUid);
            chitinousArmorAction = actionUid;

            var activate = new ChangelingChitinousArmorActionEvent
            {
                Performer = ling,
                Action = (actionUid, action),
                ChemicalCost = 0,
            };
            entMan.EventBus.RaiseLocalEvent(ling, activate);
            Assert.That(activate.Handled, Is.True);

            var mutationState = entMan.GetComponent<ChangelingMutationStateComponent>(ling);
            chitinousArmor = mutationState.ChitinousArmorVisual!.Value;
            chitinousHelmet = mutationState.ChitinousHelmetVisual!.Value;
            Assert.Multiple(() =>
            {
                Assert.That(entMan.GetComponent<ClothingComponent>(chitinousArmor).EquippedPrefix, Is.EqualTo("transform"));
                Assert.That(entMan.GetComponent<ClothingComponent>(chitinousHelmet).EquippedPrefix, Is.EqualTo("transform"));
            });
        });
        await pair.RunSeconds(2.1f);

        await server.WaitAssertion(() =>
        {
            var inventory = entMan.System<InventorySystem>();
            Assert.Multiple(() =>
            {
                Assert.That(entMan.GetComponent<ClothingComponent>(chitinousArmor).EquippedPrefix, Is.Null);
                Assert.That(entMan.GetComponent<ClothingComponent>(chitinousHelmet).EquippedPrefix, Is.Null);
            });

            var action = entMan.GetComponent<ActionComponent>(chitinousArmorAction);
            var deactivate = new ChangelingChitinousArmorActionEvent
            {
                Performer = ling,
                Action = (chitinousArmorAction, action),
                ChemicalCost = 0,
            };
            entMan.EventBus.RaiseLocalEvent(ling, deactivate);
            Assert.That(deactivate.Handled, Is.True);

            Assert.Multiple(() =>
            {
                Assert.That(inventory.TryGetSlotEntity(ling, "outerClothing", out var restoredOuter), Is.True);
                Assert.That(restoredOuter, Is.EqualTo(outer));
                Assert.That(inventory.TryGetSlotEntity(ling, "head", out var restoredHelmet), Is.True);
                Assert.That(restoredHelmet, Is.EqualTo(helmet));
            });
        });
    }

    [Test]
    [PairConfig(nameof(PsDisconnected))]
    public async Task CryogenicStingRestoresOriginalMovementSpeed()
    {
        var pair = Pair;
        var server = pair.Server;
        var entMan = server.EntMan;
        var testMap = await pair.CreateTestMap();
        EntityUid ling = default;
        EntityUid victim = default;
        float originalWalkSpeed = default;
        float originalSprintSpeed = default;

        await server.WaitAssertion(() =>
        {
            ling = entMan.SpawnEntity("MobLing", testMap.GridCoords);
            victim = entMan.SpawnEntity("MobHuman", testMap.GridCoords);
        });
        await pair.RunTicksSync(1);

        await server.WaitAssertion(() =>
        {
            var movement = entMan.GetComponent<MovementSpeedModifierComponent>(victim);
            originalWalkSpeed = movement.CurrentWalkSpeed;
            originalSprintSpeed = movement.CurrentSprintSpeed;

            var actions = entMan.System<SharedActionsSystem>();
            var actionUid = actions.AddAction(ling, CryogenicStingAction.Id)!.Value;
            var sting = new ChangelingCryogenicStingActionEvent
            {
                Performer = ling,
                Target = victim,
                Action = (actionUid, entMan.GetComponent<ActionComponent>(actionUid)),
            };
            entMan.EventBus.RaiseLocalEvent(ling, sting);

            Assert.Multiple(() =>
            {
                Assert.That(sting.Handled, Is.True);
                Assert.That(entMan.HasComponent<ChangelingCryogenicStingComponent>(victim), Is.True);
                Assert.That(movement.CurrentWalkSpeed, Is.LessThan(originalWalkSpeed));
                Assert.That(movement.CurrentSprintSpeed, Is.LessThan(originalSprintSpeed));
            });
        });

        await server.WaitAssertion(() =>
        {
            entMan.RemoveComponent<ChangelingCryogenicStingComponent>(victim);
            var movement = entMan.GetComponent<MovementSpeedModifierComponent>(victim);
            Assert.Multiple(() =>
            {
                Assert.That(movement.CurrentWalkSpeed, Is.EqualTo(originalWalkSpeed).Within(0.001f));
                Assert.That(movement.CurrentSprintSpeed, Is.EqualTo(originalSprintSpeed).Within(0.001f));
            });

            var actions = entMan.System<SharedActionsSystem>();
            var actionUid = actions.AddAction(ling, CryogenicStingAction.Id)!.Value;
            var sting = new ChangelingCryogenicStingActionEvent
            {
                Performer = ling,
                Target = victim,
                Action = (actionUid, entMan.GetComponent<ActionComponent>(actionUid)),
            };
            entMan.EventBus.RaiseLocalEvent(ling, sting);
            Assert.That(sting.Handled, Is.True);

            // Expire the effect without leaving the victim in a vacuum for twenty seconds. Environmental
            // temperature and suffocation modifiers are unrelated to this test and would mask a stale sting slow.
            entMan.GetComponent<ChangelingCryogenicStingComponent>(victim).EndTime = TimeSpan.Zero;
        });
        await pair.RunTicksSync(1);

        await server.WaitAssertion(() =>
        {
            var movement = entMan.GetComponent<MovementSpeedModifierComponent>(victim);
            Assert.Multiple(() =>
            {
                Assert.That(entMan.HasComponent<ChangelingCryogenicStingComponent>(victim), Is.False);
                Assert.That(movement.CurrentWalkSpeed, Is.EqualTo(originalWalkSpeed).Within(0.001f));
                Assert.That(movement.CurrentSprintSpeed, Is.EqualTo(originalSprintSpeed).Within(0.001f));
            });
        });
    }

    [Test]
    [PairConfig(nameof(PsDisconnected))]
    public async Task RegenerativeStasisChargesOnlyOnEntryAndRevivesFromOrdinaryDeath()
    {
        var pair = Pair;
        var server = pair.Server;
        var entMan = server.EntMan;
        var testMap = await pair.CreateTestMap();
        EntityUid ling = default;

        await server.WaitAssertion(() =>
        {
            ling = entMan.SpawnEntity("MobLing", testMap.GridCoords);
        });
        await pair.RunTicksSync(1);

        await server.WaitAssertion(() =>
        {
            var resources = entMan.GetComponent<ChangelingResourceComponent>(ling);
            resources.RegenerativeStasisDuration = TimeSpan.Zero;
            entMan.System<MobStateSystem>().ChangeMobState(ling, MobState.Dead);

            var stasisAction = resources.RegenerativeStasisActionEntity!.Value;
            var stasis = new ChangelingRegenerativeStasisActionEvent
            {
                Performer = ling,
                Action = (stasisAction, entMan.GetComponent<ActionComponent>(stasisAction)),
            };
            entMan.EventBus.RaiseLocalEvent(ling, stasis);

            Assert.Multiple(() =>
            {
                Assert.That(stasis.Handled, Is.True);
                Assert.That(resources.InRegenerativeStasis, Is.True);
                Assert.That(resources.Chemicals, Is.EqualTo(FixedPoint2.New(60)));
                Assert.That(entMan.System<MobStateSystem>().IsDead(ling), Is.True);
            });
        });
        await pair.RunSeconds(2.1f);

        await server.WaitAssertion(() =>
        {
            var resources = entMan.GetComponent<ChangelingResourceComponent>(ling);
            Assert.That(resources.Chemicals, Is.EqualTo(FixedPoint2.New(61)),
                "Chemicals must continue regenerating while the changeling is dead in stasis.");
            var regenerateAction = resources.RegenerateActionEntity!.Value;
            var regenerate = new ChangelingRegenerateActionEvent
            {
                Performer = ling,
                Action = (regenerateAction, entMan.GetComponent<ActionComponent>(regenerateAction)),
            };
            entMan.EventBus.RaiseLocalEvent(ling, regenerate);

            Assert.Multiple(() =>
            {
                Assert.That(regenerate.Handled, Is.True);
                Assert.That(resources.InRegenerativeStasis, Is.False);
                Assert.That(resources.Chemicals, Is.EqualTo(FixedPoint2.New(61)),
                    "Leaving regenerative stasis must be free after the entry cost was paid.");
                Assert.That(entMan.System<MobStateSystem>().IsAlive(ling), Is.True);
            });
        });
    }

    [Test]
    [PairConfig(nameof(PsDisconnected))]
    public async Task RegenerativeStasisRestoresMissingInitialOrgans()
    {
        var pair = Pair;
        var server = pair.Server;
        var entMan = server.EntMan;
        var testMap = await pair.CreateTestMap();
        EntityUid ling = default;

        await server.WaitAssertion(() =>
        {
            ling = entMan.SpawnEntity("MobLing", testMap.GridCoords);
        });
        await pair.RunTicksSync(1);

        await server.WaitAssertion(() =>
        {
            var body = entMan.GetComponent<BodyComponent>(ling);
            Assert.That(body.Organs, Is.Not.Null);
            var organs = body.Organs!.ContainedEntities
                .Select(uid => (Uid: uid, Organ: entMan.GetComponent<OrganComponent>(uid)))
                .ToArray();
            var lungs = organs.Single(entry => entry.Organ.Category == LungsCategory).Uid;
            var heart = organs.Single(entry => entry.Organ.Category == HeartCategory).Uid;

            entMan.DeleteEntity(lungs);
            entMan.DeleteEntity(heart);

            var remainingCategories = body.Organs.ContainedEntities
                .Select(uid => entMan.GetComponent<OrganComponent>(uid).Category)
                .ToHashSet();
            Assert.Multiple(() =>
            {
                Assert.That(remainingCategories, Does.Not.Contain(LungsCategory));
                Assert.That(remainingCategories, Does.Not.Contain(HeartCategory));
            });

            var resources = entMan.GetComponent<ChangelingResourceComponent>(ling);
            resources.RegenerativeStasisDuration = TimeSpan.Zero;
            entMan.System<MobStateSystem>().ChangeMobState(ling, MobState.Dead);

            var stasisAction = resources.RegenerativeStasisActionEntity!.Value;
            var stasis = new ChangelingRegenerativeStasisActionEvent
            {
                Performer = ling,
                Action = (stasisAction, entMan.GetComponent<ActionComponent>(stasisAction)),
            };
            entMan.EventBus.RaiseLocalEvent(ling, stasis);
            Assert.That(stasis.Handled, Is.True);
        });
        await pair.RunTicksSync(1);

        await server.WaitAssertion(() =>
        {
            var resources = entMan.GetComponent<ChangelingResourceComponent>(ling);
            var regenerateAction = resources.RegenerateActionEntity!.Value;
            var regenerate = new ChangelingRegenerateActionEvent
            {
                Performer = ling,
                Action = (regenerateAction, entMan.GetComponent<ActionComponent>(regenerateAction)),
            };
            entMan.EventBus.RaiseLocalEvent(ling, regenerate);

            var body = entMan.GetComponent<BodyComponent>(ling);
            Assert.That(body.Organs, Is.Not.Null);
            var restoredOrgans = body.Organs!.ContainedEntities
                .Select(uid => (Uid: uid, Organ: entMan.GetComponent<OrganComponent>(uid)))
                .Where(entry => entry.Organ.Category == LungsCategory || entry.Organ.Category == HeartCategory)
                .ToArray();

            Assert.Multiple(() =>
            {
                Assert.That(regenerate.Handled, Is.True);
                Assert.That(restoredOrgans.Count(entry => entry.Organ.Category == LungsCategory), Is.EqualTo(1));
                Assert.That(restoredOrgans.Count(entry => entry.Organ.Category == HeartCategory), Is.EqualTo(1));
                Assert.That(restoredOrgans.All(entry => entry.Organ.Body == ling), Is.True,
                    "Restored organs must be inserted through the body container so organ systems receive insertion events.");
                Assert.That(entMan.System<MobStateSystem>().IsAlive(ling), Is.True);
            });
        });
    }

    [Test]
    [PairConfig(nameof(PsDisconnected))]
    public async Task EvolutionResetRemovesPurchasesAndRestoresTwentyPoints()
    {
        var pair = Pair;
        var server = pair.Server;
        var entMan = server.EntMan;
        var testMap = await pair.CreateTestMap();
        EntityUid ling = default;
        EntityUid purchase = default;

        await server.WaitAssertion(() =>
        {
            ling = entMan.SpawnEntity("MobLing", testMap.GridCoords);
        });
        await pair.RunTicksSync(1);

        await server.WaitAssertion(() =>
        {
            var actions = entMan.System<SharedActionsSystem>();
            purchase = actions.AddAction(ling, SwapFormsAction.Id)!.Value;

            var store = entMan.GetComponent<StoreComponent>(ling);
            store.BoughtEntities.Add(purchase);
            store.Balance[EvolutionCurrency] = FixedPoint2.New(18);
            store.BalanceSpent[EvolutionCurrency] = FixedPoint2.New(2);
        });
        await pair.RunTicksSync(1);

        await server.WaitAssertion(() =>
        {
            var store = entMan.GetComponent<StoreComponent>(ling);
            var resources = entMan.GetComponent<ChangelingResourceComponent>(ling);
            Assert.That(resources.EvolutionPoints, Is.EqualTo(18),
                "The resource component must mirror the authoritative store balance.");
            Assert.That(entMan.System<ChangelingResourceSystem>().ResetEvolution((ling, resources)), Is.True);

            Assert.Multiple(() =>
            {
                Assert.That(resources.EvolutionPoints, Is.EqualTo(20));
                Assert.That(store.Balance[EvolutionCurrency], Is.EqualTo(FixedPoint2.New(20)));
                Assert.That(store.BalanceSpent, Is.Empty);
                Assert.That(store.BoughtEntities, Is.Empty);
            });
        });
        await pair.RunTicksSync(1);

        await server.WaitAssertion(() =>
        {
            Assert.That(entMan.Deleted(purchase), Is.True);
        });
    }
}
