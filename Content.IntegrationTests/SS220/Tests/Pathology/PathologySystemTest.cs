// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Pathology;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.IntegrationTests.SS220.Tests.Pathology;

[RegisterComponent]
public sealed partial class TestPathologyProgressionTraitComponent : Component { }

[TestFixture]
public sealed class PathologySystemTest
{
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
- type: pathology
  id: TestPathologyProgression
  name: cmd-testlog-desc
  definition:
  - description: cmd-testlog-desc
    progressConditions:
    - !type:PathologyTimeProgressCondition
      delay: 2s
  - description: cmd-testlog-desc
    components:
    - type: TestPathologyProgressionTrait
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
}
