// SS220 Changeling
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.Changeling.Components;
using Content.Server.Changeling.Objectives;
using Content.Server.Polymorph.Components;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Changeling.Components;
using Content.Shared.Changeling.Mutations;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Store.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Changeling;

[TestFixture]
public sealed class ChangelingLesserFormLifecycleTests : GameTest
{
    private static readonly EntProtoId HumanFormAction = "ActionChangelingHumanForm";
    private static readonly EntProtoId LesserFormAction = "ActionChangelingLesserForm";
    private static readonly EntProtoId OrganicSuitAction = "ActionChangelingOrganicSpaceSuit";
    private static readonly EntProtoId ArmBladeAction = "ActionChangelingArmBlade";
    private static readonly EntProtoId OriginalOuterClothing = "ClothingOuterWinterCoat";
    private static readonly EntProtoId OriginalHeadClothing = "ClothingHeadHatHardhatBlue";

    [Test]
    [PairConfig(nameof(PsDisconnected))]
    public async Task LesserFormCleansBodyEffectsAndPreservesHumanFormActionOnAutomaticRevert()
    {
        var pair = Pair;
        var server = pair.Server;
        var entMan = server.EntMan;
        var testMap = await pair.CreateTestMap();
        EntityUid ling = default;
        EntityUid child = default;
        EntityUid humanAction = default;
        EntityUid outerClothing = default;
        EntityUid headClothing = default;
        EntityUid organicSuitVisual = default;
        EntityUid organicHelmetVisual = default;
        EntityUid armBlade = default;
        EntityUid mindId = default;
        EntityUid[] purchasedActions = Array.Empty<EntityUid>();

        await server.WaitAssertion(() =>
        {
            ling = entMan.SpawnEntity("MobLing", testMap.GridCoords);
            var mindSystem = entMan.System<SharedMindSystem>();
            var mind = mindSystem.CreateMind(null);
            mindId = mind.Owner;
            mindSystem.TransferTo(mind, ling, mind: mind.Comp);
        });
        await pair.RunTicksSync(1);

        await server.WaitAssertion(() =>
        {
            var actions = entMan.System<SharedActionsSystem>();
            var store = entMan.GetComponent<StoreComponent>(ling);

            EntityUid AddPurchasedAction(EntProtoId prototype)
            {
                var action = actions.AddAction(ling, prototype.Id);
                Assert.That(action, Is.Not.Null, $"Failed to grant {prototype.Id}.");
                store.BoughtEntities.Add(action!.Value);
                return action.Value;
            }

            humanAction = AddPurchasedAction(HumanFormAction);
            var lesserAction = AddPurchasedAction(LesserFormAction);
            var organicSuitAction = AddPurchasedAction(OrganicSuitAction);
            var armBladeAction = AddPurchasedAction(ArmBladeAction);
            purchasedActions = new[] { humanAction, lesserAction, organicSuitAction, armBladeAction };

            var inventory = entMan.System<InventorySystem>();
            outerClothing = entMan.SpawnEntity(OriginalOuterClothing, testMap.GridCoords);
            headClothing = entMan.SpawnEntity(OriginalHeadClothing, testMap.GridCoords);
            Assert.That(inventory.TryEquip(ling, outerClothing, "outerClothing", silent: true, force: true), Is.True);
            Assert.That(inventory.TryEquip(ling, headClothing, "head", silent: true, force: true), Is.True);

            var suit = new ChangelingOrganicSpaceSuitActionEvent
            {
                Performer = ling,
                Action = (organicSuitAction, entMan.GetComponent<ActionComponent>(organicSuitAction)),
            };
            entMan.EventBus.RaiseLocalEvent(ling, suit);
            Assert.That(suit.Handled, Is.True);

            var utilityState = entMan.GetComponent<ChangelingUtilityStateComponent>(ling);
            organicSuitVisual = utilityState.OrganicSpaceSuitVisual!.Value;
            organicHelmetVisual = utilityState.OrganicSpaceSuitHelmetVisual!.Value;

            var blade = new ChangelingArmBladeActionEvent
            {
                Performer = ling,
                Action = (armBladeAction, entMan.GetComponent<ActionComponent>(armBladeAction)),
                ChemicalCost = 0f,
            };
            entMan.EventBus.RaiseLocalEvent(ling, blade);
            Assert.That(blade.Handled, Is.True);
            armBlade = entMan.GetComponent<ChangelingMutationStateComponent>(ling).ArmBlade!.Value;

            var lesser = new ChangelingLesserFormActionEvent
            {
                Performer = ling,
                Action = (lesserAction, entMan.GetComponent<ActionComponent>(lesserAction)),
            };
            entMan.EventBus.RaiseLocalEvent(ling, lesser);
            Assert.That(lesser.Handled, Is.True);

            var query = entMan.AllEntityQueryEnumerator<PolymorphedEntityComponent>();
            while (query.MoveNext(out var uid, out var polymorphed))
            {
                if (polymorphed.Parent == ling)
                {
                    child = uid;
                    break;
                }
            }

            Assert.That(child, Is.Not.EqualTo(default(EntityUid)), "Lesser Form child was not created.");
            Assert.That(entMan.HasComponent<ChangelingLesserFormComponent>(child), Is.True);
            Assert.That(actions.GetAction(humanAction)!.Value.Comp.AttachedEntity, Is.EqualTo(child));
        });
        await pair.RunTicksSync(1);

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(entMan.Deleted(organicSuitVisual), Is.True,
                    "Organic suit visual must not become loot after polymorphing.");
                Assert.That(entMan.Deleted(organicHelmetVisual), Is.True,
                    "Organic helmet visual must not become loot after polymorphing.");
                Assert.That(entMan.Deleted(armBlade), Is.True,
                    "Arm blade must not become loot after polymorphing.");
                Assert.That(entMan.Deleted(outerClothing), Is.False,
                    "Stored outer clothing must be released before polymorphing.");
                Assert.That(entMan.Deleted(headClothing), Is.False,
                    "Stored head clothing must be released before polymorphing.");
                Assert.That(entMan.HasComponent<ChangelingEnvironmentalProtectionComponent>(ling), Is.False);
            });

            foreach (var action in purchasedActions)
                Assert.That(entMan.Deleted(action), Is.False, "Body cleanup must preserve purchased actions.");

            var mind = entMan.GetComponent<MindComponent>(mindId);
            Assert.That(mind.OwnedEntity, Is.EqualTo(child));
            var objective = entMan.SpawnEntity("ChangelingAbsorbDnaObjective", testMap.GridCoords);
            var assigned = new ObjectiveAssignedEvent(mindId, mind);
            entMan.EventBus.RaiseLocalEvent(objective, ref assigned);
            var condition = entMan.GetComponent<ChangelingAbsorbDnaConditionComponent>(objective);
            var identity = entMan.GetComponent<ChangelingIdentityComponent>(ling);
            for (var i = 0; i < condition.TargetGenomes; i++)
                identity.AbsorbedGenomes.Add($"lesser-form-objective-{i}");

            var progress = new ObjectiveGetProgressEvent(mindId, mind);
            entMan.EventBus.RaiseLocalEvent(objective, ref progress);
            Assert.That(progress.Progress, Is.EqualTo(1f),
                "Cumulative DNA progress must resolve from the Lesser Form polymorph parent.");
            entMan.QueueDeleteEntity(objective);

            entMan.QueueDeleteEntity(child);
        });
        await pair.RunTicksSync(2);

        await server.WaitAssertion(() =>
        {
            var actions = entMan.System<SharedActionsSystem>();
            var store = entMan.GetComponent<StoreComponent>(ling);
            Assert.Multiple(() =>
            {
                Assert.That(entMan.Deleted(ling), Is.False, "Deleting Lesser Form must restore its parent.");
                Assert.That(entMan.Deleted(humanAction), Is.False,
                    "Human Form action must survive automatic polymorph reversion.");
                Assert.That(actions.GetAction(humanAction)!.Value.Comp.AttachedEntity, Is.EqualTo(ling));
                Assert.That(store.BoughtEntities.Contains(humanAction), Is.True);
                Assert.That(store.BoughtEntities.Count(action =>
                        !entMan.Deleted(action) &&
                        entMan.GetComponent<MetaDataComponent>(action).EntityPrototype?.ID == HumanFormAction.Id),
                    Is.EqualTo(1),
                    "Automatic reversion must preserve exactly one Human Form action.");
            });
        });
    }
}
