// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Body;
using Content.Shared.SS220.Pathology;
using Content.Shared.Traits.Assorted;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.SS220.Tests.Pathology;

[TestFixture]
public sealed class OrganPathologyTest
{
    // test prototypes are not loaded by the linter, so not ProtoId
    private const string OrganDamageProto = "TestOrganDamage";
    private const string MissingOrganProto = "TestMissingLiver";
    private const string DecayProto = "TestDecayPathology";
    private static readonly ProtoId<OrganCategoryPrototype> LiverCategory = "Liver";

    [TestPrototypes]
    private const string OrganPathologyPrototypes = @"
- type: pathology
  id: TestOrganDamage
  name: cmd-testlog-desc
  organ: Liver
  failsOnRot: true
  definition:
  - description: cmd-testlog-desc
    maxStackCount: 3
    progressConditions:
    - !type:PathologyStackProgressCondition
      stackCount: 3
  - description: cmd-testlog-desc
    trait: PoorVision

- type: pathology
  id: TestMissingLiver
  name: cmd-testlog-desc
  missingOrgan: Liver
  definition:
  - description: cmd-testlog-desc

- type: pathology
  id: TestDecayPathology
  name: cmd-testlog-desc
  definition:
  - description: cmd-testlog-desc
    maxStackCount: 4
    stackDecayChance: 1
";

    [Test]
    public async Task StackProgressionAndRegressTest()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = true,
            DummyTicker = false
        });

        var server = pair.Server;
        var entMng = server.ResolveDependency<IEntityManager>();
        var pathologySystem = entMng.System<SharedPathologySystem>();

        EntityUid human = default!;

        await server.WaitAssertion(() =>
        {
            human = entMng.SpawnEntity("MobHuman", MapCoordinates.Nullspace);

            Assert.That(pathologySystem.TryAddPathology(human, OrganDamageProto), Is.True);
            Assert.That(pathologySystem.TryChangePathologyStack(human, OrganDamageProto, 2), Is.True);
            Assert.That(pathologySystem.TryAddPathology(human, DecayProto), Is.True);
            Assert.That(pathologySystem.TryChangePathologyStack(human, DecayProto, 3), Is.True);
        });

        await pair.RunSeconds(1.5f);

        await server.WaitAssertion(() =>
        {
            var holder = entMng.GetComponent<PathologyHolderComponent>(human);
            var data = holder.ActivePathologies[OrganDamageProto];

            Assert.Multiple(() =>
            {
                Assert.That(data.Level, Is.EqualTo(1), "Organ damage must progress to failure at max stacks");
                Assert.That(data.StackCount, Is.EqualTo(SharedPathologySystem.OneStack));
                Assert.That(entMng.HasComponent<PermanentBlindnessComponent>(human), Is.True);
                Assert.That(holder.ActivePathologies[DecayProto].StackCount, Is.LessThan(4), "Decaying pathology must lose stacks");
            });

            Assert.That(pathologySystem.TryRegressPathology(human, OrganDamageProto, 2), Is.True);

            Assert.Multiple(() =>
            {
                Assert.That(data.Level, Is.EqualTo(0));
                Assert.That(data.StackCount, Is.EqualTo(2));
                Assert.That(data.PathologyContexts, Has.Count.EqualTo(2));
                Assert.That(entMng.HasComponent<PermanentBlindnessComponent>(human), Is.False, "Regress must remove effects of the dropped level");
                Assert.That(pathologySystem.TryRegressPathology(human, OrganDamageProto), Is.False, "First level can't be regressed");
            });
        });

        await pair.RunSeconds(5f);

        await server.WaitAssertion(() =>
        {
            Assert.That(pathologySystem.HavePathology(human, DecayProto), Is.False, "Pathology must be removed after losing the last stack");
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task OrganExtractionMovesPathologyTest()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = true,
            DummyTicker = false
        });

        var mapData = await pair.CreateTestMap();
        var server = pair.Server;
        var entMng = server.ResolveDependency<IEntityManager>();
        var pathologySystem = entMng.System<SharedPathologySystem>();
        var bodySystem = entMng.System<BodySystem>();

        await server.WaitAssertion(() =>
        {
            var donor = entMng.SpawnEntity("MobHuman", mapData.MapCoords);
            var recipient = entMng.SpawnEntity("MobHuman", mapData.MapCoords);

            Assert.That(pathologySystem.TryAddPathology(donor, OrganDamageProto), Is.True);
            Assert.That(pathologySystem.TryAddPathology(donor, MissingOrganProto), Is.False, "Missing organ pathology can't be added while the organ is present");

            Assert.That(bodySystem.TryExtractOrgan(donor, LiverCategory, out var donorLiver), Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(pathologySystem.HavePathology(donor, OrganDamageProto), Is.False, "Organ pathology must leave the body with the organ");
                Assert.That(pathologySystem.HavePathology(donor, MissingOrganProto), Is.True);
                Assert.That(pathologySystem.TryAddPathology(donor, OrganDamageProto), Is.False, "Organ pathology can't be added without the organ");
                Assert.That(pathologySystem.TryRemovePathology(donor, MissingOrganProto), Is.False, "Absence of an organ can't be cured without inserting one");
                Assert.That(entMng.TryGetComponent<OrganPathologyStorageComponent>(donorLiver!.Value, out var storage), Is.True);
                Assert.That(storage!.Pathologies.ContainsKey(OrganDamageProto), Is.True);
            });

            Assert.That(bodySystem.TryExtractOrgan(recipient, LiverCategory, out var recipientLiver), Is.True);
            Assert.That(bodySystem.TryInsertOrgan(recipient, donorLiver!.Value.Owner), Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(pathologySystem.HavePathology(recipient, OrganDamageProto), Is.True, "Inserted organ must bring its pathology");
                Assert.That(pathologySystem.HavePathology(recipient, MissingOrganProto), Is.False);
                Assert.That(entMng.HasComponent<OrganPathologyStorageComponent>(donorLiver.Value), Is.False);
            });

            Assert.That(bodySystem.TryInsertOrgan(donor, recipientLiver!.Value.Owner), Is.True);
            Assert.That(pathologySystem.HavePathology(donor, MissingOrganProto), Is.False);
        });

        await pair.CleanReturnAsync();
    }
}
