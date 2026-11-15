// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Bed.Sleep;
using Content.Shared.Body.Systems;
using Content.Shared.Buckle;
using Content.Shared.Chemistry.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.DeviceLinking;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Mobs.Systems;
using Content.Shared.Power.EntitySystems;
using Content.Shared.SS220.Experience;
using Content.Shared.SS220.Experience.Systems;
using Content.Shared.SS220.Pathology;
using Content.Shared.SS220.Surgery;
using Content.Shared.SS220.Surgery.Components;
using Content.Shared.SS220.Surgery.Systems;
using Content.Shared.SS220.Surgery.Graph;
using Content.Shared.StatusEffectNew;
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
    private static readonly ProtoId<SkillTreePrototype> AnatomySkillTree = "Anatomy";
    private static readonly EntProtoId PainNumbnessEffectProto = "StatusEffectPainNumbness";

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
        var pointLight = entMng.System<SharedPointLightSystem>();
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
        EntityUid surgicalTape = default!;
        SurgeryPatientComponent patientComp = default!;

        await server.WaitPost(() =>
        {
            var moles = new float[Atmospherics.AdjustedNumberOfGases];
            moles[(int)Gas.Oxygen] = 21.824779f;
            moles[(int)Gas.Nitrogen] = 82.10312f;
            atmosSystem.SetMapAtmosphere(mapData.MapUid, false, new GasMixture(moles, Atmospherics.T20C));
        });

        async Task PerformStep(string nodeId, string edgeId, EntityUid tool, float waitSeconds = 6f)
        {
            await server.WaitAssertion(() =>
            {
                if (handsSystem.GetActiveItem(surgeon) != tool)
                {
                    handsSystem.TryDrop(surgeon, checkActionBlocker: false);
                    Assert.That(handsSystem.TryPickup(surgeon, tool, checkActionBlocker: false, animate: false), Is.True);
                }
                Assert.That(protoManager.TryIndex<SurgeryGraphPrototype>(LeftArmGraphId, out var graphProto), Is.True);
                Assert.That(graphProto!.TryGetNode(nodeId, out var node), Is.True);
                var edge = node.Edges.FirstOrDefault(e => e.Id == edgeId);
                Assert.That(edge, Is.Not.Null, $"Missing edge {edgeId} on node {nodeId}");

                var failChance = surgerySystem.GetStepFailureChance(patient, surgeon, tool, edge!);
                Assert.That(failChance, Is.EqualTo(0f), $"Expected 0 fail chance on linked table under anesthesia, got {failChance} for {edgeId}");

                var started = surgerySystem.TryPerformOperationStep(
                    new Entity<SurgeryPatientComponent>(patient, patientComp),
                    LeftArmGraphId,
                    edge!,
                    tool,
                    surgeon);
                Assert.That(started, Is.True, $"Failed to start surgery step {edgeId}");
            });

            await pair.RunSeconds(waitSeconds);
        }

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
            surgicalTape = entMng.SpawnEntity("SurgicalTape", mapData.MapCoords);
            patientComp = entMng.GetComponent<SurgeryPatientComponent>(patient);

            experienceSystem.TrySetOverrideSkill(surgeon, AnatomySkillTree, new SkillTreeInfo { Level = 4, Sublevel = 0 });
            pointLight.EnsureLight(table);
            pointLight.SetEnabled(table, true);
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

            // SurgicalTape is painless even while the patient is awake
            var graph = protoManager.Index(LeftArmGraphId);
            Assert.That(graph.TryGetNode("leftarm-compound-reset", out var resetNode), Is.True);
            var tapeEdge = resetNode!.Edges.First(e => e.Id == "leftarm-compound-repair");
            Assert.Multiple(() =>
            {
                Assert.That(surgerySystem.IsPainlessTool(surgicalTape), Is.True);
                Assert.That(surgerySystem.IsPainlessTool(boneGel), Is.False);
                Assert.That(
                    surgerySystem.GetStepFailureChance(patient, surgeon, surgicalTape, tapeEdge),
                    Is.LessThan(surgerySystem.GetStepFailureChance(patient, surgeon, boneGel, tapeEdge)));
            });

            Assert.That(statusEffects.TryAddStatusEffectDuration(patient, PainNumbnessEffectProto, TimeSpan.FromSeconds(120)), Is.True);

            // Start with dislocation and repair directly without incision
            Assert.That(pathologySystem.TryAddPathology(patient, "LeftArmDislocation"), Is.True);
            Assert.That(
                surgerySystem.TryStartSurgery(new Entity<SurgeryPatientComponent>(patient, patientComp), LeftArmGraphId, surgeon, drapes),
                Is.True);
        });

        await pair.RunTicksSync(2);
        await PerformStep("start-leftarm-surgery", "start-leftarm-dislocation", bonesetter, 4.5f);

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(pathologySystem.HavePathology(patient, "LeftArmDislocation"), Is.False);
                Assert.That(patientComp.OngoingSurgeries.ContainsKey(LeftArmGraphId), Is.False);
            });

            // Now test full compound fracture surgery using SurgicalTape
            Assert.That(pathologySystem.TryAddPathology(patient, "LeftArmCompoundFracture"), Is.True);
            Assert.That(
                surgerySystem.TryStartSurgery(new Entity<SurgeryPatientComponent>(patient, patientComp), LeftArmGraphId, surgeon, drapes),
                Is.True);
        });

        await pair.RunTicksSync(2);
        await PerformStep("start-leftarm-surgery", "leftarm-skin-incision", scalpel, 4.5f);
        await PerformStep("leftarm-skin-incision", "leftarm-skin-clamp-bleeders", hemostat, 4.5f);
        await PerformStep("leftarm-skin-clamp-bleeders", "retract-leftarm-skin", retractor, 4.5f);
        await PerformStep("retract-leftarm-skin", "leftarm-compound-reset", bonesetter, 6f);

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(patientComp.OngoingSurgeries[LeftArmGraphId], Is.EqualTo("leftarm-compound-reset"));
                Assert.That(pathologySystem.HavePathology(patient, "LeftArmCompoundFracture"), Is.True);
            });
        });

        await PerformStep("leftarm-compound-reset", "leftarm-compound-repair", surgicalTape, 4.5f);

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(patientComp.OngoingSurgeries[LeftArmGraphId], Is.EqualTo("retract-leftarm-skin"));
                Assert.That(pathologySystem.HavePathology(patient, "LeftArmCompoundFracture"), Is.False);
            });
        });

        await PerformStep("retract-leftarm-skin", "seal-wound", surgicalTape, 4.5f);

        await server.WaitAssertion(() =>
        {
            patientComp.OngoingSurgeries.TryGetValue(LeftArmGraphId, out var remainingNode);
            float? sealDelay = null;
            if (protoManager.TryIndex<SurgeryGraphPrototype>(LeftArmGraphId, out var graphProto)
                && graphProto.TryGetNode("retract-leftarm-skin", out var retractNode))
            {
                var sealEdge = retractNode.Edges.FirstOrDefault(e => e.Id == "seal-wound");
                if (sealEdge != null)
                    sealDelay = surgerySystem.GetStepDelay(patient, surgeon, surgicalTape, sealEdge);
            }

            Assert.That(
                patientComp.OngoingSurgeries.ContainsKey(LeftArmGraphId),
                Is.False,
                $"Surgery did not finish after seal-wound. remainingNode={remainingNode}, sealDelay={sealDelay}, " +
                $"surgeonAlive={mobStateSystem.IsAlive(surgeon)}, patientAlive={mobStateSystem.IsAlive(patient)}, " +
                $"activeItem={handsSystem.GetActiveItem(surgeon)}, tool={surgicalTape}, " +
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
}
