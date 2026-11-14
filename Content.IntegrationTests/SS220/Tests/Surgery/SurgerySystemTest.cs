// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Bed.Sleep;
using Content.Shared.Body.Systems;
using Content.Shared.Buckle;
using Content.Shared.Chemistry.Components;
using Content.Shared.DeviceLinking;
using Content.Shared.Power.EntitySystems;
using Content.Shared.SS220.Surgery.Components;
using Content.Shared.SS220.Surgery.Systems;
using Content.Shared.SS220.Surgery.Graph;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.IntegrationTests.SS220.Tests.Surgery;

[TestFixture]
public sealed class SurgerySystemTest
{
    [TestPrototypes]
    private const string SurgeryDummyPrototypes = @"
- type: entity
  id: SurgeryDummyPatient
  components:
  - type: SurgeryPatient

- type: entity
  id: SurgeryDummyScalpel
  components:
  - type: SurgeryTool
    toolType: Scalpel

- type: surgeryGraph
  id: TestSurgeryGraph
  name: surgery-edge-tooltip-err
  description: surgery-edge-tooltip-err
  targetPart: Torso
  requirements: []
  start: surgery-head-start
  end: seal-wound
  graph:
  - node: surgery-head-start
    edges:
    - to: head-skin-incision
      id: head-skin-incision
      baseEdge: incision
      edgeTooltip: to-torso-skin-incision
      delay: 1
  - node: head-skin-incision
    edges:
    - to: seal-wound
      baseEdge: incision
      edgeTooltip: to-torso-skin-incision
      id: edge-2
      delay: 1
  - node: seal-wound
";

    [Test]
    public async Task StartAndEndSurgeryTest()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = true,
            DummyTicker = false
        });

        var server = pair.Server;
        var entMng = server.ResolveDependency<IEntityManager>();
        var surgerySystem = entMng.System<SharedSurgerySystem>();

        EntityUid dummyPatient = default!;
        SurgeryPatientComponent patientComp = default!;
        EntityUid dummyUser = default!;
        const string surgeryGraphId = "TestSurgeryGraph";

        await server.WaitAssertion(() =>
        {
            dummyPatient = entMng.SpawnEntity("SurgeryDummyPatient", MapCoordinates.Nullspace);
            dummyUser = entMng.SpawnEntity(null, MapCoordinates.Nullspace);
            Assert.That(entMng.TryGetComponent(dummyPatient, out patientComp!), Is.True);
        });

        await server.WaitAssertion(() =>
        {
            var entity = new Entity<SurgeryPatientComponent>(dummyPatient, patientComp);

            var started = surgerySystem.TryStartSurgery(entity, surgeryGraphId, dummyUser, dummyUser);

            Assert.Multiple(() =>
            {
                Assert.That(started, Is.True);
                Assert.That(patientComp.OngoingSurgeries.ContainsKey(surgeryGraphId), Is.True);
                Assert.That(patientComp.OngoingSurgeries[surgeryGraphId], Is.EqualTo("surgery-head-start"));
            });

            var canEnd = surgerySystem.OperationCanBeEnded(entity, surgeryGraphId);
            Assert.That(canEnd, Is.True);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task CannotStartOngoingSurgeryTest()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = true,
            DummyTicker = false
        });

        var server = pair.Server;
        var entMng = server.ResolveDependency<IEntityManager>();
        var surgerySystem = entMng.System<SharedSurgerySystem>();

        EntityUid dummyPatient = default!;
        EntityUid dummyUser = default!;
        const string surgeryGraphId = "TestSurgeryGraph";

        await server.WaitAssertion(() =>
        {
            dummyPatient = entMng.SpawnEntity("SurgeryDummyPatient", MapCoordinates.Nullspace);
            dummyUser = entMng.SpawnEntity("MobHuman", MapCoordinates.Nullspace);
        });

        await server.WaitAssertion(() =>
        {
            var entity = new Entity<SurgeryPatientComponent>(dummyPatient, entMng.GetComponent<SurgeryPatientComponent>(dummyPatient));

            var started1 = surgerySystem.TryStartSurgery(entity, surgeryGraphId, dummyUser, dummyUser);
            Assert.That(started1, Is.True);

            var canStartSecondTime = surgerySystem.CanStartSurgery(dummyUser, surgeryGraphId, dummyPatient, dummyUser, out var reason);
            var started2 = surgerySystem.TryStartSurgery(entity, surgeryGraphId, dummyUser, dummyUser);

            Assert.Multiple(() =>
            {
                Assert.That(canStartSecondTime, Is.False);
                Assert.That(started2, Is.False);
                Assert.That(reason, Is.Not.Null);
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task SurgeryProgressionDoAfterTest()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = true,
            DummyTicker = false
        });

        var server = pair.Server;
        var entMng = server.ResolveDependency<IEntityManager>();
        var protoManager = server.ResolveDependency<IPrototypeManager>();
        var surgerySystem = entMng.System<SharedSurgerySystem>();

        EntityUid dummyPatient = default!;
        SurgeryPatientComponent patientComp = default!;
        EntityUid dummyUser = default!;
        EntityUid scalpel = default!;
        const string surgeryGraphId = "TestSurgeryGraph";

        await server.WaitAssertion(() =>
        {
            dummyPatient = entMng.SpawnEntity("SurgeryDummyPatient", MapCoordinates.Nullspace);
            dummyUser = entMng.SpawnEntity("MobHuman", MapCoordinates.Nullspace);
            patientComp = entMng.GetComponent<SurgeryPatientComponent>(dummyPatient);
            scalpel = entMng.SpawnEntity("SurgeryDummyScalpel", MapCoordinates.Nullspace);
        });

        await server.WaitAssertion(() =>
        {
            var entityNull = new Entity<SurgeryPatientComponent>(dummyPatient, patientComp);
            surgerySystem.TryStartSurgery(entityNull, surgeryGraphId, dummyUser, dummyUser);
        });

        await server.WaitAssertion(() =>
        {
            var entity = new Entity<SurgeryPatientComponent>(dummyPatient, patientComp);

            Assert.That(protoManager.TryIndex<SurgeryGraphPrototype>(surgeryGraphId, out var graphProto), Is.True);
            Assert.That(graphProto.TryGetNode("surgery-head-start", out var startNode), Is.True);

            var edgeToPerform = startNode.Edges.FirstOrDefault(e => e.Id == "head-skin-incision");
            Assert.That(edgeToPerform, Is.Not.Null);

            var stepStarted = surgerySystem.TryPerformOperationStep(entity, surgeryGraphId, edgeToPerform!, scalpel, dummyUser);
            Assert.That(stepStarted, Is.True);

            Assert.That(patientComp.OngoingSurgeries[surgeryGraphId], Is.EqualTo(startNode.Name));
        });

        await pair.RunSeconds(1.5f);

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(patientComp.OngoingSurgeries.ContainsKey(surgeryGraphId), Is.True);
                Assert.That(patientComp.OngoingSurgeries[surgeryGraphId], Is.EqualTo("head-skin-incision"));
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task SurgeryRiskAndAnesthesiaTest()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = true,
            DummyTicker = false
        });

        var mapData = await pair.CreateTestMap();
        var server = pair.Server;
        var entMng = server.ResolveDependency<IEntityManager>();
        var protoManager = server.ResolveDependency<IPrototypeManager>();
        var surgerySystem = entMng.System<SharedSurgerySystem>();
        var buckleSystem = entMng.System<SharedBuckleSystem>();

        EntityUid patient = default!;
        EntityUid surgeon = default!;
        EntityUid table = default!;
        EntityUid scalpel = default!;
        SurgeryGraphEdge edge = default!;
        const string surgeryGraphId = "TestSurgeryGraph";

        await server.WaitAssertion(() =>
        {
            patient = entMng.SpawnEntity("MobHuman", mapData.MapCoords);
            surgeon = entMng.SpawnEntity("MobHuman", mapData.MapCoords);
            table = entMng.SpawnEntity("OperatingTable", mapData.MapCoords);
            scalpel = entMng.SpawnEntity("Scalpel", mapData.MapCoords);

            Assert.That(protoManager.TryIndex<SurgeryGraphPrototype>(surgeryGraphId, out var graphProto), Is.True);
            Assert.That(graphProto!.TryGetNode("surgery-head-start", out var startNode), Is.True);
            edge = startNode.Edges.First(e => e.Id == "head-skin-incision");

            Assert.That(surgerySystem.IsPatientAnesthetized(patient), Is.False);
            Assert.That(surgerySystem.IsOnOperatingTable(patient, out _), Is.False);

            var delayOffTable = surgerySystem.GetStepDelay(patient, surgeon, scalpel, edge);
            var failUnanesthetizedOffTable = surgerySystem.GetStepFailureChance(patient, surgeon, scalpel, edge);

            Assert.That(buckleSystem.TryBuckle(patient, surgeon, table, popup: false), Is.True);
            Assert.That(surgerySystem.IsOnOperatingTable(patient, out _), Is.True);

            var delayOnTable = surgerySystem.GetStepDelay(patient, surgeon, scalpel, edge);
            var failOnTable = surgerySystem.GetStepFailureChance(patient, surgeon, scalpel, edge);

            Assert.That(delayOnTable, Is.LessThan(delayOffTable));
            Assert.That(failOnTable, Is.LessThan(failUnanesthetizedOffTable));

            entMng.EnsureComponent<SleepingComponent>(patient);
            Assert.That(surgerySystem.IsPatientAnesthetized(patient), Is.True);

            var failAnesthetizedOnTable = surgerySystem.GetStepFailureChance(patient, surgeon, scalpel, edge);
            Assert.That(failAnesthetizedOnTable, Is.LessThan(failOnTable));
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task OperatingComputerDeviceLinkTest()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = true,
            DummyTicker = false
        });

        var mapData = await pair.CreateTestMap();
        var server = pair.Server;
        var entMng = server.ResolveDependency<IEntityManager>();
        var surgerySystem = entMng.System<SharedSurgerySystem>();
        var deviceLink = entMng.System<SharedDeviceLinkSystem>();
        var powerReceiver = entMng.System<SharedPowerReceiverSystem>();
        var transformSystem = entMng.System<SharedTransformSystem>();

        EntityUid computer = default!;
        EntityUid table1 = default!;
        EntityUid table2 = default!;

        await server.WaitAssertion(() =>
        {
            computer = entMng.SpawnEntity("computerBodyScanner", mapData.MapCoords);
            table1 = entMng.SpawnEntity("OperatingTable", mapData.MapCoords);
            table2 = entMng.SpawnEntity("OperatingTable", mapData.MapCoords);

            Assert.That(surgerySystem.HasLinkedOperatingComputer(table1), Is.False);

            deviceLink.LinkDefaults(null, computer, table1);

            var computerComp = entMng.GetComponent<OperatingComputerComponent>(computer);
            var table1Comp = entMng.GetComponent<OperatingTableComponent>(table1);
            Assert.Multiple(() =>
            {
                Assert.That(computerComp.LinkedTable, Is.EqualTo(table1));
                Assert.That(table1Comp.LinkedComputer, Is.EqualTo(computer));
                Assert.That(surgerySystem.HasLinkedOperatingComputer(table1), Is.False);
            });

            powerReceiver.SetNeedsPower(computer, false);
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            Assert.That(surgerySystem.HasLinkedOperatingComputer(table1), Is.True);

            transformSystem.SetMapCoordinates(table1, new MapCoordinates(20f, 0f, mapData.MapId));
            Assert.That(surgerySystem.HasLinkedOperatingComputer(table1), Is.False);

            transformSystem.SetMapCoordinates(table1, mapData.MapCoords);
            Assert.That(surgerySystem.HasLinkedOperatingComputer(table1), Is.True);

            deviceLink.LinkDefaults(null, computer, table2);

            var computerComp = entMng.GetComponent<OperatingComputerComponent>(computer);
            var table1Comp = entMng.GetComponent<OperatingTableComponent>(table1);
            var table2Comp = entMng.GetComponent<OperatingTableComponent>(table2);
            Assert.Multiple(() =>
            {
                Assert.That(table1Comp.LinkedComputer, Is.Null);
                Assert.That(surgerySystem.HasLinkedOperatingComputer(table1), Is.False);
                Assert.That(computerComp.LinkedTable, Is.EqualTo(table2));
                Assert.That(table2Comp.LinkedComputer, Is.EqualTo(computer));
                Assert.That(surgerySystem.HasLinkedOperatingComputer(table2), Is.True);
            });
        });

        await pair.CleanReturnAsync();
    }

    [TestCase("Aglomorphine")]
    [TestCase("Frontier")]
    [TestCase("Opium")]
    public async Task NarcoticsPainNumbnessAnesthesiaTest(string reagentId)
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = true,
            DummyTicker = false
        });

        var mapData = await pair.CreateTestMap();
        var server = pair.Server;
        var entMng = server.ResolveDependency<IEntityManager>();
        var surgerySystem = entMng.System<SharedSurgerySystem>();
        var bloodstream = entMng.System<SharedBloodstreamSystem>();

        EntityUid patient = default!;

        await server.WaitAssertion(() =>
        {
            patient = entMng.SpawnEntity("MobHuman", mapData.MapCoords);
            Assert.That(surgerySystem.IsPatientAnesthetized(patient), Is.False);
            Assert.That(bloodstream.TryAddToBloodstream(patient, new Solution(reagentId, 5f)), Is.True);
        });

        await pair.RunSeconds(2.5f);

        await server.WaitAssertion(() =>
        {
            Assert.That(surgerySystem.IsPatientAnesthetized(patient), Is.True);
        });

        await pair.CleanReturnAsync();
    }
}
