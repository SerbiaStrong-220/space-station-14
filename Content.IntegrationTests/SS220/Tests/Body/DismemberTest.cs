// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using Content.IntegrationTests.Pair;
using Content.Shared.Body;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Explosion;
using Content.Shared.SS220.Pathology;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.SS220.Tests.Body;

[TestFixture]
public sealed class DismemberTest
{
    private static readonly ProtoId<OrganCategoryPrototype> ArmLeftCategory = "ArmLeft";
    private static readonly ProtoId<OrganCategoryPrototype> HeadCategory = "Head";
    private static readonly ProtoId<OrganCategoryPrototype>[] Limbs = ["ArmLeft", "ArmRight", "LegLeft", "LegRight"];
    private static readonly ProtoId<PathologyPrototype> ArmCompoundFractureProto = "LeftArmCompoundFracture";
    private static readonly ProtoId<DamageTypePrototype> BluntDamage = "Blunt";

    // a roll per hit lands on the arm in about one case out of eight, so this many hits tear it off for sure
    private const int Hits = 300;

    /// <summary>
    /// One hit per tick: after a part is torn off, the rest of the tick rolls no other tear.
    /// </summary>
    private static async Task HitUntil(TestPair pair, EntityUid target, DamageSpecifier hit, int hits, Func<bool> done)
    {
        var damageSystem = pair.Server.ResolveDependency<IEntityManager>().System<DamageableSystem>();
        for (var i = 0; i < hits; i++)
        {
            var finished = false;
            await pair.Server.WaitPost(() =>
            {
                finished = done();
                if (finished)
                    return;

                // hundreds of hits would gib the body and take every part with it
                damageSystem.ClearAllDamage(target);
                damageSystem.TryChangeDamage(target, hit, ignoreResistances: true);
            });

            if (finished)
                return;

            await pair.RunTicksSync(1);
        }
    }

    [TestCase("Slash", true)]
    [TestCase("Blunt", true)]
    [TestCase("Piercing", false)]
    public async Task HitTearsOffTraumatizedArmTest(string damageType, bool tears)
    {
        await using var pair = await PoolManager.GetServerClient();
        var mapData = await pair.CreateTestMap();
        var server = pair.Server;
        var entMng = server.ResolveDependency<IEntityManager>();
        var protoManager = server.ResolveDependency<IPrototypeManager>();
        var bodySystem = entMng.System<BodySystem>();
        var pathologySystem = entMng.System<SharedPathologySystem>();
        var containerSystem = entMng.System<SharedContainerSystem>();

        EntityUid human = default!;
        EntityUid arm = default!;

        await server.WaitAssertion(() =>
        {
            human = entMng.SpawnEntity("MobHuman", mapData.GridCoords);
            Assert.That(pathologySystem.TryAddPathology(human, ArmCompoundFractureProto), Is.True);
            Assert.That(bodySystem.TryGetOrganByCategory(human, ArmLeftCategory, out var organ), Is.True);
            arm = organ!.Value.Owner;
        });

        var hit = new DamageSpecifier(protoManager.Index<DamageTypePrototype>(damageType), 40);
        await HitUntil(pair, human, hit, Hits, () => !bodySystem.HasOrganCategory(human, ArmLeftCategory));

        await server.WaitAssertion(() =>
        {
            if (!tears)
            {
                Assert.That(bodySystem.HasOrganCategory(human, ArmLeftCategory), Is.True, "Stabs don't tear limbs off");
                return;
            }

            Assert.Multiple(() =>
            {
                Assert.That(bodySystem.HasOrganCategory(human, ArmLeftCategory), Is.False, "A fractured arm is torn off by strong hits");
                Assert.That(containerSystem.IsEntityInContainer(arm), Is.False, "The torn arm falls to the floor");
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task ExplosionTearsOffLimbsTest()
    {
        await using var pair = await PoolManager.GetServerClient();
        var mapData = await pair.CreateTestMap();
        var server = pair.Server;
        var entMng = server.ResolveDependency<IEntityManager>();
        var protoManager = server.ResolveDependency<IPrototypeManager>();
        var bodySystem = entMng.System<BodySystem>();

        await server.WaitAssertion(() =>
        {
            var human = entMng.SpawnEntity("MobHuman", mapData.GridCoords);
            var weak = new BeforeExplodeEvent(new DamageSpecifier(protoManager.Index(BluntDamage), 15), "Default", []);
            for (var i = 0; i < 50; i++)
            {
                entMng.EventBus.RaiseLocalEvent(human, ref weak);
            }

            Assert.That(Limbs.All(x => bodySystem.HasOrganCategory(human, x)), Is.True, "A weak blast doesn't tear limbs off");

            var heavy = new BeforeExplodeEvent(new DamageSpecifier(protoManager.Index(BluntDamage), 100), "Default", []);
            for (var i = 0; i < 50; i++)
            {
                entMng.EventBus.RaiseLocalEvent(human, ref heavy);
            }

            Assert.Multiple(() =>
            {
                Assert.That(Limbs.Any(x => bodySystem.HasOrganCategory(human, x)), Is.False, "A heavy blast tears limbs off");
                Assert.That(bodySystem.HasOrganCategory(human, HeadCategory), Is.True, "A blast never tears the head off");
            });
        });

        await pair.CleanReturnAsync();
    }

}
