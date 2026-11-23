// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Atmos.EntitySystems;
using Content.Server.SS220.Surgery.Actions;
using Content.Shared.Atmos;
using Content.Shared.Bed.Sleep;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Buckle;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.DeviceLinking;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Power.EntitySystems;
using Content.Shared.SS220.Bioware;
using Content.Shared.SS220.Body;
using Content.Shared.SS220.Experience;
using Content.Shared.SS220.Experience.Components;
using Content.Shared.SS220.Experience.Systems.Experience;
using Content.Shared.SS220.Pathology;
using Content.Shared.SS220.Surgery;
using Content.Shared.SS220.Surgery.Components;
using Content.Shared.SS220.Surgery.Systems;
using Content.Shared.SS220.Surgery.Graph;
using Content.Shared.StatusEffectNew;
using Content.Shared.Traits.Assorted;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using System.Linq;

namespace Content.IntegrationTests.SS220.Tests.Surgery;

[TestFixture]
public sealed class SurgerySystemTest
{
    private static readonly ProtoId<PathologyPrototype> LeftArmDislocationProto = "LeftArmDislocation";
    private static readonly ProtoId<PathologyPrototype> HeadCranialFissureProto = "HeadCranialFissure";
    private static readonly ProtoId<PathologyPrototype> EyeCornealTraumaProto = "EyeCornealTrauma";
    private static readonly ProtoId<PathologyPrototype> MouthJawDislocationProto = "MouthJawDislocation";
    private static readonly ProtoId<SurgeryGraphPrototype> LeftArmGraphId = "LeftArmSurgery";
    private static readonly ProtoId<SurgeryGraphPrototype> LeftHandGraphId = "LeftHandSurgery";
    private static readonly ProtoId<SurgeryGraphPrototype> TorsoSurgeryGraphId = "TorsoSurgery";
    private static readonly ProtoId<SurgeryGraphPrototype> HeadSurgeryGraphId = "HeadSurgery";
    private static readonly ProtoId<SurgeryGraphPrototype> EyesSurgeryGraphId = "EyesSurgery";
    private static readonly ProtoId<OrganCategoryPrototype> HeadCategory = "Head";
    private static readonly ProtoId<OrganCategoryPrototype> BrainCategory = "Brain";
    private static readonly ProtoId<PathologyPrototype> MissingEyesProto = "MissingEyes";
    private static readonly ProtoId<OrganCategoryPrototype> EyesCategory = "Eyes";
    private static readonly ProtoId<KnowledgePrototype> SyndicateAgentKnowledge = "SyndicateAgentKnowledge";
    private static readonly ProtoId<SkillTreePrototype> AnatomySkillTree = "Anatomy";
    private static readonly EntProtoId PainNumbnessEffectProto = "StatusEffectPainNumbness";
    private static readonly ProtoId<OrganCategoryPrototype> ArmLeftCategory = "ArmLeft";
    private static readonly ProtoId<OrganCategoryPrototype> HandLeftCategory = "HandLeft";
    private static readonly ProtoId<PathologyPrototype> MissingArmLeftProto = "MissingArmLeft";
    private static readonly ProtoId<PathologyPrototype> MissingHandLeftProto = "MissingHandLeft";
    private static readonly ProtoId<DamageTypePrototype> PoisonDamage = "Poison";
    private static readonly ProtoId<DamageTypePrototype> SlashDamage = "Slash";

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
    public async Task LobotomySurgeryTest()
    {
        await using var pair = await PoolManager.GetServerClient();
        var mapData = await pair.CreateTestMap();
        var server = pair.Server;
        var entMng = server.ResolveDependency<IEntityManager>();
        var protoManager = server.ResolveDependency<IPrototypeManager>();
        var surgerySystem = entMng.System<SharedSurgerySystem>();
        var buckleSystem = entMng.System<SharedBuckleSystem>();
        var deviceLink = entMng.System<SharedDeviceLinkSystem>();
        var powerReceiver = entMng.System<SharedPowerReceiverSystem>();
        var itemSlots = entMng.System<ItemSlotsSystem>();
        var bodySystem = entMng.System<BodySystem>();
        var graphSystem = entMng.System<SurgeryGraphSystem>();

        EntityUid patient = default!;
        EntityUid surgeon = default!;
        EntityUid table = default!;
        EntityUid computer = default!;
        EntityUid disk = default!;
        EntityUid scalpel = default!;
        await server.WaitAssertion(() =>
        {
            patient = entMng.SpawnEntity("MobHuman", mapData.MapCoords);
            surgeon = entMng.SpawnEntity("MobHuman", mapData.MapCoords);
            table = entMng.SpawnEntity("OperatingTable", mapData.MapCoords);
            computer = entMng.SpawnEntity("computerBodyScanner", new MapCoordinates(1f, 0f, mapData.MapId));
            disk = entMng.SpawnEntity("SurgeryDiskAdvanced", mapData.MapCoords);
            scalpel = entMng.SpawnEntity("Scalpel", mapData.MapCoords);

            deviceLink.LinkDefaults(null, computer, table);
            powerReceiver.SetNeedsPower(computer, false);
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            Assert.That(buckleSystem.TryBuckle(patient, surgeon, table, popup: false), Is.True);
            var entity = new Entity<SurgeryPatientComponent>(patient, entMng.GetComponent<SurgeryPatientComponent>(patient));
            Assert.That(protoManager.Index<SurgeryGraphPrototype>(HeadSurgeryGraphId).TryGetNode("brain-exposed", out var brainNode), Is.True);
            var lobotomy = brainNode!.Edges.Single(e => e.Id == "brain-exposed-lobotomy");

            Assert.Multiple(() =>
            {
                Assert.That(graphSystem.FailureChance(lobotomy), Is.EqualTo(0.5f), "A failed roll is the failed lobotomy");
                Assert.That(graphSystem.FailureDamage(lobotomy)!.DamageDict["Cerebral"].Float(), Is.EqualTo(10f));
                Assert.That(surgerySystem.GetPerformSurgeryEdgeInfo(entity, lobotomy, scalpel, surgeon).Visible, Is.False, "Hidden without the disk");
            });

            var computerComp = entMng.GetComponent<OperatingComputerComponent>(computer);
            Assert.That(itemSlots.TryInsert(computer, computerComp.DiskSlotId, disk, null), Is.True);
            var info = surgerySystem.GetPerformSurgeryEdgeInfo(entity, lobotomy, scalpel, surgeon);
            Assert.Multiple(() =>
            {
                Assert.That(info.Visible, Is.True, "Shown with the disk");
                Assert.That(info.FailureReason, Is.Not.Null, "Inactive without a congenital trait");
            });

            Assert.That(bodySystem.TryGetOrganByCategory(patient, BrainCategory, out var brain), Is.True);
            var congenital = entMng.EnsureComponent<CongenitalTraitsComponent>(brain!.Value);
            congenital.Traits.Add("Blindness");
            entMng.EnsureComponent<PermanentBlindnessComponent>(patient);
            Assert.That(surgerySystem.GetPerformSurgeryEdgeInfo(entity, lobotomy, scalpel, surgeon).FailureReason, Is.Null);

            // the outcomes of a successful roll
            var pathologies = entMng.GetComponent<PathologyHolderComponent>(patient).ActivePathologies;
            var pathologyCount = pathologies.Count;
            new LobotomyAction { TraumaChance = 1f }.PerformAction(patient, surgeon, scalpel, entMng);
            Assert.Multiple(() =>
            {
                Assert.That(pathologies, Has.Count.EqualTo(pathologyCount + 1), "Bad luck leaves a new trauma");
                Assert.That(congenital.Traits, Is.Not.Empty);
            });

            new LobotomyAction { TraumaChance = 0f }.PerformAction(patient, surgeon, scalpel, entMng);
            Assert.Multiple(() =>
            {
                Assert.That(congenital.Traits, Is.Empty);
                Assert.That(entMng.HasComponent<PermanentBlindnessComponent>(patient), Is.False, "The trait is cured");
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task BiowareSurgeryTest()
    {
        await using var pair = await PoolManager.GetServerClient();
        var mapData = await pair.CreateTestMap();
        var server = pair.Server;
        var entMng = server.ResolveDependency<IEntityManager>();
        var protoManager = server.ResolveDependency<IPrototypeManager>();
        var surgerySystem = entMng.System<SharedSurgerySystem>();
        var buckleSystem = entMng.System<SharedBuckleSystem>();
        var deviceLink = entMng.System<SharedDeviceLinkSystem>();
        var powerReceiver = entMng.System<SharedPowerReceiverSystem>();
        var itemSlots = entMng.System<ItemSlotsSystem>();
        var graphSystem = entMng.System<SurgeryGraphSystem>();

        EntityUid patient = default!;
        EntityUid surgeon = default!;
        EntityUid table = default!;
        EntityUid computer = default!;
        EntityUid disk = default!;
        EntityUid hemostat = default!;
        EntityUid scalpel = default!;
        await server.WaitAssertion(() =>
        {
            patient = entMng.SpawnEntity("MobHuman", mapData.MapCoords);
            surgeon = entMng.SpawnEntity("MobHuman", mapData.MapCoords);
            table = entMng.SpawnEntity("OperatingTable", mapData.MapCoords);
            computer = entMng.SpawnEntity("computerBodyScanner", new MapCoordinates(1f, 0f, mapData.MapId));
            disk = entMng.SpawnEntity("SurgeryDiskBioware", mapData.MapCoords);
            hemostat = entMng.SpawnEntity("Hemostat", mapData.MapCoords);
            scalpel = entMng.SpawnEntity("Scalpel", mapData.MapCoords);

            deviceLink.LinkDefaults(null, computer, table);
            powerReceiver.SetNeedsPower(computer, false);
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            Assert.That(buckleSystem.TryBuckle(patient, surgeon, table, popup: false), Is.True);
            var entity = new Entity<SurgeryPatientComponent>(patient, entMng.GetComponent<SurgeryPatientComponent>(patient));
            Assert.That(protoManager.Index<SurgeryGraphPrototype>(TorsoSurgeryGraphId).TryGetNode("open-heart-incision", out var node), Is.True);
            var veins = node!.Edges.Single(e => e.Id == "open-heart-vein-threading");
            var capillaries = node.Edges.Single(e => e.Id == "open-heart-capillary-weaving");
            var splicing = node.Edges.Single(e => e.Id == "open-heart-nerve-splicing");

            Assert.That(surgerySystem.GetPerformSurgeryEdgeInfo(entity, veins, hemostat, surgeon).Visible, Is.False, "Hidden without the disk");

            var computerComp = entMng.GetComponent<OperatingComputerComponent>(computer);
            Assert.That(itemSlots.TryInsert(computer, computerComp.DiskSlotId, disk, null), Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(surgerySystem.GetPerformSurgeryEdgeInfo(entity, veins, hemostat, surgeon).FailureReason, Is.Null);
                Assert.That(surgerySystem.GetPerformSurgeryEdgeInfo(entity, capillaries, hemostat, surgeon).FailureReason, Is.Null);
            });

            foreach (var action in graphSystem.GetActions(veins))
            {
                action.PerformAction(patient, surgeon, hemostat, entMng);
            }

            Assert.Multiple(() =>
            {
                Assert.That(entMng.HasComponent<VeinThreadingComponent>(patient), Is.True);
                Assert.That(surgerySystem.GetPerformSurgeryEdgeInfo(entity, capillaries, hemostat, surgeon).FailureReason, Is.Not.Null,
                    "One modification per group");
                Assert.That(surgerySystem.GetPerformSurgeryEdgeInfo(entity, veins, hemostat, surgeon).FailureReason, Is.Not.Null);
                Assert.That(surgerySystem.GetPerformSurgeryEdgeInfo(entity, splicing, scalpel, surgeon).FailureReason, Is.Null,
                    "Another group is free");
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task PacificationSurgeryTest()
    {
        await using var pair = await PoolManager.GetServerClient();
        var mapData = await pair.CreateTestMap();
        var server = pair.Server;
        var entMng = server.ResolveDependency<IEntityManager>();
        var protoManager = server.ResolveDependency<IPrototypeManager>();
        var surgerySystem = entMng.System<SharedSurgerySystem>();
        var buckleSystem = entMng.System<SharedBuckleSystem>();
        var deviceLink = entMng.System<SharedDeviceLinkSystem>();
        var powerReceiver = entMng.System<SharedPowerReceiverSystem>();
        var itemSlots = entMng.System<ItemSlotsSystem>();
        var bodySystem = entMng.System<BodySystem>();
        var graphSystem = entMng.System<SurgeryGraphSystem>();

        EntityUid patient = default!;
        EntityUid surgeon = default!;
        EntityUid table = default!;
        EntityUid computer = default!;
        EntityUid disk = default!;
        EntityUid hemostat = default!;
        await server.WaitAssertion(() =>
        {
            patient = entMng.SpawnEntity("MobHuman", mapData.MapCoords);
            surgeon = entMng.SpawnEntity("MobHuman", mapData.MapCoords);
            table = entMng.SpawnEntity("OperatingTable", mapData.MapCoords);
            computer = entMng.SpawnEntity("computerBodyScanner", new MapCoordinates(1f, 0f, mapData.MapId));
            disk = entMng.SpawnEntity("SurgeryDiskAdvanced", mapData.MapCoords);
            hemostat = entMng.SpawnEntity("Hemostat", mapData.MapCoords);

            deviceLink.LinkDefaults(null, computer, table);
            powerReceiver.SetNeedsPower(computer, false);
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            Assert.That(buckleSystem.TryBuckle(patient, surgeon, table, popup: false), Is.True);
            var computerComp = entMng.GetComponent<OperatingComputerComponent>(computer);
            Assert.That(itemSlots.TryInsert(computer, computerComp.DiskSlotId, disk, null), Is.True);

            var entity = new Entity<SurgeryPatientComponent>(patient, entMng.GetComponent<SurgeryPatientComponent>(patient));
            Assert.That(protoManager.Index<SurgeryGraphPrototype>(HeadSurgeryGraphId).TryGetNode("brain-exposed", out var brainNode), Is.True);
            var pacify = brainNode!.Edges.Single(e => e.Id == "brain-exposed-pacify");
            var unpacify = brainNode.Edges.Single(e => e.Id == "brain-exposed-unpacify");
            var removePathology = brainNode.Edges.Single(e => e.Id == "brain-exposed-remove-brain-pathology");

            Assert.Multiple(() =>
            {
                Assert.That(surgerySystem.GetPerformSurgeryEdgeInfo(entity, pacify, hemostat, surgeon).FailureReason, Is.Null);
                Assert.That(surgerySystem.GetPerformSurgeryEdgeInfo(entity, unpacify, hemostat, surgeon) is { Visible: true, FailureReason: not null }, Is.True,
                    "Nothing to undo yet");
            });

            foreach (var action in graphSystem.GetActions(pacify))
            {
                action.PerformAction(patient, surgeon, hemostat, entMng);
            }

            Assert.Multiple(() =>
            {
                Assert.That(entMng.HasComponent<PacifiedComponent>(patient), Is.True);
                Assert.That(surgerySystem.GetPerformSurgeryEdgeInfo(entity, pacify, hemostat, surgeon).FailureReason, Is.Not.Null, "Already pacified");
                Assert.That(surgerySystem.GetPerformSurgeryEdgeInfo(entity, unpacify, hemostat, surgeon).FailureReason, Is.Null);
                Assert.That(surgerySystem.CanMeetRequirements(entity, removePathology, hemostat, surgeon), Is.False,
                    "The ordinary brain surgery doesn't undo it");
            });

            // it lives in the brain
            Assert.That(bodySystem.TryExtractOrgan(patient, BrainCategory, out var brain), Is.True);
            Assert.That(entMng.HasComponent<PacifiedComponent>(patient), Is.False);
            Assert.That(bodySystem.TryInsertOrgan(patient, brain!.Value), Is.True);
            Assert.That(entMng.HasComponent<PacifiedComponent>(patient), Is.True);

            foreach (var action in graphSystem.GetActions(unpacify))
            {
                action.PerformAction(patient, surgeon, hemostat, entMng);
            }

            Assert.That(entMng.HasComponent<PacifiedComponent>(patient), Is.False);
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

    [Test]
    public async Task Phase2TraumaAndLimbSurgeryTest()
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
        var pathologySystem = entMng.System<SharedPathologySystem>();
        var buckleSystem = entMng.System<SharedBuckleSystem>();
        var deviceLink = entMng.System<SharedDeviceLinkSystem>();
        var powerReceiver = entMng.System<SharedPowerReceiverSystem>();
        var handsSystem = entMng.System<SharedHandsSystem>();
        var damageableSystem = entMng.System<DamageableSystem>();
        var experienceSystem = entMng.System<ExperienceSystem>();
        var statusEffects = entMng.System<StatusEffectsSystem>();
        var atmosSystem = entMng.System<AtmosphereSystem>();
        var mobStateSystem = entMng.System<MobStateSystem>();

        EntityUid patient = default!;
        EntityUid surgeon = default!;
        EntityUid table = default!;
        EntityUid computer = default!;
        EntityUid drapes = default!;
        EntityUid scalpel = default!;
        EntityUid hemostat = default!;
        EntityUid retractor = default!;
        EntityUid bonesetter = default!;
        EntityUid boneGel = default!;
        EntityUid cautery = default!;
        SurgeryPatientComponent patientComp = default!;

        await server.WaitPost(() =>
        {
            var moles = new float[Atmospherics.AdjustedNumberOfGases];
            moles[(int)Gas.Oxygen] = 21.824779f;
            moles[(int)Gas.Nitrogen] = 82.10312f;
            atmosSystem.SetMapAtmosphere(mapData.MapUid, false, new GasMixture(moles, Atmospherics.T20C));
        });

        await server.WaitAssertion(() =>
        {
            patient = entMng.SpawnEntity("MobHuman", mapData.MapCoords);
            surgeon = entMng.SpawnEntity("MobHuman", mapData.MapCoords);
            table = entMng.SpawnEntity("OperatingTable", mapData.MapCoords);
            computer = entMng.SpawnEntity("computerBodyScanner", new MapCoordinates(1f, 0f, mapData.MapId));
            drapes = entMng.SpawnEntity("SurgeryDrapes", mapData.MapCoords);
            scalpel = entMng.SpawnEntity("Scalpel", mapData.MapCoords);
            hemostat = entMng.SpawnEntity("Hemostat", mapData.MapCoords);
            retractor = entMng.SpawnEntity("Retractor", mapData.MapCoords);
            bonesetter = entMng.SpawnEntity("BoneSetter", mapData.MapCoords);
            boneGel = entMng.SpawnEntity("BoneGel", mapData.MapCoords);
            cautery = entMng.SpawnEntity("Cautery", mapData.MapCoords);
            patientComp = entMng.GetComponent<SurgeryPatientComponent>(patient);

            experienceSystem.TrySetOverrideSkill(surgeon, AnatomySkillTree, new SkillTreeInfo { Level = 4, Sublevel = 0 });
            deviceLink.LinkDefaults(null, computer, table);
            powerReceiver.SetNeedsPower(computer, false);
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            Assert.That(surgerySystem.HasLinkedOperatingComputer(table), Is.True);
            Assert.That(buckleSystem.TryBuckle(patient, surgeon, table, popup: false), Is.True);

            // Validate zone metadata on Phase 2 pathology prototypes
            Assert.That(protoManager.Index(LeftArmDislocationProto).TargetZone, Is.EqualTo(SurgeryPuppetPart.LeftArm));
            Assert.That(protoManager.Index(HeadCranialFissureProto).TargetZone, Is.EqualTo(SurgeryPuppetPart.Head));
            Assert.That(protoManager.Index(EyeCornealTraumaProto).TargetZone, Is.EqualTo(SurgeryPuppetPart.Eyes));
            Assert.That(protoManager.Index(MouthJawDislocationProto).TargetZone, Is.EqualTo(SurgeryPuppetPart.Mouth));

            Assert.That(statusEffects.TryAddStatusEffectDuration(patient, PainNumbnessEffectProto, TimeSpan.FromSeconds(120)), Is.True);

            // Start with dislocation and repair directly without incision
            Assert.That(pathologySystem.TryAddPathology(patient, "LeftArmDislocation"), Is.True);
            Assert.That(
                surgerySystem.TryStartSurgery(new Entity<SurgeryPatientComponent>(patient, patientComp), LeftArmGraphId, surgeon, drapes),
                Is.True);
        });

        await pair.RunTicksSync(2);
        await PerformStep(pair, LeftArmGraphId, patient, surgeon, patientComp, "start-leftarm-surgery", "start-leftarm-dislocation", bonesetter, 4.5f);

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(pathologySystem.HavePathology(patient, "LeftArmDislocation"), Is.False);
                Assert.That(patientComp.OngoingSurgeries.ContainsKey(LeftArmGraphId), Is.False);
            });

            // Now test full compound fracture surgery
            Assert.That(pathologySystem.TryAddPathology(patient, "LeftArmCompoundFracture"), Is.True);
            Assert.That(
                surgerySystem.TryStartSurgery(new Entity<SurgeryPatientComponent>(patient, patientComp), LeftArmGraphId, surgeon, drapes),
                Is.True);
        });

        await pair.RunTicksSync(2);
        await PerformStep(pair, LeftArmGraphId, patient, surgeon, patientComp, "start-leftarm-surgery", "leftarm-skin-incision", scalpel, 4.5f);
        await PerformStep(pair, LeftArmGraphId, patient, surgeon, patientComp, "leftarm-skin-incision", "leftarm-skin-clamp-bleeders", hemostat, 4.5f);
        await PerformStep(pair, LeftArmGraphId, patient, surgeon, patientComp, "leftarm-skin-clamp-bleeders", "retract-leftarm-skin", retractor, 4.5f);
        await PerformStep(pair, LeftArmGraphId, patient, surgeon, patientComp, "retract-leftarm-skin", "leftarm-compound-reset", bonesetter, 6f);

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(patientComp.OngoingSurgeries[LeftArmGraphId], Is.EqualTo("leftarm-compound-reset"));
                Assert.That(pathologySystem.HavePathology(patient, "LeftArmCompoundFracture"), Is.True);
            });
        });

        await PerformStep(pair, LeftArmGraphId, patient, surgeon, patientComp, "leftarm-compound-reset", "leftarm-compound-repair", boneGel, 4.5f);

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(patientComp.OngoingSurgeries[LeftArmGraphId], Is.EqualTo("retract-leftarm-skin"));
                Assert.That(pathologySystem.HavePathology(patient, "LeftArmCompoundFracture"), Is.False);
            });
        });

        await PerformStep(pair, LeftArmGraphId, patient, surgeon, patientComp, "retract-leftarm-skin", "seal-wound", cautery, 4.5f);

        await server.WaitAssertion(() =>
        {
            patientComp.OngoingSurgeries.TryGetValue(LeftArmGraphId, out var remainingNode);
            float? sealDelay = null;
            if (protoManager.TryIndex<SurgeryGraphPrototype>(LeftArmGraphId, out var graphProto)
                && graphProto.TryGetNode("retract-leftarm-skin", out var retractNode))
            {
                var sealEdge = retractNode.Edges.FirstOrDefault(e => e.Id == "seal-wound");
                if (sealEdge != null)
                    sealDelay = surgerySystem.GetStepDelay(patient, surgeon, cautery, sealEdge);
            }

            Assert.That(
                patientComp.OngoingSurgeries.ContainsKey(LeftArmGraphId),
                Is.False,
                $"Surgery did not finish after seal-wound. remainingNode={remainingNode}, sealDelay={sealDelay}, " +
                $"surgeonAlive={mobStateSystem.IsAlive(surgeon)}, patientAlive={mobStateSystem.IsAlive(patient)}, " +
                $"activeItem={handsSystem.GetActiveItem(surgeon)}, tool={cautery}, " +
                $"patientDamage={damageableSystem.GetTotalDamage(patient)}, surgeonDamage={damageableSystem.GetTotalDamage(surgeon)}");

            // Verify heavy blunt damage triggers trauma pathologies via PathologyProviderSystem
            var holder = entMng.GetComponent<PathologyHolderComponent>(patient);
            for (var i = 0; i < 8; i++)
            {
                damageableSystem.ClearAllDamage(patient);
                damageableSystem.TryChangeDamage(
                    patient,
                    new DamageSpecifier { DamageDict = { ["Blunt"] = 45 } },
                    ignoreResistances: true);
            }

            Assert.That(holder.ActivePathologies.Count, Is.GreaterThan(0));
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task EyesTransplantSurgeryTest()
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
        var pathologySystem = entMng.System<SharedPathologySystem>();
        var bodySystem = entMng.System<BodySystem>();
        var buckleSystem = entMng.System<SharedBuckleSystem>();
        var deviceLink = entMng.System<SharedDeviceLinkSystem>();
        var powerReceiver = entMng.System<SharedPowerReceiverSystem>();
        var experienceSystem = entMng.System<ExperienceSystem>();
        var statusEffects = entMng.System<StatusEffectsSystem>();
        var atmosSystem = entMng.System<AtmosphereSystem>();
        var handsSystem = entMng.System<SharedHandsSystem>();

        EntityUid patient = default!;
        EntityUid surgeon = default!;
        EntityUid table = default!;
        EntityUid drapes = default!;
        EntityUid hemostat = default!;
        EntityUid retractor = default!;
        EntityUid eyes = default!;
        SurgeryPatientComponent patientComp = default!;

        await server.WaitPost(() =>
        {
            var moles = new float[Atmospherics.AdjustedNumberOfGases];
            moles[(int)Gas.Oxygen] = 21.824779f;
            moles[(int)Gas.Nitrogen] = 82.10312f;
            atmosSystem.SetMapAtmosphere(mapData.MapUid, false, new GasMixture(moles, Atmospherics.T20C));
        });

        await server.WaitAssertion(() =>
        {
            patient = entMng.SpawnEntity("MobHuman", mapData.MapCoords);
            surgeon = entMng.SpawnEntity("MobHuman", mapData.MapCoords);
            table = entMng.SpawnEntity("OperatingTable", mapData.MapCoords);
            var computer = entMng.SpawnEntity("computerBodyScanner", new MapCoordinates(1f, 0f, mapData.MapId));
            drapes = entMng.SpawnEntity("SurgeryDrapes", mapData.MapCoords);
            hemostat = entMng.SpawnEntity("Hemostat", mapData.MapCoords);
            retractor = entMng.SpawnEntity("Retractor", mapData.MapCoords);
            patientComp = entMng.GetComponent<SurgeryPatientComponent>(patient);

            experienceSystem.TrySetOverrideSkill(surgeon, AnatomySkillTree, new SkillTreeInfo { Level = 4, Sublevel = 0 });
            deviceLink.LinkDefaults(null, computer, table);
            powerReceiver.SetNeedsPower(computer, false);
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            Assert.That(surgerySystem.HasLinkedOperatingComputer(table), Is.True);
            Assert.That(buckleSystem.TryBuckle(patient, surgeon, table, popup: false), Is.True);
            Assert.That(statusEffects.TryAddStatusEffectDuration(patient, PainNumbnessEffectProto, TimeSpan.FromSeconds(120)), Is.True);
            Assert.That(surgerySystem.TryStartSurgery(new Entity<SurgeryPatientComponent>(patient, patientComp), EyesSurgeryGraphId, surgeon, drapes), Is.True);
        });

        await pair.RunTicksSync(2);
        await PerformStep(pair, EyesSurgeryGraphId, patient, surgeon, patientComp, "start-eyes-surgery", "eyes-retract-lids", retractor, 4.5f);

        await server.WaitAssertion(() =>
        {
            var state = surgerySystem.MakeSelectorState(new Entity<SurgeryPatientComponent>(patient, patientComp), hemostat, surgeon);
            Assert.That(state.Infos.Any(x => x.TargetEdgeId == "extract-eyes"), Is.True);
            Assert.That(state.Infos.Any(x => x.TargetEdgeId == "insert-eyes"), Is.False);

            Assert.That(pathologySystem.TryAddPathology(patient, EyeCornealTraumaProto), Is.True);
        });

        await PerformStep(pair, EyesSurgeryGraphId, patient, surgeon, patientComp, "eyes-retract-lids", "extract-eyes", hemostat, 7f);

        await server.WaitAssertion(() =>
        {
            Assert.That(bodySystem.HasOrganCategory(patient, EyesCategory), Is.False);
            Assert.That(pathologySystem.HavePathology(patient, MissingEyesProto), Is.True);
            Assert.That(pathologySystem.HavePathology(patient, EyeCornealTraumaProto), Is.False, "Corneal trauma must leave with the eyes");
            Assert.That(entMng.HasComponent<PermanentBlindnessComponent>(patient), Is.True);

            eyes = default;
            foreach (var held in handsSystem.EnumerateHeld(surgeon))
            {
                if (bodySystem.IsOrganOfCategory(held, EyesCategory))
                    eyes = held;
            }

            Assert.That(eyes.IsValid(), Is.True, "Extracted organ must be put into the surgeon's hand");

            var state = surgerySystem.MakeSelectorState(new Entity<SurgeryPatientComponent>(patient, patientComp), eyes, surgeon);
            Assert.That(state.Infos.Any(x => x.TargetEdgeId == "extract-eyes"), Is.False, "Missing organ can't be extracted");
            Assert.That(state.Infos.Any(x => x.TargetEdgeId == "insert-eyes" && x.MetEdgeRequirement), Is.True);

            Assert.That(handsSystem.TryDrop(surgeon, eyes, checkActionBlocker: false), Is.True);
        });

        await PerformStep(pair, EyesSurgeryGraphId, patient, surgeon, patientComp, "eyes-retract-lids", "insert-eyes", eyes, 7f);

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(bodySystem.HasOrganCategory(patient, EyesCategory), Is.True);
                Assert.That(pathologySystem.HavePathology(patient, MissingEyesProto), Is.False);
                Assert.That(pathologySystem.HavePathology(patient, EyeCornealTraumaProto), Is.True, "Corneal trauma must come back with the same eyes");
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task LimbAmputationSurgeryTest()
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
        var pathologySystem = entMng.System<SharedPathologySystem>();
        var bodySystem = entMng.System<BodySystem>();
        var buckleSystem = entMng.System<SharedBuckleSystem>();
        var deviceLink = entMng.System<SharedDeviceLinkSystem>();
        var powerReceiver = entMng.System<SharedPowerReceiverSystem>();
        var experienceSystem = entMng.System<ExperienceSystem>();
        var statusEffects = entMng.System<StatusEffectsSystem>();
        var atmosSystem = entMng.System<AtmosphereSystem>();
        var handsSystem = entMng.System<SharedHandsSystem>();
        var damageableSystem = entMng.System<DamageableSystem>();

        EntityUid patient = default!;
        EntityUid surgeon = default!;
        EntityUid table = default!;
        EntityUid drapes = default!;
        EntityUid scalpel = default!;
        EntityUid hemostat = default!;
        EntityUid retractor = default!;
        EntityUid saw = default!;
        EntityUid cautery = default!;
        EntityUid reptilianArm = default!;
        SurgeryPatientComponent patientComp = default!;

        await server.WaitPost(() =>
        {
            var moles = new float[Atmospherics.AdjustedNumberOfGases];
            moles[(int)Gas.Oxygen] = 21.824779f;
            moles[(int)Gas.Nitrogen] = 82.10312f;
            atmosSystem.SetMapAtmosphere(mapData.MapUid, false, new GasMixture(moles, Atmospherics.T20C));
        });

        await server.WaitAssertion(() =>
        {
            patient = entMng.SpawnEntity("MobHuman", mapData.MapCoords);
            surgeon = entMng.SpawnEntity("MobHuman", mapData.MapCoords);
            table = entMng.SpawnEntity("OperatingTable", mapData.MapCoords);
            var computer = entMng.SpawnEntity("computerBodyScanner", new MapCoordinates(1f, 0f, mapData.MapId));
            drapes = entMng.SpawnEntity("SurgeryDrapes", mapData.MapCoords);
            scalpel = entMng.SpawnEntity("Scalpel", mapData.MapCoords);
            hemostat = entMng.SpawnEntity("Hemostat", mapData.MapCoords);
            retractor = entMng.SpawnEntity("Retractor", mapData.MapCoords);
            saw = entMng.SpawnEntity("SawElectric", mapData.MapCoords);
            cautery = entMng.SpawnEntity("Cautery", mapData.MapCoords);
            reptilianArm = entMng.SpawnEntity("OrganReptilianArmLeft", mapData.MapCoords);
            patientComp = entMng.GetComponent<SurgeryPatientComponent>(patient);

            experienceSystem.TrySetOverrideSkill(surgeon, AnatomySkillTree, new SkillTreeInfo { Level = 4, Sublevel = 0 });
            deviceLink.LinkDefaults(null, computer, table);
            powerReceiver.SetNeedsPower(computer, false);
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            Assert.That(buckleSystem.TryBuckle(patient, surgeon, table, popup: false), Is.True);
            Assert.That(statusEffects.TryAddStatusEffectDuration(patient, PainNumbnessEffectProto, TimeSpan.FromSeconds(120)), Is.True);
            Assert.That(surgerySystem.TryStartSurgery(new Entity<SurgeryPatientComponent>(patient, patientComp), LeftArmGraphId, surgeon, drapes), Is.True);
        });

        await pair.RunTicksSync(2);
        await PerformStep(pair, LeftArmGraphId, patient, surgeon, patientComp, "start-leftarm-surgery", "leftarm-skin-incision", scalpel, 4.5f);
        await PerformStep(pair, LeftArmGraphId, patient, surgeon, patientComp, "leftarm-skin-incision", "leftarm-skin-clamp-bleeders", hemostat, 4.5f);
        await PerformStep(pair, LeftArmGraphId, patient, surgeon, patientComp, "leftarm-skin-clamp-bleeders", "retract-leftarm-skin", retractor, 4.5f);

        await server.WaitAssertion(() =>
        {
            var state = surgerySystem.MakeSelectorState(new Entity<SurgeryPatientComponent>(patient, patientComp), saw, surgeon);
            Assert.That(state.Infos.Any(x => x.TargetEdgeId == "leftarm-amputate"), Is.True);
            Assert.That(state.Infos.Any(x => x.TargetEdgeId == "leftarm-attach"), Is.False);
        });

        await PerformStep(pair, LeftArmGraphId, patient, surgeon, patientComp, "retract-leftarm-skin", "leftarm-amputate", saw, 9f);

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(patientComp.OngoingSurgeries[LeftArmGraphId], Is.EqualTo("leftarm-amputated"));
                Assert.That(bodySystem.HasOrganCategory(patient, ArmLeftCategory), Is.False);
                Assert.That(pathologySystem.HavePathology(patient, MissingArmLeftProto), Is.True);
                Assert.That(handsSystem.GetHandCount(patient), Is.EqualTo(1));
                Assert.That(handsSystem.EnumerateHeld(surgeon).Any(x => bodySystem.IsOrganOfCategory(x, ArmLeftCategory)), Is.True,
                    "Severed arm must be put into the surgeon's hand");
            });

            var state = surgerySystem.MakeSelectorState(new Entity<SurgeryPatientComponent>(patient, patientComp), reptilianArm, surgeon);
            Assert.That(state.Infos.Any(x => x.TargetEdgeId == "leftarm-amputated-attach" && x.MetEdgeRequirement), Is.True);
        });

        await PerformStep(pair, LeftArmGraphId, patient, surgeon, patientComp, "leftarm-amputated", "leftarm-amputated-attach", reptilianArm, 7f);

        await server.WaitAssertion(() =>
        {
            var damage = damageableSystem.GetAllDamage(patient).DamageDict;
            Assert.Multiple(() =>
            {
                Assert.That(patientComp.OngoingSurgeries[LeftArmGraphId], Is.EqualTo("leftarm-attached"));
                Assert.That(bodySystem.TryGetOrganByCategory(patient, ArmLeftCategory, out var arm) && arm.Value.Owner == reptilianArm, Is.True);
                Assert.That(pathologySystem.HavePathology(patient, MissingArmLeftProto), Is.False);
                Assert.That(pathologySystem.HavePathology(patient, MissingHandLeftProto), Is.True, "Donor arm without a hand doesn't bring one");
                // part of the rejection damage heals naturally while the step timer runs out
                Assert.That(damage.TryGetValue(PoisonDamage, out var poison) && poison > 25, Is.True, "Arm of another species must be rejected");
            });
        });

        await PerformStep(pair, LeftArmGraphId, patient, surgeon, patientComp, "leftarm-attached", "seal-wound", cautery, 4.5f);

        await server.WaitAssertion(() =>
        {
            Assert.That(patientComp.OngoingSurgeries.ContainsKey(LeftArmGraphId), Is.False);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task DecapitationSurgeryTest()
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
        var bodySystem = entMng.System<BodySystem>();
        var buckleSystem = entMng.System<SharedBuckleSystem>();
        var deviceLink = entMng.System<SharedDeviceLinkSystem>();
        var powerReceiver = entMng.System<SharedPowerReceiverSystem>();
        var experienceSystem = entMng.System<ExperienceSystem>();
        var statusEffects = entMng.System<StatusEffectsSystem>();
        var handsSystem = entMng.System<SharedHandsSystem>();
        var mindSystem = entMng.System<SharedMindSystem>();
        var damageableSystem = entMng.System<DamageableSystem>();

        EntityUid patient = default!;
        EntityUid surgeon = default!;
        EntityUid table = default!;
        EntityUid drapes = default!;
        EntityUid shears = default!;
        EntityUid saw = default!;
        EntityUid cautery = default!;
        EntityUid mindId = default!;
        EntityUid head = default!;
        SurgeryPatientComponent patientComp = default!;

        await server.WaitAssertion(() =>
        {
            patient = entMng.SpawnEntity("MobHuman", mapData.MapCoords);
            surgeon = entMng.SpawnEntity("MobHuman", mapData.MapCoords);
            table = entMng.SpawnEntity("OperatingTable", mapData.MapCoords);
            var computer = entMng.SpawnEntity("computerBodyScanner", new MapCoordinates(1f, 0f, mapData.MapId));
            drapes = entMng.SpawnEntity("SurgeryDrapes", mapData.MapCoords);
            shears = entMng.SpawnEntity("AmputationShears", mapData.MapCoords);
            saw = entMng.SpawnEntity("SawElectric", mapData.MapCoords);
            cautery = entMng.SpawnEntity("Cautery", mapData.MapCoords);
            patientComp = entMng.GetComponent<SurgeryPatientComponent>(patient);
            mindId = mindSystem.CreateMind(null);
            mindSystem.TransferTo(mindId, patient);

            experienceSystem.TrySetOverrideSkill(surgeon, AnatomySkillTree, new SkillTreeInfo { Level = 4, Sublevel = 0 });
            deviceLink.LinkDefaults(null, computer, table);
            powerReceiver.SetNeedsPower(computer, false);
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            Assert.That(buckleSystem.TryBuckle(patient, surgeon, table, popup: false), Is.True);
            Assert.That(statusEffects.TryAddStatusEffectDuration(patient, PainNumbnessEffectProto, TimeSpan.FromSeconds(120)), Is.True);
            Assert.That(surgerySystem.TryStartSurgery(new Entity<SurgeryPatientComponent>(patient, patientComp), HeadSurgeryGraphId, surgeon, drapes), Is.True);
        });

        await pair.RunTicksSync(2);
        await PerformStep(pair, HeadSurgeryGraphId, patient, surgeon, patientComp, "surgery-head-start", "head-amputate-shears", shears, 16f);

        await server.WaitAssertion(() =>
        {
            Assert.That(bodySystem.TryGetOrganByCategory(patient, BrainCategory, out _), Is.False);
            head = handsSystem.EnumerateHeld(surgeon).First(x => bodySystem.IsOrganOfCategory(x, HeadCategory));
            Assert.Multiple(() =>
            {
                Assert.That(patientComp.OngoingSurgeries[HeadSurgeryGraphId], Is.EqualTo("head-amputated"));
                Assert.That(bodySystem.HasOrganCategory(patient, HeadCategory), Is.False);
                Assert.That(entMng.GetComponent<MindComponent>(mindId).OwnedEntity, Is.Not.EqualTo(patient), "The person leaves with the head");
                Assert.That(surgerySystem.CanStartSurgery(surgeon, EyesSurgeryGraphId, patient, drapes, out _), Is.False, "No eyes surgery without a head");
                Assert.That(damageableSystem.GetAllDamage(patient).DamageDict.TryGetValue(SlashDamage, out var slash) ? slash.Float() : 0f, Is.GreaterThanOrEqualTo(100f), "Shears cut the neck hard");
                var bloodstream = entMng.GetComponent<BloodstreamComponent>(patient);
                // the bleeding starts at the maximum and has been easing off for a few seconds since
                Assert.That(bloodstream.BleedAmount, Is.GreaterThan(bloodstream.MaxBleedAmount * 0.7f), "The stump bleeds as hard as it can");
            });
        });

        await PerformStep(pair, HeadSurgeryGraphId, patient, surgeon, patientComp, "head-amputated", "head-amputated-attach", head, 7f);

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(patientComp.OngoingSurgeries[HeadSurgeryGraphId], Is.EqualTo("head-attached"));
                Assert.That(bodySystem.HasOrganCategory(patient, HeadCategory), Is.True);
                Assert.That(bodySystem.HasOrganCategory(patient, BrainCategory), Is.True);
                Assert.That(entMng.GetComponent<MindComponent>(mindId).OwnedEntity, Is.EqualTo(patient), "The attached head brings the person back");
            });

            // the skull is reachable only with a head on
            var state = surgerySystem.MakeSelectorState(new Entity<SurgeryPatientComponent>(patient, patientComp), saw, surgeon);
            Assert.That(state.Infos.Any(x => x.TargetEdgeId == "head-amputate"), Is.False);
        });

        await PerformStep(pair, HeadSurgeryGraphId, patient, surgeon, patientComp, "head-attached", "seal-wound", cautery, 4.5f);

        await server.WaitAssertion(() =>
        {
            Assert.That(patientComp.OngoingSurgeries.ContainsKey(HeadSurgeryGraphId), Is.False);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task ProsthesisSurgeryTest()
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
        var bodySystem = entMng.System<BodySystem>();
        var buckleSystem = entMng.System<SharedBuckleSystem>();
        var deviceLink = entMng.System<SharedDeviceLinkSystem>();
        var powerReceiver = entMng.System<SharedPowerReceiverSystem>();
        var experienceSystem = entMng.System<ExperienceSystem>();
        var statusEffects = entMng.System<StatusEffectsSystem>();
        var handsSystem = entMng.System<SharedHandsSystem>();
        var damageableSystem = entMng.System<DamageableSystem>();

        EntityUid patient = default!;
        EntityUid surgeon = default!;
        EntityUid table = default!;
        EntityUid drapes = default!;
        EntityUid scalpel = default!;
        EntityUid retractor = default!;
        EntityUid prosthesis = default!;
        SurgeryPatientComponent patientComp = default!;

        await server.WaitAssertion(() =>
        {
            patient = entMng.SpawnEntity("MobHuman", mapData.MapCoords);
            surgeon = entMng.SpawnEntity("MobHuman", mapData.MapCoords);
            table = entMng.SpawnEntity("OperatingTable", mapData.MapCoords);
            var computer = entMng.SpawnEntity("computerBodyScanner", new MapCoordinates(1f, 0f, mapData.MapId));
            drapes = entMng.SpawnEntity("SurgeryDrapes", mapData.MapCoords);
            scalpel = entMng.SpawnEntity("Scalpel", mapData.MapCoords);
            retractor = entMng.SpawnEntity("Retractor", mapData.MapCoords);
            prosthesis = entMng.SpawnEntity("ProsthesisCyberArmLeft", mapData.MapCoords);
            patientComp = entMng.GetComponent<SurgeryPatientComponent>(patient);

            experienceSystem.TrySetOverrideSkill(surgeon, AnatomySkillTree, new SkillTreeInfo { Level = 4, Sublevel = 0 });
            deviceLink.LinkDefaults(null, computer, table);
            powerReceiver.SetNeedsPower(computer, false);
            Assert.That(bodySystem.TryExtractOrgan(patient, ArmLeftCategory, out _), Is.True);
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            Assert.That(buckleSystem.TryBuckle(patient, surgeon, table, popup: false), Is.True);
            Assert.That(statusEffects.TryAddStatusEffectDuration(patient, PainNumbnessEffectProto, TimeSpan.FromSeconds(120)), Is.True);
            Assert.That(surgerySystem.TryStartSurgery(new Entity<SurgeryPatientComponent>(patient, patientComp), LeftArmGraphId, surgeon, drapes), Is.True);
        });

        await pair.RunTicksSync(2);
        await PerformStep(pair, LeftArmGraphId, patient, surgeon, patientComp, "start-leftarm-surgery", "leftarm-install-prosthesis", prosthesis, 7f);

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(patientComp.OngoingSurgeries.ContainsKey(LeftArmGraphId), Is.False, "Installing a prosthesis doesn't leave a wound to close");
                Assert.That(bodySystem.TryGetOrganByCategory(patient, ArmLeftCategory, out var arm) && arm.Value.Owner == prosthesis, Is.True);
                Assert.That(bodySystem.HasOrganCategory(patient, HandLeftCategory), Is.True, "The prosthesis brings its hand");
                Assert.That(damageableSystem.GetAllDamage(patient).DamageDict.TryGetValue(PoisonDamage, out var poison) && poison > 0, Is.False, "A prosthesis isn't rejected");
                Assert.That(surgerySystem.CanStartSurgery(surgeon, LeftHandGraphId, patient, drapes, out _), Is.False, "The hand of a prosthesis isn't operated on alone");
            });

            Assert.That(surgerySystem.TryStartSurgery(new Entity<SurgeryPatientComponent>(patient, patientComp), LeftArmGraphId, surgeon, drapes), Is.True);
            var scalpelState = surgerySystem.MakeSelectorState(new Entity<SurgeryPatientComponent>(patient, patientComp), scalpel, surgeon);
            Assert.That(scalpelState.Infos.Any(x => x.TargetEdgeId == "leftarm-skin-incision"), Is.False, "A prosthesis has no skin to cut");
        });

        await pair.RunTicksSync(2);
        await PerformStep(pair, LeftArmGraphId, patient, surgeon, patientComp, "start-leftarm-surgery", "leftarm-remove-prosthesis", retractor, 6f);

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(patientComp.OngoingSurgeries.ContainsKey(LeftArmGraphId), Is.False);
                Assert.That(bodySystem.HasOrganCategory(patient, ArmLeftCategory), Is.False);
                Assert.That(handsSystem.EnumerateHeld(surgeon).Contains(prosthesis), Is.True, "The removed prosthesis goes to the surgeon's hand");
            });
        });

        await pair.CleanReturnAsync();
    }

    private static async Task PerformStep(
        Pair.TestPair pair,
        ProtoId<SurgeryGraphPrototype> graphId,
        EntityUid patient,
        EntityUid surgeon,
        SurgeryPatientComponent patientComp,
        string nodeId,
        string edgeId,
        EntityUid tool,
        float waitSeconds = 6f)
    {
        var server = pair.Server;
        var protoManager = server.ResolveDependency<IPrototypeManager>();
        var entMng = server.ResolveDependency<IEntityManager>();
        var surgerySystem = entMng.System<SharedSurgerySystem>();
        var handsSystem = entMng.System<SharedHandsSystem>();

        await server.WaitAssertion(() =>
        {
            if (handsSystem.GetActiveItem(surgeon) != tool)
            {
                handsSystem.TryDrop(surgeon, checkActionBlocker: false);
                Assert.That(handsSystem.TryPickup(surgeon, tool, checkActionBlocker: false, animate: false), Is.True);
            }
            Assert.That(protoManager.TryIndex<SurgeryGraphPrototype>(graphId, out var graphProto), Is.True);
            Assert.That(graphProto!.TryGetNode(nodeId, out var node), Is.True);
            var edge = node.Edges.FirstOrDefault(e => e.Id == edgeId);
            Assert.That(edge, Is.Not.Null, $"Missing edge {edgeId} on node {nodeId}");

            var failChance = surgerySystem.GetStepFailureChance(patient, surgeon, tool, edge!);
            Assert.That(failChance, Is.EqualTo(0f), $"Expected 0 fail chance on linked table under anesthesia, got {failChance} for {edgeId}");

            var started = surgerySystem.TryPerformOperationStep(
                new Entity<SurgeryPatientComponent>(patient, patientComp),
                graphId,
                edge!,
                tool,
                surgeon);
            Assert.That(started, Is.True, $"Failed to start surgery step {edgeId}");
        });

        await pair.RunSeconds(waitSeconds);
    }
}
