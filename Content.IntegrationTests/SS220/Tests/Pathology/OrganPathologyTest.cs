// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Atmos.EntitySystems;
using Content.Server.Body.Components;
using Content.Server.SS220.LimitationRevive;
using Content.Shared.Administration.Systems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Body;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.EntityConditions;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.SS220.EntityConditions;
using Content.Shared.SS220.LimitationRevive;
using Content.Shared.SS220.Pathology;
using Content.Shared.SS220.Surgery.Systems;
using Content.Shared.StatusEffectNew;
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
    private static readonly ProtoId<OrganCategoryPrototype> BrainCategory = "Brain";
    private static readonly ProtoId<PathologyPrototype> SlownessProto = "Slowness";
    private static readonly ProtoId<PathologyPrototype> MissingBrainProto = "MissingBrain";
    private static readonly ProtoId<PathologyPrototype> RetinaDamageProto = "RetinaDamage";
    private static readonly ProtoId<PathologyPrototype> MissingEyesProto = "MissingEyes";
    private static readonly ProtoId<PathologyPrototype> LungDamageProto = "LungDamage";
    private static readonly ProtoId<OrganCategoryPrototype> EyesCategory = "Eyes";
    private static readonly ProtoId<PathologyPrototype> PainNumbnessProto = "PainNumbness";
    private static readonly ProtoId<DamageTypePrototype> CerebralDamage = "Cerebral";
    private static readonly ProtoId<OrganCategoryPrototype> HeartCategory = "Heart";
    private static readonly ProtoId<PathologyPrototype> MissingHeartProto = "MissingHeart";

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

    [Test]
    public async Task SpeciesOrganCompatibilityTest()
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
        var initialBodySystem = entMng.System<InitialBodySystem>();

        await server.WaitAssertion(() =>
        {
            var human = entMng.SpawnEntity("MobHuman", mapData.MapCoords);
            var slime = entMng.SpawnEntity("MobSlimePerson", mapData.MapCoords);
            var slimeCore = entMng.SpawnEntity("OrganSlimePersonCore", mapData.MapCoords);
            var humanBrain = entMng.SpawnEntity("OrganHumanBrain", mapData.MapCoords);
            var humanHeart = entMng.SpawnEntity("OrganHumanHeart", mapData.MapCoords);
            var humanStomach = entMng.SpawnEntity("OrganHumanStomach", mapData.MapCoords);
            var dionaStomach = entMng.SpawnEntity("OrganDionaStomach", mapData.MapCoords);
            var voxLungs = entMng.SpawnEntity("OrganVoxLungs", mapData.MapCoords);
            var reptilianLiver = entMng.SpawnEntity("OrganReptilianLiver", mapData.MapCoords);

            Assert.Multiple(() =>
            {
                Assert.That(initialBodySystem.IsOrganCompatible(human, humanBrain), Is.True);
                Assert.That(initialBodySystem.IsOrganCompatible(human, voxLungs), Is.True, "Same stages, only the metabolizer type differs");
                Assert.That(initialBodySystem.IsOrganCompatible(human, reptilianLiver), Is.True);
                Assert.That(initialBodySystem.IsOrganCompatible(human, slimeCore), Is.False, "Slime core also digests and filters blood");
                Assert.That(initialBodySystem.IsOrganCompatible(human, dionaStomach), Is.False);
                Assert.That(initialBodySystem.IsOrganCompatible(slime, humanBrain), Is.False, "Human brain can't replace the slime core");
                Assert.That(initialBodySystem.IsOrganCompatible(slime, slimeCore), Is.True);
                Assert.That(initialBodySystem.IsOrganCompatible(slime, humanHeart), Is.False, "Slimes have no hearts");
                Assert.That(initialBodySystem.IsOrganCompatible(slime, humanStomach), Is.False);
            });

            Assert.That(bodySystem.TryInsertOrgan(slime, humanHeart), Is.True);
            Assert.That(bodySystem.TryExtractOrgan(slime, HeartCategory, out _), Is.True);
            Assert.That(pathologySystem.HavePathology(slime, MissingHeartProto), Is.False);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task BrainPathologyMovesWithBrainTest()
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

            Assert.That(pathologySystem.TryAddPathology(donor, SlownessProto), Is.True);
            Assert.That(bodySystem.TryExtractOrgan(donor, BrainCategory, out var brain), Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(pathologySystem.HavePathology(donor, SlownessProto), Is.False);
                Assert.That(pathologySystem.HavePathology(donor, MissingBrainProto), Is.True);
            });

            Assert.That(bodySystem.TryExtractOrgan(recipient, BrainCategory, out _), Is.True);
            Assert.That(bodySystem.TryInsertOrgan(recipient, brain!.Value.Owner), Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(pathologySystem.HavePathology(recipient, SlownessProto), Is.True, "Brain trauma must move with the brain");
                Assert.That(pathologySystem.HavePathology(recipient, MissingBrainProto), Is.False);
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task BrainCarriesReviveStateTest()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = true,
            DummyTicker = false
        });

        var mapData = await pair.CreateTestMap();
        var server = pair.Server;
        var entMng = server.ResolveDependency<IEntityManager>();
        var bodySystem = entMng.System<BodySystem>();
        var damageableSystem = entMng.System<DamageableSystem>();

        await server.WaitAssertion(() =>
        {
            var donor = entMng.SpawnEntity("MobHuman", mapData.MapCoords);
            var recipient = entMng.SpawnEntity("MobHuman", mapData.MapCoords);
            var donorRevive = entMng.GetComponent<LimitationReviveComponent>(donor);
            var recipientRevive = entMng.GetComponent<LimitationReviveComponent>(recipient);

            donorRevive.DeathCounter = 2;
            var cerebral = new DamageSpecifier();
            cerebral.DamageDict.Add(CerebralDamage, 15);
            Assert.That(damageableSystem.TryChangeDamage(donor, cerebral, ignoreResistances: true), Is.True);

            Assert.That(bodySystem.TryExtractOrgan(donor, BrainCategory, out var brain), Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(donorRevive.DeathCounter, Is.Zero);
                Assert.That(GetCerebral(damageableSystem, donor), Is.EqualTo(FixedPoint2.Zero), "Brain damage must leave with the brain");
                Assert.That(entMng.HasComponent<BrainReviveStateComponent>(brain!.Value.Owner), Is.True);
            });

            Assert.That(bodySystem.TryExtractOrgan(recipient, BrainCategory, out _), Is.True);
            Assert.That(bodySystem.TryInsertOrgan(recipient, brain!.Value.Owner), Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(recipientRevive.DeathCounter, Is.EqualTo(2), "A transplanted brain must not reset the revive limit");
                Assert.That(GetCerebral(damageableSystem, recipient), Is.EqualTo(FixedPoint2.New(15)));
                Assert.That(entMng.HasComponent<BrainReviveStateComponent>(brain.Value.Owner), Is.False);
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task AcquiredAnalgesiaAnesthetizesTest()
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
        var surgerySystem = entMng.System<SharedSurgerySystem>();

        EntityUid patient = default!;
        await server.WaitAssertion(() =>
        {
            patient = entMng.SpawnEntity("MobHuman", mapData.MapCoords);
            Assert.That(surgerySystem.IsPatientAnesthetized(patient), Is.False);

            Assert.That(pathologySystem.TryAddPathology(patient, PainNumbnessProto), Is.True);
            Assert.That(surgerySystem.IsPatientAnesthetized(patient), Is.True, "Acquired analgesia must numb the pain");

            Assert.That(pathologySystem.TryRemovePathology(patient, PainNumbnessProto), Is.True);
        });

        await pair.RunTicksSync(1);

        await server.WaitAssertion(() =>
        {
            Assert.That(surgerySystem.IsPatientAnesthetized(patient), Is.False);
        });

        await pair.CleanReturnAsync();
    }

    private static FixedPoint2 GetCerebral(DamageableSystem damageableSystem, EntityUid uid)
    {
        return damageableSystem.GetAllDamage(uid).DamageDict.TryGetValue(CerebralDamage, out var damage) ? damage : FixedPoint2.Zero;
    }

    [Test]
    public async Task BurntOutEyesDamageRetinaTest()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = true,
            DummyTicker = false
        });

        var server = pair.Server;
        var entMng = server.ResolveDependency<IEntityManager>();
        var pathologySystem = entMng.System<SharedPathologySystem>();
        var blindableSystem = entMng.System<BlindableSystem>();

        await server.WaitAssertion(() =>
        {
            var human = entMng.SpawnEntity("MobHuman", MapCoordinates.Nullspace);
            var blindable = entMng.GetComponent<BlindableComponent>(human);

            blindableSystem.AdjustEyeDamage((human, blindable), blindable.MaxDamage - 1);
            Assert.That(pathologySystem.HavePathology(human, RetinaDamageProto), Is.False, "Damaged but not burnt out eyes keep the retina");

            blindableSystem.AdjustEyeDamage((human, blindable), 1);
            blindableSystem.AdjustEyeDamage((human, blindable), 1);
            Assert.That(pathologySystem.TryGetPathologyStack(human, RetinaDamageProto, out var stacks), Is.True);
            Assert.That(stacks, Is.EqualTo(2), "Every injury of burnt out eyes damages the retina");
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task LungFailureStopsBreathingTest()
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
        var atmosSystem = entMng.System<AtmosphereSystem>();

        EntityUid human = default!;

        await server.WaitAssertion(() =>
        {
            var moles = new float[Atmospherics.AdjustedNumberOfGases];
            moles[(int)Gas.Oxygen] = 21.824779f;
            moles[(int)Gas.Nitrogen] = 82.10312f;
            atmosSystem.SetMapAtmosphere(mapData.MapUid, false, new GasMixture(moles, Atmospherics.T20C));

            human = entMng.SpawnEntity("MobHuman", mapData.MapCoords);
            Assert.That(pathologySystem.TryAddPathology(human, LungDamageProto), Is.True);
            Assert.That(pathologySystem.TryChangePathologyStack(human, LungDamageProto, 4), Is.True);
        });

        await pair.RunSeconds(15f);

        await server.WaitAssertion(() =>
        {
            var respirator = entMng.GetComponent<RespiratorComponent>(human);
            Assert.That(entMng.GetComponent<PathologyHolderComponent>(human).ActivePathologies[LungDamageProto].Level, Is.EqualTo(1));
            Assert.That(respirator.Saturation, Is.LessThan(respirator.SuffocationThreshold), "Failed lungs must not take in air");
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task HeavyDrunkennessConditionTest()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = true,
            DummyTicker = false
        });

        var server = pair.Server;
        var entMng = server.ResolveDependency<IEntityManager>();
        var conditions = entMng.System<SharedEntityConditionsSystem>();
        var statusEffects = entMng.System<StatusEffectsSystem>();
        var heavyDrunk = new StatusEffectTimeCondition { Effect = "StatusEffectDrunk", Min = TimeSpan.FromSeconds(120) };

        await server.WaitAssertion(() =>
        {
            var sober = entMng.SpawnEntity("MobHuman", MapCoordinates.Nullspace);
            var tipsy = entMng.SpawnEntity("MobHuman", MapCoordinates.Nullspace);
            var drunk = entMng.SpawnEntity("MobHuman", MapCoordinates.Nullspace);

            Assert.That(statusEffects.TrySetStatusEffectDuration(tipsy, "StatusEffectDrunk", TimeSpan.FromSeconds(30)), Is.True);
            Assert.That(statusEffects.TrySetStatusEffectDuration(drunk, "StatusEffectDrunk", TimeSpan.FromSeconds(200)), Is.True);

            Assert.Multiple(() =>
            {
                Assert.That(conditions.TryCondition(sober, heavyDrunk), Is.False);
                Assert.That(conditions.TryCondition(tipsy, heavyDrunk), Is.False, "A pint of beer must not damage the liver");
                Assert.That(conditions.TryCondition(drunk, heavyDrunk), Is.True);
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task RottenOrganFailsTest()
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
            var corpse = entMng.SpawnEntity("MobHuman", mapData.MapCoords);
            var recipient = entMng.SpawnEntity("MobHuman", mapData.MapCoords);
            entMng.EnsureComponent<RottingComponent>(corpse);

            Assert.That(bodySystem.TryExtractOrgan(corpse, LiverCategory, out var rottenLiver), Is.True);
            Assert.That(entMng.TryGetComponent<OrganPathologyStorageComponent>(rottenLiver!.Value, out var storage), Is.True);
            Assert.That(storage!.Pathologies[OrganDamageProto].Level, Is.EqualTo(1), "Organ from a rotten body must be failed");

            Assert.That(bodySystem.TryExtractOrgan(recipient, LiverCategory, out _), Is.True);
            Assert.That(bodySystem.TryInsertOrgan(recipient, rottenLiver.Value.Owner), Is.True);

            var holder = entMng.GetComponent<PathologyHolderComponent>(recipient);
            Assert.Multiple(() =>
            {
                Assert.That(holder.ActivePathologies[OrganDamageProto].Level, Is.EqualTo(1));
                Assert.That(entMng.HasComponent<PermanentBlindnessComponent>(recipient), Is.True, "Restored pathology must apply effects of all its levels");
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task RejuvenateRestoresOrgansTest()
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
        var rejuvenateSystem = entMng.System<RejuvenateSystem>();

        await server.WaitAssertion(() =>
        {
            var human = entMng.SpawnEntity("MobHuman", mapData.MapCoords);
            var voxLiver = entMng.SpawnEntity("OrganVoxLiver", mapData.MapCoords);

            Assert.That(bodySystem.TryExtractOrgan(human, EyesCategory, out _), Is.True);
            Assert.That(bodySystem.TryExtractOrgan(human, LiverCategory, out _), Is.True);
            Assert.That(bodySystem.TryInsertOrgan(human, voxLiver), Is.True);
            Assert.That(pathologySystem.TryAddPathology(human, SlownessProto), Is.True);
            Assert.That(pathologySystem.HavePathology(human, MissingEyesProto), Is.True);

            rejuvenateSystem.PerformRejuvenate(human);

            var holder = entMng.GetComponent<PathologyHolderComponent>(human);
            Assert.Multiple(() =>
            {
                Assert.That(bodySystem.HasOrganCategory(human, EyesCategory), Is.True, "Missing organs must be restored");
                Assert.That(bodySystem.TryGetOrganByCategory(human, LiverCategory, out var liver), Is.True);
                Assert.That(entMng.GetComponent<MetaDataComponent>(liver!.Value).EntityPrototype?.ID, Is.EqualTo("OrganHumanLiver"), "Transplanted organs must be swapped back");
                Assert.That(entMng.Deleted(voxLiver), Is.True);
                Assert.That(holder.ActivePathologies, Is.Empty, "Rejuvenate must remove all pathologies");
            });
        });

        await pair.CleanReturnAsync();
    }
}
