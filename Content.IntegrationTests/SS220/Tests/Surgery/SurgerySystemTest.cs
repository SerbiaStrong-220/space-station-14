// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.SS220.Surgery.Actions;
using Content.Shared.Bed.Sleep;
using Content.Shared.Buckle;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DeviceLinking;
using Content.Shared.Power.EntitySystems;
using Content.Shared.SS220.Experience;
using Content.Shared.SS220.Experience.Components;
using Content.Shared.SS220.Experience.Systems.Experience;
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
    private static readonly ProtoId<SurgeryGraphPrototype> TorsoSurgeryGraphId = "TorsoSurgery";
    private static readonly ProtoId<SkillTreePrototype> AnatomySkillTree = "Anatomy";

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

- type: surgeryGraph
  id: TestDiskSurgeryGraph
  name: surgery-edge-tooltip-err
  description: surgery-edge-tooltip-err
  targetPart: Torso
  start: surgery-head-start
  end: seal-wound
  graph:
  - node: surgery-head-start
    edges:
    - to: seal-wound
      id: disk-edge
      baseEdge: incision
      edgeTooltip: to-torso-skin-incision
      delay: 1
      visibilityRequirements:
      - !type:OperatingComputerRequirement
        subject: target
        protocol: TestProtocol
        description: surgery-requirement-operating-computer-description
        failureMessage: surgery-requirement-operating-computer-failure
  - node: seal-wound

- type: surgeryProtocol
  id: TestProtocol

- type: entity
  parent: BaseSurgeryDisk
  id: TestSurgeryDisk
  components:
  - type: SurgeryDisk
    protocols:
    - TestProtocol
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
    public async Task SurgeryDiskUnlocksSurgeryTest()
    {
        await using var pair = await PoolManager.GetServerClient();
        var mapData = await pair.CreateTestMap();
        var server = pair.Server;
        var entMng = server.ResolveDependency<IEntityManager>();
        var surgerySystem = entMng.System<SharedSurgerySystem>();
        var buckleSystem = entMng.System<SharedBuckleSystem>();
        var deviceLink = entMng.System<SharedDeviceLinkSystem>();
        var powerReceiver = entMng.System<SharedPowerReceiverSystem>();
        var itemSlots = entMng.System<ItemSlotsSystem>();
        var protoManager = server.ResolveDependency<IPrototypeManager>();
        const string diskGraphId = "TestDiskSurgeryGraph";

        EntityUid patient = default!;
        EntityUid surgeon = default!;
        EntityUid table = default!;
        EntityUid computer = default!;
        EntityUid disk = default!;
        await server.WaitAssertion(() =>
        {
            patient = entMng.SpawnEntity("MobHuman", mapData.MapCoords);
            surgeon = entMng.SpawnEntity("MobHuman", mapData.MapCoords);
            table = entMng.SpawnEntity("OperatingTable", mapData.MapCoords);
            computer = entMng.SpawnEntity("computerBodyScanner", new MapCoordinates(1f, 0f, mapData.MapId));
            disk = entMng.SpawnEntity("TestSurgeryDisk", mapData.MapCoords);

            deviceLink.LinkDefaults(null, computer, table);
            powerReceiver.SetNeedsPower(computer, false);
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            Assert.That(buckleSystem.TryBuckle(patient, surgeon, table, popup: false), Is.True);
            Assert.That(surgerySystem.HasLinkedOperatingComputer(table), Is.True);

            var patientComp = entMng.GetComponent<SurgeryPatientComponent>(patient);
            var entity = new Entity<SurgeryPatientComponent>(patient, patientComp);
            Assert.That(protoManager.Index<SurgeryGraphPrototype>(diskGraphId).TryGetNode("surgery-head-start", out var startNode), Is.True);
            var diskEdge = startNode!.Edges[0];

            // the operation itself needs no disk, only its disk step does
            Assert.That(surgerySystem.TryStartSurgery(entity, diskGraphId, surgeon, surgeon), Is.True);
            Assert.That(surgerySystem.GetPerformSurgeryEdgeInfo(entity, diskEdge, null, surgeon).Visible, Is.False, "No disk in the computer");

            var computerComp = entMng.GetComponent<OperatingComputerComponent>(computer);
            Assert.That(itemSlots.TryInsert(computer, computerComp.DiskSlotId, disk, null), Is.True);
            Assert.That(surgerySystem.GetPerformSurgeryEdgeInfo(entity, diskEdge, null, surgeon).Visible, Is.True);

            Assert.That(itemSlots.TryEject(computer, computerComp.DiskSlotId, null, out _), Is.True);
            Assert.That(surgerySystem.GetPerformSurgeryEdgeInfo(entity, diskEdge, null, surgeon).Visible, Is.False, "The computer doesn't keep the protocol of a taken disk");
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
            entMng.GetComponent<SurgeryToolComponent>(scalpel).FailureModifier = -1f;
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

        await pair.RunSeconds(3f);

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
        var experienceSystem = entMng.System<ExperienceSystem>();

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
            Assert.That(surgerySystem.TryGetOperatingTable(patient, out _), Is.False);

            var delayOffTable = surgerySystem.GetStepDelay(patient, surgeon, scalpel, edge);
            var failUnanesthetizedOffTable = surgerySystem.GetStepFailureChance(patient, surgeon, scalpel, edge);

            Assert.That(buckleSystem.TryBuckle(patient, surgeon, table, popup: false), Is.True);
            Assert.That(surgerySystem.TryGetOperatingTable(patient, out _), Is.True);

            var delayOnTable = surgerySystem.GetStepDelay(patient, surgeon, scalpel, edge);
            var failOnTable = surgerySystem.GetStepFailureChance(patient, surgeon, scalpel, edge);

            Assert.That(delayOnTable, Is.LessThan(delayOffTable));
            Assert.That(failOnTable, Is.LessThan(failUnanesthetizedOffTable));

            entMng.EnsureComponent<SleepingComponent>(patient);
            Assert.That(surgerySystem.IsPatientAnesthetized(patient), Is.True);

            var failAnesthetizedOnTable = surgerySystem.GetStepFailureChance(patient, surgeon, scalpel, edge);
            Assert.That(failAnesthetizedOnTable, Is.LessThan(failOnTable));

            // Verify SurgeryDoAfterSkillSystem integration & DeceaseSkillBonus behavior
            var torsoGraph = protoManager.Index(TorsoSurgeryGraphId);
            Assert.That(torsoGraph.TryGetNode("torso-skin-clamp-bleeders", out var clampNode), Is.True);
            Assert.That(torsoGraph.TryGetNode("retract-torso-skin", out var retractNode), Is.True);
            var normalRetractEdge = clampNode!.Edges.First(e => e.Id == "retract-torso-skin");
            var decreasedBonusEdge = retractNode!.Edges.First(e => e.Id == "retract-torso-skin-heal-wounds");

            var initialNormalDelay = surgerySystem.GetStepDelay(patient, surgeon, scalpel, normalRetractEdge);
            var initialDecreasedDelay = surgerySystem.GetStepDelay(patient, surgeon, scalpel, decreasedBonusEdge);
            Assert.That(initialDecreasedDelay, Is.EqualTo(initialNormalDelay).Within(0.001f));

            experienceSystem.TrySetOverrideSkill(surgeon, AnatomySkillTree, new SkillTreeInfo { Level = 4, Sublevel = 0 });
            var proNormalDelay = surgerySystem.GetStepDelay(patient, surgeon, scalpel, normalRetractEdge);
            var proDecreasedDelay = surgerySystem.GetStepDelay(patient, surgeon, scalpel, decreasedBonusEdge);
            Assert.That(proNormalDelay, Is.LessThan(initialNormalDelay));
            Assert.That(proDecreasedDelay, Is.GreaterThan(proNormalDelay));
            Assert.That(proDecreasedDelay, Is.LessThan(initialDecreasedDelay));
        });

        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// The action checks the patient's node when the voice is chosen, a renamed node would silently block it.
    /// </summary>
    [Test]
    public async Task ChangeTtsActionMatchesItsStepTest()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var protoManager = server.ResolveDependency<IPrototypeManager>();
        var surgeryGraph = server.ResolveDependency<IEntityManager>().System<SurgeryGraphSystem>();

        await server.WaitAssertion(() =>
        {
            var found = 0;
            foreach (var graph in protoManager.EnumeratePrototypes<SurgeryGraphPrototype>())
            {
                foreach (var node in graph.Nodes)
                {
                    foreach (var edge in node.Edges)
                    {
                        foreach (var action in surgeryGraph.GetActions(edge))
                        {
                            if (action is not ChangeTtsAction tts)
                                continue;

                            found++;
                            Assert.Multiple(() =>
                            {
                                Assert.That(tts.Surgery.Id, Is.EqualTo(graph.ID), $"Edge {edge.Id} names another surgery");
                                Assert.That(tts.Node, Is.EqualTo(edge.Target), $"Edge {edge.Id} names another node than it leads to");
                            });
                        }
                    }
                }
            }

            Assert.That(found, Is.GreaterThan(0), "No graph changes the voice");
        });

        await pair.CleanReturnAsync();
    }
}
