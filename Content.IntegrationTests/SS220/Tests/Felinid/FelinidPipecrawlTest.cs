using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server.Body.Systems;
using Content.Server.Disposal.Tube;
using Content.Server.Disposal.Unit;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Disposal.Components;
using Content.Shared.Disposal.Tube;
using Content.Shared.DoAfter;
using Content.Shared.Eye;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Nutrition;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.SS220.Felinid;
using Content.Shared.SS220.Felinid.Components;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Components;

namespace Content.IntegrationTests.SS220.Tests.Felinid;

[TestFixture]
[TestOf(typeof(DisposalPipeCrawlerComponent))]
public sealed class FelinidPipecrawlTest : GameTest
{
    [TestPrototypes]
    private const string Prototypes = """
- type: entity
  parent: InventoryBase
  id: FelinidPipecrawlTestMob
  components:
  - type: Sprite
  - type: Appearance
  - type: Body
  - type: Damageable
    damageContainer: Biological
  - type: MobState
  - type: MobThresholds
    thresholds:
      0: Alive
      100: Critical
      200: Dead
  - type: InputMover
  - type: MobMover
  - type: Hands
    hands:
      hand_right:
        location: Right
      hand_left:
        location: Left
    sortedHands:
    - hand_right
    - hand_left
  - type: ContainerContainer
  - type: Blindable
  - type: Eye
  - type: ContentEye
  - type: Physics
    bodyType: KinematicController
  - type: Fixtures
    fixtures:
      fix1:
        shape:
          !type:PhysShapeCircle
          radius: 0.35
  - type: DisposalPipeCrawler
    transitTime: 0.1
    extractionDelay: 0
    enterCooldown: 120
    exitCooldown: 600
""";

    [Test]
    public async Task HeldMovementTraversesConnectedTubesAndManualReleaseUsesDisposalFlow()
    {
        var server = Pair.Server;
        var testMap = await Pair.CreateTestMap();

        var entMan = server.ResolveDependency<IEntityManager>();
        var actions = entMan.System<SharedActionsSystem>();
        var maps = entMan.System<SharedMapSystem>();
        var disposalTubes = entMan.System<DisposalTubeSystem>();
        var disposalUnits = entMan.System<DisposalUnitSystem>();
        var transforms = entMan.System<SharedTransformSystem>();
        var timing = server.Timing;

        EntityUid mob = default;
        EntityUid entry = default;
        EntityUid secondTube = default;
        Direction direction = Direction.Invalid;

        await server.WaitAssertion(() =>
        {
            var coordinates = testMap.GridCoords;
            mob = entMan.SpawnEntity("FelinidPipecrawlTestMob", coordinates);
            var disposalUnit = entMan.SpawnEntity("DisposalUnit", coordinates);
            entry = entMan.SpawnEntity("DisposalTrunk", coordinates);

            EnsureAnchored(disposalUnit, entMan, transforms);
            EnsureAnchored(entry, entMan, transforms);

            var directions = new GetDisposalsConnectableDirectionsEvent();
            entMan.EventBus.RaiseLocalEvent(entry, ref directions);
            direction = directions.Connectable.Single();

            var firstTubeCoordinates = maps.DirectionToGrid(
                testMap.Grid.Owner,
                testMap.Grid.Comp,
                entMan.GetComponent<TransformComponent>(entry).Coordinates,
                direction);
            maps.SetTile(testMap.Grid.Owner, testMap.Grid.Comp, firstTubeCoordinates, testMap.Tile.Tile);
            var firstTube = entMan.SpawnEntity("DisposalPipe", firstTubeCoordinates);
            var secondTubeCoordinates = maps.DirectionToGrid(
                testMap.Grid.Owner,
                testMap.Grid.Comp,
                entMan.GetComponent<TransformComponent>(firstTube).Coordinates,
                direction);
            maps.SetTile(testMap.Grid.Owner, testMap.Grid.Comp, secondTubeCoordinates, testMap.Tile.Tile);
            secondTube = entMan.SpawnEntity("DisposalPipe", secondTubeCoordinates);
            EnsureAnchored(firstTube, entMan, transforms);
            EnsureAnchored(secondTube, entMan, transforms);
            Assert.That(disposalTubes.NextTubeFor(entry, direction), Is.EqualTo(firstTube));
            Assert.That(disposalTubes.NextTubeFor(firstTube, direction), Is.EqualTo(secondTube));

            var unit = entMan.GetComponent<DisposalUnitComponent>(disposalUnit);
            var pipecrawl = entMan.GetComponent<DisposalPipeCrawlerComponent>(mob);
            Assert.That(pipecrawl.ActionEntity, Is.Null);
            Assert.That(disposalUnits.TryInsert(disposalUnit, mob, null), Is.True);
            Assert.That(unit.Container.Contains(mob), Is.True);

            Assert.That(pipecrawl.ActionEntity, Is.Not.Null);
            var action = entMan.GetComponent<ActionComponent>(pipecrawl.ActionEntity!.Value);
            Assert.That(action.CheckCanInteract, Is.False);
            actions.PerformAction(mob, (pipecrawl.ActionEntity.Value, action));

            Assert.Multiple(() =>
            {
                Assert.That(pipecrawl.InsidePipe, Is.True);
                Assert.That(pipecrawl.CurrentTube, Is.EqualTo(entry));
                Assert.That(unit.Container.Contains(mob), Is.False);
                Assert.That(entMan.GetComponent<InputMoverComponent>(mob).CanMove, Is.False);
                Assert.That(entMan.GetComponent<PhysicsComponent>(mob).CanCollide, Is.False);
                Assert.That(entMan.GetComponent<BlindableComponent>(mob).IsBlind, Is.True);
                var eye = entMan.GetComponent<EyeComponent>(mob);
                Assert.That(eye.VisibilityMask & (int) VisibilityFlags.Subfloor, Is.Not.Zero);
                Assert.That(eye.PvsScale, Is.EqualTo(1f));
                Assert.That(actions.IsCooldownActive(action), Is.False);
            });

            var inhale = new InhaleLocationEvent();
            entMan.EventBus.RaiseLocalEvent(mob, ref inhale);
            Assert.That(inhale.Gas, Is.SameAs(pipecrawl.Air));

            var mover = entMan.GetComponent<InputMoverComponent>(mob);
            mover.HeldMoveButtons = ToMoveButton(direction);
            var moveEvent = new MoveInputEvent((mob, mover), MoveButtons.None);
            entMan.EventBus.RaiseLocalEvent(mob, ref moveEvent);
        });

        await server.WaitRunTicks(10);

        await server.WaitAssertion(() =>
        {
            var pipecrawl = entMan.GetComponent<DisposalPipeCrawlerComponent>(mob);
            Assert.Multiple(() =>
            {
                Assert.That(pipecrawl.InsidePipe, Is.True);
                Assert.That(pipecrawl.CurrentTube, Is.EqualTo(secondTube));
                Assert.That(pipecrawl.NextTube, Is.Null);
                Assert.That(transforms.GetWorldPosition(mob), Is.EqualTo(transforms.GetWorldPosition(secondTube)));
                Assert.That(entMan.GetComponent<InputMoverComponent>(mob).CanMove, Is.False);
            });

            var actionUid = pipecrawl.ActionEntity;
            Assert.That(actionUid, Is.Not.Null);
            var exitAction = entMan.GetComponent<ActionComponent>(actionUid!.Value);
            actions.PerformAction(mob, (actionUid.Value, exitAction));

            var disposed = entMan.GetComponent<BeingDisposedComponent>(mob);
            var holder = entMan.GetComponent<DisposalHolderComponent>(disposed.Holder);

            Assert.Multiple(() =>
            {
                Assert.That(pipecrawl.InsidePipe, Is.False);
                Assert.That(pipecrawl.CurrentTube, Is.Null);
                Assert.That(entMan.GetComponent<InputMoverComponent>(mob).CanMove, Is.True);
                Assert.That(entMan.GetComponent<PhysicsComponent>(mob).CanCollide, Is.False);
                Assert.That(entMan.GetComponent<BlindableComponent>(mob).IsBlind, Is.False);
                Assert.That(entMan.GetComponent<EyeComponent>(mob).VisibilityMask & (int) VisibilityFlags.Subfloor,
                    Is.Zero);
                Assert.That(entMan.GetComponent<EyeComponent>(mob).PvsScale, Is.EqualTo(1f));
                Assert.That(pipecrawl.ActionEntity, Is.Null);
                Assert.That(holder.Container.Contains(mob), Is.True);
                Assert.That(holder.CurrentTube, Is.EqualTo(secondTube));
                Assert.That(pipecrawl.NextEntryAllowed - timing.CurTime,
                    Is.EqualTo(TimeSpan.FromMinutes(10)).Within(TimeSpan.FromSeconds(1)));
            });
        });
    }

    [Test]
    public async Task TrunkWithoutRealDisposalUnitDoesNotAllowExit()
    {
        var server = Pair.Server;
        var testMap = await Pair.CreateTestMap();

        var entMan = server.ResolveDependency<IEntityManager>();
        var actions = entMan.System<SharedActionsSystem>();
        var disposalUnits = entMan.System<DisposalUnitSystem>();
        var transforms = entMan.System<SharedTransformSystem>();

        await server.WaitAssertion(() =>
        {
            var coordinates = testMap.GridCoords;
            var mob = entMan.SpawnEntity("FelinidPipecrawlTestMob", coordinates);
            var disposalUnit = entMan.SpawnEntity("DisposalUnit", coordinates);
            var entry = entMan.SpawnEntity("DisposalTrunk", coordinates);

            EnsureAnchored(disposalUnit, entMan, transforms);
            EnsureAnchored(entry, entMan, transforms);
            Assert.That(disposalUnits.TryInsert(disposalUnit, mob, null), Is.True);

            var pipecrawl = entMan.GetComponent<DisposalPipeCrawlerComponent>(mob);
            var enterAction = entMan.GetComponent<ActionComponent>(pipecrawl.ActionEntity!.Value);
            actions.PerformAction(mob, (pipecrawl.ActionEntity.Value, enterAction));
                Assert.That(pipecrawl.InsidePipe, Is.True);

            entMan.DeleteEntity(disposalUnit);
            var toilet = entMan.SpawnEntity("ToiletEmpty", coordinates);
            EnsureAnchored(toilet, entMan, transforms);

            var exitAction = entMan.GetComponent<ActionComponent>(pipecrawl.ActionEntity!.Value);
            actions.PerformAction(mob, (pipecrawl.ActionEntity.Value, exitAction));

            Assert.Multiple(() =>
            {
                Assert.That(pipecrawl.InsidePipe, Is.True);
                Assert.That(pipecrawl.CurrentTube, Is.EqualTo(entry));
                Assert.That(entMan.HasComponent<BeingDisposedComponent>(mob), Is.False);
                Assert.That(entMan.GetComponent<PhysicsComponent>(mob).CanCollide, Is.False);
            });
        });
    }

    [Test]
    public async Task OwnItemsRemainUsableAndUnequippedBeltTransfersIntoInventory()
    {
        var server = Pair.Server;
        var testMap = await Pair.CreateTestMap();

        var entMan = server.ResolveDependency<IEntityManager>();
        var actions = entMan.System<SharedActionsSystem>();
        var disposalUnits = entMan.System<DisposalUnitSystem>();
        var hands = entMan.System<SharedHandsSystem>();
        var inventory = entMan.System<InventorySystem>();
        var storage = entMan.System<StorageSystem>();
        var transforms = entMan.System<SharedTransformSystem>();

        await server.WaitAssertion(() =>
        {
            var coordinates = testMap.GridCoords;
            var mob = entMan.SpawnEntity("FelinidPipecrawlTestMob", coordinates);
            var disposalUnit = entMan.SpawnEntity("DisposalUnit", coordinates);
            var entry = entMan.SpawnEntity("DisposalTrunk", coordinates);
            var belt = entMan.SpawnEntity("ClothingBeltUtility", coordinates);
            var backpack = entMan.SpawnEntity("ClothingBackpack", coordinates);
            var screwdriver = entMan.SpawnEntity("Screwdriver", coordinates);

            EnsureAnchored(disposalUnit, entMan, transforms);
            EnsureAnchored(entry, entMan, transforms);
            Assert.That(inventory.TryEquip(mob, belt, "belt"), Is.True);
            Assert.That(inventory.TryEquip(mob, backpack, "back"), Is.True);
            Assert.That(hands.TryPickupAnyHand(mob, screwdriver), Is.True);
            Assert.That(disposalUnits.TryInsert(disposalUnit, mob, null), Is.True);

            var pipecrawl = entMan.GetComponent<DisposalPipeCrawlerComponent>(mob);
            var enterAction = entMan.GetComponent<ActionComponent>(pipecrawl.ActionEntity!.Value);
            actions.PerformAction(mob, (pipecrawl.ActionEntity.Value, enterAction));
                Assert.That(pipecrawl.InsidePipe, Is.True);

            var selfInteraction = new InteractionAttemptEvent(mob, mob);
            entMan.EventBus.RaiseLocalEvent(mob, ref selfInteraction);
            Assert.That(selfInteraction.Cancelled, Is.False);

            var external = entMan.SpawnEntity("Wrench", coordinates);
            var externalInteraction = new InteractionAttemptEvent(mob, external);
            entMan.EventBus.RaiseLocalEvent(mob, ref externalInteraction);
            Assert.That(externalInteraction.Cancelled, Is.True);

            Assert.That(inventory.CanUnequip(mob, "belt", out var unequipReason), Is.True, unequipReason);
            Assert.That(inventory.TryUnequip(mob, "belt", out var removedBelt), Is.True);
            Assert.That(removedBelt, Is.EqualTo(belt));
            hands.PickupOrDrop(mob, belt);

            var useOwnItem = new UseAttemptEvent(mob, screwdriver);
            entMan.EventBus.RaiseLocalEvent(mob, useOwnItem);
            Assert.That(useOwnItem.Cancelled, Is.False);

            var handsComponent = entMan.GetComponent<HandsComponent>(mob);
            var backpackStorage = entMan.GetComponent<StorageComponent>(backpack);
            Assert.That(hands.IsHolding((mob, handsComponent), screwdriver, out var screwdriverHand), Is.True);
            hands.SetActiveHand((mob, handsComponent), screwdriverHand);
            Assert.That(storage.PlayerInsertHeldEntity((backpack, backpackStorage), (mob, handsComponent)), Is.True);

            Assert.Multiple(() =>
            {
                Assert.That(backpackStorage.Container.Contains(screwdriver), Is.True);
                Assert.That(hands.IsHolding((mob, handsComponent), belt), Is.True);
                Assert.That(hands.TryPickupAnyHand(mob, external), Is.False);
            });
        });
    }

    [Test]
    public async Task FoodInOwnHandCanBeEatenWhilePipecrawling()
    {
        var server = Pair.Server;
        var testMap = await Pair.CreateTestMap();

        var entMan = server.ResolveDependency<IEntityManager>();
        var actionBlocker = entMan.System<ActionBlockerSystem>();
        var actions = entMan.System<SharedActionsSystem>();
        var disposalUnits = entMan.System<DisposalUnitSystem>();
        var hands = entMan.System<SharedHandsSystem>();
        var ingestion = entMan.System<IngestionSystem>();
        var interactions = entMan.System<SharedInteractionSystem>();
        var transforms = entMan.System<SharedTransformSystem>();

        await server.WaitAssertion(() =>
        {
            var coordinates = testMap.GridCoords;
            var mob = entMan.SpawnEntity("MobFelinid", coordinates);
            var disposalUnit = entMan.SpawnEntity("DisposalUnit", coordinates);
            var entry = entMan.SpawnEntity("DisposalTrunk", coordinates);
            var food = entMan.SpawnEntity("FoodApple", coordinates);

            EnsureAnchored(disposalUnit, entMan, transforms);
            EnsureAnchored(entry, entMan, transforms);
            Assert.That(hands.TryPickupAnyHand(mob, food), Is.True);

            var handsComponent = entMan.GetComponent<HandsComponent>(mob);
            Assert.That(hands.IsHolding((mob, handsComponent), food, out var foodHand), Is.True);
            hands.SetActiveHand((mob, handsComponent), foodHand);
            Assert.That(disposalUnits.TryInsert(disposalUnit, mob, null), Is.True);

            var pipecrawl = entMan.GetComponent<DisposalPipeCrawlerComponent>(mob);
            var actionUid = pipecrawl.ActionEntity;
            Assert.That(actionUid, Is.Not.Null);
            var action = entMan.GetComponent<ActionComponent>(actionUid!.Value);
            actions.PerformAction(mob, (actionUid.Value, action));
                Assert.That(pipecrawl.InsidePipe, Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(hands.GetActiveItem((mob, handsComponent)), Is.EqualTo(food));
                Assert.That(actionBlocker.CanInteract(mob, mob), Is.True);
                Assert.That(actionBlocker.CanUseHeldEntity(mob, food), Is.True);
                Assert.That(interactions.IsAccessible(mob, mob), Is.True);
                Assert.That(interactions.InRangeUnobstructed(mob, mob), Is.True);
                Assert.That(ingestion.CanIngest(mob, food), Is.True);
            });

            interactions.UserInteraction(
                mob,
                entMan.GetComponent<TransformComponent>(mob).Coordinates,
                mob);

            var doAfter = entMan.GetComponent<DoAfterComponent>(mob);
            Assert.That(doAfter.DoAfters.Values.Any(entry => entry.Args.Event is EatingDoAfterEvent), Is.True);
        });
    }

    [Test]
    public async Task ExtractionCompletionSafelyRestoresTheCrawler()
    {
        var server = Pair.Server;
        var testMap = await Pair.CreateTestMap();

        var entMan = server.ResolveDependency<IEntityManager>();
        var actions = entMan.System<SharedActionsSystem>();
        var disposalUnits = entMan.System<DisposalUnitSystem>();
        var transforms = entMan.System<SharedTransformSystem>();

        EntityUid mob = default;
        EntityUid entry = default;

        await server.WaitAssertion(() =>
        {
            var coordinates = testMap.GridCoords;
            mob = entMan.SpawnEntity("FelinidPipecrawlTestMob", coordinates);
            var disposalUnit = entMan.SpawnEntity("DisposalUnit", coordinates);
            entry = entMan.SpawnEntity("DisposalTrunk", coordinates);

            EnsureAnchored(disposalUnit, entMan, transforms);
            EnsureAnchored(entry, entMan, transforms);

            Assert.That(disposalUnits.TryInsert(disposalUnit, mob, null), Is.True);

            var pipecrawl = entMan.GetComponent<DisposalPipeCrawlerComponent>(mob);
            Assert.That(pipecrawl.ActionEntity, Is.Not.Null);
            var action = entMan.GetComponent<ActionComponent>(pipecrawl.ActionEntity!.Value);
            actions.PerformAction(mob, (pipecrawl.ActionEntity.Value, action));
                Assert.That(pipecrawl.InsidePipe, Is.True);

            var wrench = entMan.SpawnEntity("Wrench", coordinates);
            entMan.EnsureComponent<DoAfterComponent>(wrench);
            var interaction = new InteractUsingEvent(
                wrench,
                wrench,
                entry,
                entMan.GetComponent<TransformComponent>(entry).Coordinates);
            entMan.EventBus.RaiseLocalEvent(entry, interaction);
            Assert.That(interaction.Handled, Is.True);
        });

        await server.WaitRunTicks(2);

        await server.WaitAssertion(() =>
        {
            var pipecrawl = entMan.GetComponent<DisposalPipeCrawlerComponent>(mob);
            Assert.Multiple(() =>
            {
                Assert.That(pipecrawl.InsidePipe, Is.False);
                Assert.That(pipecrawl.CurrentTube, Is.Null);
                Assert.That(entMan.GetComponent<InputMoverComponent>(mob).CanMove, Is.True);
                Assert.That(entMan.GetComponent<PhysicsComponent>(mob).CanCollide, Is.True);
                Assert.That(transforms.GetWorldPosition(mob), Is.EqualTo(transforms.GetWorldPosition(entry)));
            });
        });
    }

    [Test]
    public async Task ExitingAtEntryReturnsToDisposalUnitWithoutFlush()
    {
        var server = Pair.Server;
        var testMap = await Pair.CreateTestMap();

        var entMan = server.ResolveDependency<IEntityManager>();
        var actions = entMan.System<SharedActionsSystem>();
        var disposalUnits = entMan.System<DisposalUnitSystem>();
        var transforms = entMan.System<SharedTransformSystem>();
        var timing = server.Timing;

        await server.WaitAssertion(() =>
        {
            var coordinates = testMap.GridCoords;
            var mob = entMan.SpawnEntity("FelinidPipecrawlTestMob", coordinates);
            var disposalUnit = entMan.SpawnEntity("DisposalUnit", coordinates);
            var entry = entMan.SpawnEntity("DisposalTrunk", coordinates);

            EnsureAnchored(disposalUnit, entMan, transforms);
            EnsureAnchored(entry, entMan, transforms);

            var unit = entMan.GetComponent<DisposalUnitComponent>(disposalUnit);
            Assert.That(disposalUnits.TryInsert(disposalUnit, mob, null), Is.True);

            var pipecrawl = entMan.GetComponent<DisposalPipeCrawlerComponent>(mob);
            var enterActionUid = pipecrawl.ActionEntity;
            Assert.That(enterActionUid, Is.Not.Null);
            var enterAction = entMan.GetComponent<ActionComponent>(enterActionUid!.Value);
            actions.PerformAction(mob, (enterActionUid.Value, enterAction));

                Assert.That(pipecrawl.InsidePipe, Is.True);
            Assert.That(actions.IsCooldownActive(enterAction), Is.False);

            var exitActionUid = pipecrawl.ActionEntity;
            Assert.That(exitActionUid, Is.Not.Null);
            var exitAction = entMan.GetComponent<ActionComponent>(exitActionUid!.Value);
            Assert.That(exitAction.CheckCanInteract, Is.False);
            actions.PerformAction(mob, (exitActionUid.Value, exitAction));

            Assert.Multiple(() =>
            {
                Assert.That(pipecrawl.InsidePipe, Is.False);
                Assert.That(unit.Container.Contains(mob), Is.True);
                Assert.That(entMan.GetComponent<PhysicsComponent>(mob).CanCollide, Is.False);
                Assert.That(pipecrawl.NextEntryAllowed - timing.CurTime,
                    Is.EqualTo(TimeSpan.FromMinutes(2)).Within(TimeSpan.FromSeconds(1)));
                Assert.That(pipecrawl.ActionEntity, Is.Not.Null);
            });

            var cooldownAction = entMan.GetComponent<ActionComponent>(pipecrawl.ActionEntity!.Value);
            Assert.That(actions.IsCooldownActive(cooldownAction), Is.True);
        });
    }

    [TestCase(MobState.Critical)]
    [TestCase(MobState.Dead)]
    public async Task IncapacitatedCrawlerContinuesInStandardDisposals(MobState targetState)
    {
        var server = Pair.Server;
        var testMap = await Pair.CreateTestMap();

        var entMan = server.ResolveDependency<IEntityManager>();
        var actions = entMan.System<SharedActionsSystem>();
        var disposalTubes = entMan.System<DisposalTubeSystem>();
        var disposalUnits = entMan.System<DisposalUnitSystem>();
        var maps = entMan.System<SharedMapSystem>();
        var mobStates = entMan.System<MobStateSystem>();
        var transforms = entMan.System<SharedTransformSystem>();

        EntityUid holderUid = default;
        EntityUid pipe = default;

        await server.WaitAssertion(() =>
        {
            var coordinates = testMap.GridCoords;
            var mob = entMan.SpawnEntity("FelinidPipecrawlTestMob", coordinates);
            var disposalUnit = entMan.SpawnEntity("DisposalUnit", coordinates);
            var entry = entMan.SpawnEntity("DisposalTrunk", coordinates);

            EnsureAnchored(disposalUnit, entMan, transforms);
            EnsureAnchored(entry, entMan, transforms);

            var directions = new GetDisposalsConnectableDirectionsEvent();
            entMan.EventBus.RaiseLocalEvent(entry, ref directions);
            var direction = directions.Connectable.Single();
            var pipeCoordinates = maps.DirectionToGrid(
                testMap.Grid.Owner,
                testMap.Grid.Comp,
                entMan.GetComponent<TransformComponent>(entry).Coordinates,
                direction);
            maps.SetTile(testMap.Grid.Owner, testMap.Grid.Comp, pipeCoordinates, testMap.Tile.Tile);
            pipe = entMan.SpawnEntity("DisposalPipe", pipeCoordinates);
            EnsureAnchored(pipe, entMan, transforms);
            Assert.That(disposalTubes.NextTubeFor(entry, direction), Is.EqualTo(pipe));

            Assert.That(disposalUnits.TryInsert(disposalUnit, mob, null), Is.True);
            var pipecrawl = entMan.GetComponent<DisposalPipeCrawlerComponent>(mob);
            var actionUid = pipecrawl.ActionEntity;
            Assert.That(actionUid, Is.Not.Null);
            var action = entMan.GetComponent<ActionComponent>(actionUid!.Value);
            actions.PerformAction(mob, (actionUid.Value, action));
                Assert.That(pipecrawl.InsidePipe, Is.True);

            mobStates.ChangeMobState(mob, targetState);

            var disposed = entMan.GetComponent<BeingDisposedComponent>(mob);
            holderUid = disposed.Holder;
            var holder = entMan.GetComponent<DisposalHolderComponent>(holderUid);
            Assert.Multiple(() =>
            {
                Assert.That(pipecrawl.InsidePipe, Is.False);
                Assert.That(pipecrawl.CurrentTube, Is.Null);
                Assert.That(holder.Container.Contains(mob), Is.True);
                Assert.That(holder.CurrentTube, Is.EqualTo(entry));
                Assert.That(holder.CurrentDirection, Is.EqualTo(direction));
                Assert.That(entMan.GetComponent<PhysicsComponent>(mob).CanCollide, Is.False);
            });
        });

        var reachedPipe = false;
        for (var i = 0; i < 7 && !reachedPipe; i++)
        {
            await server.WaitRunTicks(1);
            await server.WaitAssertion(() =>
            {
                if (entMan.TryGetComponent<DisposalHolderComponent>(holderUid, out var holder))
                    reachedPipe = holder.CurrentTube == pipe;
            });
        }

        Assert.That(reachedPipe, Is.True);
    }

    private static MoveButtons ToMoveButton(Direction direction)
    {
        return direction switch
        {
            Direction.North => MoveButtons.Up,
            Direction.South => MoveButtons.Down,
            Direction.West => MoveButtons.Left,
            Direction.East => MoveButtons.Right,
            _ => MoveButtons.None,
        };
    }

    private static void EnsureAnchored(EntityUid uid, IEntityManager entMan, SharedTransformSystem transforms)
    {
        if (!entMan.GetComponent<TransformComponent>(uid).Anchored)
            transforms.AnchorEntity(uid);
    }
}
