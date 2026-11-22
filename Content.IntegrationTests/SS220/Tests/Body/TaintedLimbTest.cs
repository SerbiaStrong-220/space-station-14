// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Zombies;
using Content.Shared.Atmos.Rotting;
using Content.Shared.Body;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.SS220.Pathology;
using Content.Shared.Zombies;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.SS220.Tests.Body;

[TestFixture]
public sealed class TaintedLimbTest
{
    private static readonly ProtoId<OrganCategoryPrototype> ArmLeftCategory = "ArmLeft";
    private static readonly ProtoId<OrganCategoryPrototype> HandLeftCategory = "HandLeft";
    private static readonly ProtoId<OrganCategoryPrototype> LegLeftCategory = "LegLeft";
    private static readonly ProtoId<OrganCategoryPrototype> HeartCategory = "Heart";
    private static readonly ProtoId<PathologyPrototype> TaintedFleshProto = "TaintedFlesh";
    private static readonly ProtoId<DamageTypePrototype> PoisonDamage = "Poison";

    [TestCase(false)]
    [TestCase(true)]
    public async Task ZombieArmInfectsTest(bool vaccinated)
    {
        await using var pair = await PoolManager.GetServerClient();
        var mapData = await pair.CreateTestMap();
        var server = pair.Server;
        var entMng = server.ResolveDependency<IEntityManager>();
        var bodySystem = entMng.System<BodySystem>();
        var pathologySystem = entMng.System<SharedPathologySystem>();

        EntityUid human = default!;
        EntityUid arm = default!;
        await server.WaitAssertion(() =>
        {
            var zombie = entMng.SpawnEntity("MobHuman", mapData.MapCoords);
            human = entMng.SpawnEntity("MobHuman", mapData.MapCoords);
            entMng.System<ZombieSystem>().ZombifyEntity(zombie);
            if (vaccinated)
                entMng.AddComponent<ZombieImmuneComponent>(human);

            Assert.That(bodySystem.TryExtractOrgan(zombie, ArmLeftCategory, out var zombieArm), Is.True);
            Assert.That(bodySystem.TryExtractOrgan(human, ArmLeftCategory, out var ownArm), Is.True);
            entMng.DeleteEntity(ownArm!.Value);
            arm = zombieArm!.Value;

            Assert.That(bodySystem.TryInsertOrgan(human, arm), Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(bodySystem.HasOrganCategory(human, HandLeftCategory), Is.True, "The zombie hand comes with its arm");
                Assert.That(pathologySystem.HavePathology(human, TaintedFleshProto), Is.True);
                Assert.That(entMng.HasComponent<PendingZombieComponent>(human), Is.Not.EqualTo(vaccinated), "Only the vaccine stops the infection");
            });
        });

        await server.WaitAssertion(() =>
        {
            Assert.That(bodySystem.TryExtractOrgan(human, ArmLeftCategory, out _), Is.True);
            Assert.That(pathologySystem.HavePathology(human, TaintedFleshProto), Is.False, "The poisoning leaves with the zombie arm");
        });

        await pair.CleanReturnAsync();
    }

    [TestCase("Heart")]
    [TestCase("ArmLeft")]
    public async Task InfectedBodyOrganInfectsTest(string category)
    {
        await using var pair = await PoolManager.GetServerClient();
        var mapData = await pair.CreateTestMap();
        var server = pair.Server;
        var entMng = server.ResolveDependency<IEntityManager>();
        var bodySystem = entMng.System<BodySystem>();
        var pathologySystem = entMng.System<SharedPathologySystem>();

        await server.WaitAssertion(() =>
        {
            var infected = entMng.SpawnEntity("MobHuman", mapData.MapCoords);
            var human = entMng.SpawnEntity("MobHuman", mapData.MapCoords);
            entMng.EnsureComponent<PendingZombieComponent>(infected);

            Assert.That(bodySystem.TryExtractOrgan(infected, category, out var organ), Is.True);
            Assert.That(bodySystem.TryExtractOrgan(human, category, out var own), Is.True);
            entMng.DeleteEntity(own!.Value);

            Assert.That(bodySystem.TryInsertOrgan(human, organ!.Value), Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(entMng.HasComponent<PendingZombieComponent>(human), Is.True, "An organ of an infected body carries the virus");
                Assert.That(pathologySystem.HavePathology(human, TaintedFleshProto), Is.True);
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task RottenHeartFailsWithoutPoisoningTest()
    {
        await using var pair = await PoolManager.GetServerClient();
        var mapData = await pair.CreateTestMap();
        var server = pair.Server;
        var entMng = server.ResolveDependency<IEntityManager>();
        var bodySystem = entMng.System<BodySystem>();
        var pathologySystem = entMng.System<SharedPathologySystem>();

        await server.WaitAssertion(() =>
        {
            var corpse = entMng.SpawnEntity("MobHuman", mapData.MapCoords);
            var human = entMng.SpawnEntity("MobHuman", mapData.MapCoords);
            entMng.EnsureComponent<RottingComponent>(corpse);

            Assert.That(bodySystem.TryExtractOrgan(corpse, HeartCategory, out var heart), Is.True);
            Assert.That(bodySystem.TryExtractOrgan(human, HeartCategory, out var own), Is.True);
            entMng.DeleteEntity(own!.Value);

            Assert.That(bodySystem.TryInsertOrgan(human, heart!.Value), Is.True);
            // a rotten inner organ fails by its own pathologies
            Assert.That(pathologySystem.HavePathology(human, TaintedFleshProto), Is.False);
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task RottenPartsPoisonOnceTest()
    {
        // pathologies tick only while the game runs, an empty server is paused
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = true,
            DummyTicker = false
        });
        var mapData = await pair.CreateTestMap();
        var server = pair.Server;
        var entMng = server.ResolveDependency<IEntityManager>();
        var bodySystem = entMng.System<BodySystem>();
        var pathologySystem = entMng.System<SharedPathologySystem>();
        var damageableSystem = entMng.System<DamageableSystem>();

        EntityUid human = default!;
        await server.WaitAssertion(() =>
        {
            var corpse = entMng.SpawnEntity("MobHuman", mapData.MapCoords);
            human = entMng.SpawnEntity("MobHuman", mapData.MapCoords);
            entMng.EnsureComponent<RottingComponent>(corpse);

            foreach (var category in new[] { ArmLeftCategory, LegLeftCategory })
            {
                Assert.That(bodySystem.TryExtractOrgan(corpse, category, out var rotten), Is.True);
                Assert.That(bodySystem.TryExtractOrgan(human, category, out var own), Is.True);
                entMng.DeleteEntity(own!.Value);
                Assert.That(bodySystem.TryInsertOrgan(human, rotten!.Value), Is.True);
            }

            Assert.Multiple(() =>
            {
                Assert.That(pathologySystem.HavePathology(human, TaintedFleshProto), Is.True);
                Assert.That(entMng.HasComponent<PendingZombieComponent>(human), Is.False, "A rotten part doesn't infect");
            });

            damageableSystem.ClearAllDamage(human);
        });

        const float seconds = 10f;
        await pair.RunSeconds(seconds);

        await server.WaitAssertion(() =>
        {
            var poison = damageableSystem.GetAllDamage(human).DamageDict.TryGetValue(PoisonDamage, out var amount) ? amount : FixedPoint2.Zero;
            // an arm, its hand and a leg poison as much as one part
            Assert.That(poison.Float(), Is.InRange(seconds - 2f, seconds + 2f));

            Assert.That(bodySystem.TryExtractOrgan(human, ArmLeftCategory, out _), Is.True);
            Assert.That(pathologySystem.HavePathology(human, TaintedFleshProto), Is.True, "The rotten leg still poisons");
            Assert.That(bodySystem.TryExtractOrgan(human, LegLeftCategory, out _), Is.True);
            Assert.That(pathologySystem.HavePathology(human, TaintedFleshProto), Is.False);
        });

        await pair.CleanReturnAsync();
    }
}
