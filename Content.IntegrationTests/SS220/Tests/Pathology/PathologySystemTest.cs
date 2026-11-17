// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.DoAfter;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.SS220.ChangeSpeedDoAfters;
using Content.Shared.SS220.ChangeSpeedDoAfters.Events;
using Content.Shared.SS220.Pathology;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Wieldable;
using Content.Shared.Wieldable.Components;
using Robust.Server.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.SS220.Tests.Pathology;

[RegisterComponent]
public sealed partial class TestPathologyProgressionTraitComponent : Component { }

[Serializable]
public sealed partial class TestPathologyDoAfterEvent : SimpleDoAfterEvent { }

[TestFixture]
public sealed class PathologySystemTest
{
    private static readonly ProtoId<PathologyPrototype> LeftHandDislocationProto = "LeftHandDislocation";
    private static readonly ProtoId<PathologyPrototype> LeftHandHairlineFractureProto = "LeftHandHairlineFracture";
    private static readonly ProtoId<PathologyPrototype> LeftHandCompoundFractureProto = "LeftHandCompoundFracture";
    [TestPrototypes]
    private const string PathologyHolderPrototype = @"
- type: entity
  id: PathologyHolderDummy
  components:
  - type: PathologyHolder
";

    [TestPrototypes]
    private const string StackablePrototypes = @"
- type: pathology
  id: TestPathology
  name: cmd-testlog-desc
  definition:
  - description: cmd-testlog-desc
    maxStackCount: 4
    statusEffects: []
";

    private const int MaxStackCount = 4;

    [Test]
    public async Task AddChangeRemoveStackablePathologyTest()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = true,
            DummyTicker = false
        });

        var server = pair.Server;
        var entMng = server.ResolveDependency<IEntityManager>();
        var pathologySystem = entMng.System<SharedPathologySystem>();

        EntityUid dummy = default!;
        PathologyHolderComponent holder = default!;
        const string pathologyId = "TestPathology";

        await server.WaitAssertion(() =>
        {
            dummy = entMng.SpawnEntity("PathologyHolderDummy", MapCoordinates.Nullspace);
            Assert.That(entMng.TryGetComponent(dummy, out holder!), Is.True);
        });

        await server.WaitAssertion(() =>
        {
            var entity = new Entity<PathologyHolderComponent>(dummy, holder);

            // try add pathology
            var added = pathologySystem.TryAddPathology(entity, pathologyId);
            Assert.Multiple(() =>
            {
                Assert.That(added, Is.True);
                Assert.That(pathologySystem.HavePathology(entity, pathologyId), Is.True);
            });

            // check stack count
            var hasStack = pathologySystem.TryGetPathologyStack(entity, pathologyId, out var stackCount);
            Assert.Multiple(() =>
            {
                Assert.That(hasStack, Is.True);
                Assert.That(stackCount, Is.EqualTo(SharedPathologySystem.OneStack));
            });

            // try change stack
            var delta = 2;
            var stackChanged = pathologySystem.TryChangePathologyStack(entity, pathologyId, delta);
            Assert.That(stackChanged, Is.True);

            pathologySystem.TryGetPathologyStack(entity, pathologyId, out stackCount);
            Assert.That(stackCount, Is.EqualTo(SharedPathologySystem.OneStack + delta));

            // check overlimit of stacks
            pathologySystem.TryChangePathologyStack(entity, pathologyId, 10);
            pathologySystem.TryGetPathologyStack(entity, pathologyId, out stackCount);
            Assert.That(stackCount, Is.EqualTo(MaxStackCount));

            // try to remove with more than OneStack count
            var removed = pathologySystem.TryRemovePathology(entity, pathologyId, checkStacks: true);
            Assert.Multiple(() =>
            {
                Assert.That(removed, Is.False);
                Assert.That(pathologySystem.HavePathology(entity, pathologyId), Is.True);
            });

            // remove stack to OneStackCount
            var decreased = pathologySystem.TryChangePathologyStack(entity, pathologyId, -MaxStackCount);
            Assert.That(decreased, Is.True);

            // check if it removed
            Assert.That(pathologySystem.HavePathology(entity, pathologyId), Is.False);

            // repeat same path but end with other API call
            added = pathologySystem.TryAddPathology(entity, pathologyId);
            Assert.Multiple(() =>
            {
                Assert.That(added, Is.True);
                Assert.That(pathologySystem.HavePathology(entity, pathologyId), Is.True);
            });

            removed = pathologySystem.TryRemovePathology(entity, pathologyId, checkStacks: true);
            Assert.Multiple(() =>
            {
                Assert.That(removed, Is.True);
                Assert.That(pathologySystem.HavePathology(entity, pathologyId), Is.False);
            });
        });

        await pair.CleanReturnAsync();
    }

    [TestPrototypes]
    private const string ProgressionPrototype = @"
- type: trait
  id: TestPathologyProgression
  name: cmd-testlog-desc
  components:
  - type: TestPathologyProgressionTrait

- type: pathology
  id: TestPathologyProgression
  name: cmd-testlog-desc
  definition:
  - description: cmd-testlog-desc
    progressConditions:
    - !type:PathologyTimeProgressCondition
      delay: 2s
  - description: cmd-testlog-desc
    trait: TestPathologyProgression
";

    [Test]
    public async Task PathologyTimeProgressionAndTraitTest()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = true,
            DummyTicker = false
        });

        var server = pair.Server;
        var entMng = server.ResolveDependency<IEntityManager>();
        var pathologySystem = entMng.System<SharedPathologySystem>();

        EntityUid dummy = default!;
        PathologyHolderComponent holder = default!;
        const string pathologyId = "TestPathologyProgression";

        await server.WaitAssertion(() =>
        {
            dummy = entMng.SpawnEntity("PathologyHolderDummy", MapCoordinates.Nullspace);
            Assert.That(entMng.TryGetComponent(dummy, out holder!), Is.True);
        });

        // add pathology
        await server.WaitAssertion(() =>
        {
            var entity = new Entity<PathologyHolderComponent>(dummy, holder);
            var added = pathologySystem.TryAddPathology(entity, pathologyId);

            Assert.Multiple(() =>
            {
                Assert.That(added, Is.True);
                Assert.That(holder.ActivePathologies[pathologyId].Level, Is.EqualTo(0));
                Assert.That(entMng.HasComponent<TestPathologyProgressionTraitComponent>(dummy), Is.False);
            });
        });

        // wait some time
        await pair.RunSeconds(1);

        // assert no progression
        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(holder.ActivePathologies[pathologyId].Level, Is.EqualTo(0));
                Assert.That(entMng.HasComponent<TestPathologyProgressionTraitComponent>(dummy), Is.False);
            });
        });

        // wait for progression
        await pair.RunSeconds(4);

        // assert progression
        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(holder.ActivePathologies[pathologyId].Level, Is.EqualTo(1));
                Assert.That(entMng.HasComponent<TestPathologyProgressionTraitComponent>(dummy), Is.True);
            });
        });

        // remove pathology
        await server.WaitAssertion(() =>
        {
            var entity = new Entity<PathologyHolderComponent>(dummy, holder);
            pathologySystem.TryRemovePathology(entity, pathologyId, checkStacks: false);

            Assert.Multiple(() =>
            {
                Assert.That(pathologySystem.HavePathology(entity, pathologyId), Is.False);
                Assert.That(entMng.HasComponent<TestPathologyProgressionTraitComponent>(dummy), Is.False);
            });
        });

        await pair.CleanReturnAsync();
    }

    [TestPrototypes]
    private const string HandTraumaTestPrototypes = @"
- type: entity
  id: TestTraumaPistol
  components:
  - type: Item
  - type: Gun
    minAngle: 1
    maxAngle: 2
    fireRate: 2

- type: entity
  id: TestTraumaRifle
  components:
  - type: Item
    size: Large
  - type: Wieldable
  - type: Gun
    minAngle: 10
    maxAngle: 20
    fireRate: 2
  - type: GunWieldBonus
    minAngle: -9
    maxAngle: -18

- type: entity
  id: TestTraumaPlainRifle
  components:
  - type: Item
    size: Large
  - type: Wieldable
  - type: Gun
    minAngle: 4
    maxAngle: 8
    fireRate: 2
";

    [Test]
    public async Task HandTraumaEffectsTest()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = true,
            DummyTicker = false
        });

        var server = pair.Server;
        var mapData = await pair.CreateTestMap();
        var entMng = server.ResolveDependency<IEntityManager>();
        var protoMan = server.ResolveDependency<IPrototypeManager>();
        var pathologySystem = entMng.System<SharedPathologySystem>();
        var handsSystem = entMng.System<SharedHandsSystem>();
        var wieldSystem = entMng.System<SharedWieldableSystem>();

        EntityUid human = default!;
        EntityUid pistol = default!;
        EntityUid rifle = default!;
        EntityUid plainRifle = default!;
        PathologyHolderComponent holder = default!;

        await server.WaitAssertion(() =>
        {
            var dislocation = protoMan.Index(LeftHandDislocationProto).Definition[0];
            var hairline = protoMan.Index(LeftHandHairlineFractureProto).Definition[0];
            var compound = protoMan.Index(LeftHandCompoundFractureProto).Definition[0];

            Assert.Multiple(() =>
            {
                Assert.That(dislocation.Trait, Is.Null);
                Assert.That(dislocation.DoAfterDelayModifier, Is.EqualTo(1.2f).Within(0.001f));
                Assert.That(dislocation.GunSpreadPenalty.Degrees, Is.EqualTo(5d).Within(0.001d));

                Assert.That(hairline.Trait, Is.Null);
                Assert.That(hairline.DoAfterDelayModifier, Is.EqualTo(1.35f).Within(0.001f));
                Assert.That(hairline.GunSpreadPenalty.Degrees, Is.EqualTo(5d).Within(0.001d));

                Assert.That(compound.Trait, Is.Null);
                Assert.That(compound.DoAfterDelayModifier, Is.EqualTo(1.6f).Within(0.001f));
                Assert.That(compound.GunSpreadPenalty.Degrees, Is.EqualTo(5d).Within(0.001d));
            });

            human = entMng.SpawnEntity("MobHuman", mapData.MapCoords);
            pistol = entMng.SpawnEntity("TestTraumaPistol", mapData.MapCoords);
            rifle = entMng.SpawnEntity("TestTraumaRifle", mapData.MapCoords);
            plainRifle = entMng.SpawnEntity("TestTraumaPlainRifle", mapData.MapCoords);
            Assert.That(entMng.TryGetComponent(human, out holder!), Is.True);
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            var entity = new Entity<PathologyHolderComponent>(human, holder);
            Assert.That(pathologySystem.TryAddPathology(entity, "LeftHandDislocation"), Is.True);
            Assert.That(entMng.HasComponent<ChangeSpeedDoAftersComponent>(human), Is.False);

            var delayArgs = new DoAfterArgs(entMng, human, TimeSpan.FromSeconds(2), new TestPathologyDoAfterEvent(), human);
            entMng.EventBus.RaiseLocalEvent(human, new BeforeDoAfterStartEvent(delayArgs, 0), true);
            Assert.That(delayArgs.DelayModifier, Is.EqualTo(1.2f).Within(0.001f));

            // Stack a second trauma on the other hand and verify separate removal
            Assert.That(pathologySystem.TryAddPathology(entity, "RightHandCompoundFracture"), Is.True);
            var stackedArgs = new DoAfterArgs(entMng, human, TimeSpan.FromSeconds(2), new TestPathologyDoAfterEvent(), human);
            entMng.EventBus.RaiseLocalEvent(human, new BeforeDoAfterStartEvent(stackedArgs, 1), true);
            Assert.That(stackedArgs.DelayModifier, Is.EqualTo(1.2f * 1.6f).Within(0.001f));

            Assert.That(pathologySystem.TryRemovePathology(entity, "RightHandCompoundFracture", checkStacks: false), Is.True);
            var afterRemoveArgs = new DoAfterArgs(entMng, human, TimeSpan.FromSeconds(2), new TestPathologyDoAfterEvent(), human);
            entMng.EventBus.RaiseLocalEvent(human, new BeforeDoAfterStartEvent(afterRemoveArgs, 2), true);
            Assert.That(afterRemoveArgs.DelayModifier, Is.EqualTo(1.2f).Within(0.001f));

            // Test gun spread: +5 deg when held in injured left hand, +0 deg in healthy right hand
            var gunComp = entMng.GetComponent<GunComponent>(pistol);
            Assert.That(handsSystem.TryPickup(human, pistol, "left", checkActionBlocker: false, animate: false), Is.True);
            Assert.That(gunComp.MinAngleModified.Degrees, Is.EqualTo(6d).Within(0.01d));
            Assert.That(gunComp.MaxAngleModified.Degrees, Is.EqualTo(7d).Within(0.01d));

            Assert.That(handsSystem.TryDrop(human, pistol, checkActionBlocker: false), Is.True);
            Assert.That(handsSystem.TryPickup(human, pistol, "right", checkActionBlocker: false, animate: false), Is.True);
            Assert.That(gunComp.MinAngleModified.Degrees, Is.EqualTo(1d).Within(0.01d));
            Assert.That(gunComp.MaxAngleModified.Degrees, Is.EqualTo(2d).Within(0.01d));

            // Plain wieldable gun (without GunWieldBonusComponent) also refreshes spread when wielded/unwielded
            Assert.That(handsSystem.TryDrop(human, pistol, checkActionBlocker: false), Is.True);
            Assert.That(handsSystem.TryPickup(human, plainRifle, "right", checkActionBlocker: false, animate: false), Is.True);
            var plainRifleGun = entMng.GetComponent<GunComponent>(plainRifle);
            var plainRifleWield = entMng.GetComponent<WieldableComponent>(plainRifle);
            Assert.That(plainRifleGun.MinAngleModified.Degrees, Is.EqualTo(4d).Within(0.01d));

            Assert.That(wieldSystem.TryWield(plainRifle, plainRifleWield, human), Is.True);
            Assert.That(plainRifleGun.MinAngleModified.Degrees, Is.EqualTo(9d).Within(0.01d));
            Assert.That(plainRifleGun.MaxAngleModified.Degrees, Is.EqualTo(13d).Within(0.01d));

            Assert.That(wieldSystem.TryUnwield(plainRifle, plainRifleWield, human), Is.True);
            Assert.That(plainRifleGun.MinAngleModified.Degrees, Is.EqualTo(4d).Within(0.01d));
            Assert.That(plainRifleGun.MaxAngleModified.Degrees, Is.EqualTo(8d).Within(0.01d));

            // Two-handed rifle in healthy right hand gets +5 deg once wielded, then reverts when trauma is cured
            Assert.That(handsSystem.TryDrop(human, plainRifle, checkActionBlocker: false), Is.True);
            Assert.That(handsSystem.TryPickup(human, rifle, "right", checkActionBlocker: false, animate: false), Is.True);
            var rifleGun = entMng.GetComponent<GunComponent>(rifle);
            var rifleWield = entMng.GetComponent<WieldableComponent>(rifle);
            Assert.That(rifleGun.MinAngleModified.Degrees, Is.EqualTo(10d).Within(0.01d));

            Assert.That(wieldSystem.TryWield(rifle, rifleWield, human), Is.True);
            Assert.That(rifleWield.Wielded, Is.True);
            Assert.That(rifleGun.MinAngleModified.Degrees, Is.EqualTo(6d).Within(0.01d));
            Assert.That(rifleGun.MaxAngleModified.Degrees, Is.EqualTo(7d).Within(0.01d));

            Assert.That(pathologySystem.TryRemovePathology(entity, "LeftHandDislocation", checkStacks: false), Is.True);
            Assert.That(rifleGun.MinAngleModified.Degrees, Is.EqualTo(1d).Within(0.01d));
            Assert.That(rifleGun.MaxAngleModified.Degrees, Is.EqualTo(2d).Within(0.01d));
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task PathologyAdminCommandsTest()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = true,
            DummyTicker = false
        });

        var server = pair.Server;
        var mapData = await pair.CreateTestMap();
        var entMng = server.ResolveDependency<IEntityManager>();
        var console = server.ResolveDependency<IServerConsoleHost>();
        var pathologySystem = entMng.System<SharedPathologySystem>();

        EntityUid human = default!;
        PathologyHolderComponent holder = default!;

        await server.WaitAssertion(() =>
        {
            human = entMng.SpawnEntity("MobHuman", mapData.MapCoords);
            Assert.That(entMng.TryGetComponent(human, out holder!), Is.True);
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            var netHuman = entMng.GetNetEntity(human);
            var entity = new Entity<PathologyHolderComponent>(human, holder);

            console.ExecuteCommand($"addpathology {netHuman} {LeftHandDislocationProto}");
            Assert.That(pathologySystem.HavePathology(entity, LeftHandDislocationProto), Is.True);

            console.ExecuteCommand($"removepathology {netHuman} {LeftHandDislocationProto}");
            Assert.That(pathologySystem.HavePathology(entity, LeftHandDislocationProto), Is.False);
        });

        await pair.CleanReturnAsync();
    }
}
