// SS220 Changeling
using System.Linq;
using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.Changeling.Components;
using Content.Server.Changeling.Systems;
using Content.Server.Doors.Systems;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Changeling.Components;
using Content.Shared.Changeling.Mutations;
using Content.Shared.Changeling.Systems;
using Content.Shared.Cloning;
using Content.Shared.Doors.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Inventory;
using Content.Shared.Physics;
using Content.Shared.Stealth.Components;
using Content.Shared.Standing;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Changeling;

[TestFixture]
public sealed class ChangelingTransformationStingSecurityTests : GameTest
{
    private static readonly ProtoId<CloningSettingsPrototype> ChangelingCloningSettings =
        "ChangelingCloningSettings";
    private static readonly EntProtoId CryogenicStingAction = "ActionChangelingCryogenicSting";
    private static readonly EntProtoId TransformationStingAction = "ActionChangelingTransformationSting";
    private static readonly EntProtoId ContortBodyAction = "ActionChangelingContortBody";

    [Test]
    [PairConfig(nameof(PsDisconnected))]
    public async Task RemovingTransformationStingRestoresVictimAndDeletesBackup()
    {
        var pair = Pair;
        var server = pair.Server;
        var entMan = server.EntMan;
        var testMap = await pair.CreateTestMap();
        EntityUid victim = default;
        EntityUid backup = default;
        const string disguisedName = "Temporary changeling disguise";
        string originalName = string.Empty;

        await server.WaitAssertion(() =>
        {
            victim = entMan.SpawnEntity("MobHuman", testMap.GridCoords);
        });
        await pair.RunTicksSync(1);

        await server.WaitAssertion(() =>
        {
            originalName = entMan.GetComponent<MetaDataComponent>(victim).EntityName;
            var settings = server.ProtoMan.Index(ChangelingCloningSettings);
            backup = entMan.System<ChangelingIdentitySystem>().CloneToPausedMap(settings, victim)!.Value;

            entMan.System<MetaDataSystem>().SetEntityName(victim, disguisedName);
            var transformation = entMan.AddComponent<ChangelingTransformationStingComponent>(victim);
            transformation.Backup = backup;
            transformation.CloningSettings = settings.ID;
            transformation.EndTime = TimeSpan.MaxValue;

            Assert.That(entMan.GetComponent<MetaDataComponent>(victim).EntityName, Is.EqualTo(disguisedName));
            entMan.RemoveComponent<ChangelingTransformationStingComponent>(victim);
            Assert.That(entMan.GetComponent<MetaDataComponent>(victim).EntityName, Is.EqualTo(originalName),
                "Removing the timed component early must restore the victim immediately.");
        });
        await pair.RunTicksSync(1);

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(entMan.HasComponent<ChangelingTransformationStingComponent>(victim), Is.False);
                Assert.That(entMan.EntityExists(backup), Is.False,
                    "The paused identity backup must not leak after external component removal.");
            });
        });
    }

    [Test]
    [PairConfig(nameof(PsDisconnected))]
    public async Task ContortionUsesMouseCollisionUntilStanding()
    {
        var pair = Pair;
        var server = pair.Server;
        var entMan = server.EntMan;
        var testMap = await pair.CreateTestMap();
        EntityUid ling = default;

        await server.WaitAssertion(() =>
        {
            ling = entMan.SpawnEntity("MobLing", testMap.GridCoords);
            var action = entMan.System<SharedActionsSystem>().AddAction(ling, ContortBodyAction.Id)!.Value;
            var contort = new ChangelingContortBodyActionEvent
            {
                Performer = ling,
                Action = (action, entMan.GetComponent<ActionComponent>(action)),
            };
            entMan.EventBus.RaiseLocalEvent(ling, contort);
            Assert.That(contort.Handled, Is.True);
        });
        await pair.RunTicksSync(1);

        await server.WaitAssertion(() =>
        {
            var lingFixture = entMan.GetComponent<FixturesComponent>(ling).Fixtures.Values.First();
            Assert.Multiple(() =>
            {
                Assert.That(entMan.HasComponent<ChangelingContortedComponent>(ling), Is.True);
                Assert.That(entMan.System<StandingStateSystem>().IsDown(ling), Is.True);
                Assert.That(lingFixture.CollisionMask, Is.EqualTo((int) CollisionGroup.SmallMobMask));
            });

            Assert.That(entMan.System<StandingStateSystem>().Stand(ling), Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(entMan.HasComponent<ChangelingContortedComponent>(ling), Is.False);
                Assert.That(entMan.System<StandingStateSystem>().IsDown(ling), Is.False);
                Assert.That(lingFixture.CollisionMask, Is.EqualTo((int) CollisionGroup.MobMask));
            });
        });
    }

    [Test]
    [PairConfig(nameof(PsDisconnected))]
    public async Task RemovingResourceComponentCleansUtilityStateAndRestoresEquipment()
    {
        var pair = Pair;
        var server = pair.Server;
        var entMan = server.EntMan;
        var testMap = await pair.CreateTestMap();
        EntityUid ling = default;
        EntityUid outerClothing = default;
        EntityUid headClothing = default;
        EntityUid suitVisual = default;
        EntityUid helmetVisual = default;

        await server.WaitAssertion(() =>
        {
            ling = entMan.SpawnEntity("MobLing", testMap.GridCoords);
            outerClothing = entMan.SpawnEntity("ClothingOuterArmorBasic", testMap.GridCoords);
            headClothing = entMan.SpawnEntity("ClothingHeadHelmetBasic", testMap.GridCoords);
        });
        await pair.RunTicksSync(1);

        await server.WaitAssertion(() =>
        {
            var inventory = entMan.System<InventorySystem>();
            Assert.That(
                inventory.TryEquip(ling, outerClothing, "outerClothing", silent: true, force: true),
                Is.True);
            Assert.That(
                inventory.TryEquip(ling, headClothing, "head", silent: true, force: true),
                Is.True);

            var suit = new ChangelingOrganicSpaceSuitActionEvent
            {
                Performer = ling,
            };
            entMan.EventBus.RaiseLocalEvent(ling, suit);
            Assert.That(suit.Handled, Is.True);

            var camouflage = new ChangelingChameleonSkinActionEvent
            {
                Performer = ling,
            };
            entMan.EventBus.RaiseLocalEvent(ling, camouflage);
            Assert.That(camouflage.Handled, Is.True);

            var utility = entMan.GetComponent<ChangelingUtilityStateComponent>(ling);
            suitVisual = utility.OrganicSpaceSuitVisual!.Value;
            helmetVisual = utility.OrganicSpaceSuitHelmetVisual!.Value;
            Assert.That(entMan.HasComponent<StealthComponent>(ling), Is.True);

            entMan.RemoveComponent<ChangelingResourceComponent>(ling);
        });
        await pair.RunTicksSync(2);

        await server.WaitAssertion(() =>
        {
            var inventory = entMan.System<InventorySystem>();
            Assert.Multiple(() =>
            {
                Assert.That(entMan.HasComponent<ChangelingUtilityStateComponent>(ling), Is.False);
                Assert.That(entMan.HasComponent<ChangelingOrganicSpaceSuitComponent>(ling), Is.False);
                Assert.That(entMan.HasComponent<ChangelingEnvironmentalProtectionComponent>(ling), Is.False);
                Assert.That(entMan.HasComponent<StealthComponent>(ling), Is.False);
                Assert.That(entMan.EntityExists(suitVisual), Is.False);
                Assert.That(entMan.EntityExists(helmetVisual), Is.False);
                Assert.That(inventory.TryGetSlotEntity(ling, "outerClothing", out var restoredOuter), Is.True);
                Assert.That(restoredOuter, Is.EqualTo(outerClothing));
                Assert.That(inventory.TryGetSlotEntity(ling, "head", out var restoredHead), Is.True);
                Assert.That(restoredHead, Is.EqualTo(headClothing));
            });
        });
    }

    [Test]
    [PairConfig(nameof(PsDisconnected))]
    public async Task UtilityActionsRejectForeignPerformersInvalidNumbersAndRemoteTargets()
    {
        var pair = Pair;
        var server = pair.Server;
        var entMan = server.EntMan;
        var testMap = await pair.CreateTestMap();
        EntityUid ling = default;
        EntityUid victim = default;
        EntityUid attacker = default;

        await server.WaitAssertion(() =>
        {
            ling = entMan.SpawnEntity("MobLing", testMap.GridCoords);
            victim = entMan.SpawnEntity("MobHuman", testMap.GridCoords);
            attacker = entMan.SpawnEntity("MobHuman", testMap.GridCoords);
        });
        await pair.RunTicksSync(1);

        await server.WaitAssertion(() =>
        {
            var resources = entMan.GetComponent<ChangelingResourceComponent>(ling);
            var initialChemicals = resources.Chemicals;
            var actions = entMan.System<SharedActionsSystem>();
            var stingAction = actions.AddAction(ling, CryogenicStingAction.Id)!.Value;
            var action = entMan.GetComponent<ActionComponent>(stingAction);

            var forged = new ChangelingCryogenicStingActionEvent
            {
                Performer = attacker,
                Target = victim,
                Action = (stingAction, action),
            };
            entMan.EventBus.RaiseLocalEvent(ling, forged);
            Assert.Multiple(() =>
            {
                Assert.That(forged.Handled, Is.False);
                Assert.That(entMan.HasComponent<ChangelingCryogenicStingComponent>(victim), Is.False);
                Assert.That(resources.Chemicals, Is.EqualTo(initialChemicals));
            });

            entMan.System<SharedTransformSystem>()
                .SetCoordinates(victim, testMap.GridCoords.Offset(new Vector2(10f, 0f)));
            var remote = new ChangelingCryogenicStingActionEvent
            {
                Performer = ling,
                Target = victim,
                Action = (stingAction, action),
            };
            entMan.EventBus.RaiseLocalEvent(ling, remote);
            Assert.Multiple(() =>
            {
                Assert.That(remote.Handled, Is.False);
                Assert.That(entMan.HasComponent<ChangelingCryogenicStingComponent>(victim), Is.False);
                Assert.That(resources.Chemicals, Is.EqualTo(initialChemicals));
            });

            var invalidConfiguration = new ChangelingVoidAdaptationActionEvent
            {
                Performer = ling,
                ChemicalCost = float.NaN,
                UpkeepCost = float.PositiveInfinity,
                TemperatureCoefficient = float.NegativeInfinity,
            };
            entMan.EventBus.RaiseLocalEvent(ling, invalidConfiguration);
            Assert.Multiple(() =>
            {
                Assert.That(invalidConfiguration.Handled, Is.False);
                Assert.That(resources.Chemicals, Is.EqualTo(initialChemicals));
                Assert.That(entMan.TryGetComponent<ChangelingUtilityStateComponent>(ling, out var utility) &&
                            utility.VoidAdaptation,
                    Is.False);
            });

            Assert.That(initialChemicals, Is.GreaterThan(FixedPoint2.Zero));
        });
    }

    [Test]
    [PairConfig(nameof(PsDisconnected))]
    public async Task TransformationIdentitySelectionIsOwnerOnlyAndRevalidatesRange()
    {
        var pair = Pair;
        var server = pair.Server;
        var entMan = server.EntMan;
        var testMap = await pair.CreateTestMap();
        EntityUid ling = default;
        EntityUid victim = default;
        EntityUid attacker = default;
        EntityUid transformationBackup = default;
        string victimName = string.Empty;
        string imposedName = string.Empty;

        await server.WaitAssertion(() =>
        {
            ling = entMan.SpawnEntity("MobLing", testMap.GridCoords);
            victim = entMan.SpawnEntity("MobHuman", testMap.GridCoords);
            attacker = entMan.SpawnEntity("MobHuman", testMap.GridCoords);
        });
        await pair.RunTicksSync(1);

        await server.WaitAssertion(() =>
        {
            var actions = entMan.System<SharedActionsSystem>();
            var transform = entMan.System<SharedTransformSystem>();
            var actionUid = actions.AddAction(ling, TransformationStingAction.Id)!.Value;
            var action = entMan.GetComponent<ActionComponent>(actionUid);
            var storedIdentity = entMan.GetComponent<ChangelingIdentityComponent>(ling)
                .ConsumedIdentities
                .Keys
                .First();
            Assert.That(entMan.HasComponent<ChangelingStoredIdentityComponent>(storedIdentity), Is.True);
            victimName = entMan.GetComponent<MetaDataComponent>(victim).EntityName;
            imposedName = entMan.GetComponent<MetaDataComponent>(storedIdentity).EntityName;

            void PrepareSting()
            {
                var activate = new ChangelingTransformationStingActionEvent
                {
                    Performer = ling,
                    Target = victim,
                    Action = (actionUid, action),
                    ChemicalCost = 0f,
                    StingWindup = TimeSpan.Zero,
                };
                entMan.EventBus.RaiseLocalEvent(ling, activate);
                Assert.That(activate.Handled, Is.True);
            }

            PrepareSting();
            var state = entMan.GetComponent<ChangelingUtilityStateComponent>(ling);
            var forged = new ChangelingTransformationStingIdentitySelectMessage(
                entMan.GetNetEntity(storedIdentity))
            {
                Actor = attacker,
            };
            entMan.EventBus.RaiseLocalEvent(ling, forged);

            Assert.Multiple(() =>
            {
                Assert.That(state.TransformationStingInProgress, Is.False,
                    "A foreign BUI actor must not start another changeling's sting.");
                Assert.That(state.PendingTransformationTarget, Is.EqualTo(victim),
                    "A forged message must not alter the owner's pending selection.");
            });

            transform.SetCoordinates(victim, testMap.GridCoords.Offset(new Vector2(10f, 0f)));
            var outOfRange = new ChangelingTransformationStingIdentitySelectMessage(
                entMan.GetNetEntity(storedIdentity))
            {
                Actor = ling,
            };
            entMan.EventBus.RaiseLocalEvent(ling, outOfRange);

            Assert.Multiple(() =>
            {
                Assert.That(state.TransformationStingInProgress, Is.False);
                Assert.That(state.PendingTransformationTarget, Is.Null,
                    "The target range must be revalidated when the identity is selected.");
            });

            transform.SetCoordinates(victim, testMap.GridCoords);
            PrepareSting();
            var authorized = new ChangelingTransformationStingIdentitySelectMessage(
                entMan.GetNetEntity(storedIdentity))
            {
                Actor = ling,
            };
            entMan.EventBus.RaiseLocalEvent(ling, authorized);

            Assert.That(
                state.TransformationStingInProgress ||
                entMan.HasComponent<ChangelingTransformationStingComponent>(victim),
                Is.True,
                "The owner should still be able to start a valid sting after rejected messages.");
        });

        await pair.RunTicksSync(2);
        await server.WaitAssertion(() =>
        {
            var transformation = entMan.GetComponent<ChangelingTransformationStingComponent>(victim);
            transformationBackup = transformation.Backup!.Value;
            Assert.Multiple(() =>
            {
                Assert.That(entMan.GetComponent<MetaDataComponent>(victim).EntityName, Is.EqualTo(imposedName));
                Assert.That(entMan.EntityExists(transformationBackup), Is.True);
            });

            transformation.EndTime = TimeSpan.Zero;
        });

        await pair.RunTicksSync(2);
        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(entMan.HasComponent<ChangelingTransformationStingComponent>(victim), Is.False);
                Assert.That(entMan.GetComponent<MetaDataComponent>(victim).EntityName, Is.EqualTo(victimName),
                    "The victim's original identity state must be restored after the sting expires.");
                Assert.That(entMan.EntityExists(transformationBackup), Is.False,
                    "The paused-map backup must be deleted after restoration.");
            });
        });
    }
}
