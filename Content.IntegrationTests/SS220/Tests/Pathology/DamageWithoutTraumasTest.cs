// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.SS220.Pathology;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.SS220.Tests.Pathology;

[TestFixture]
public sealed class DamageWithoutTraumasTest
{
    private static readonly ProtoId<DamageTypePrototype> Blunt = "Blunt";
    private const int Hits = 10;

    [Test]
    public async Task SurgeryDamageRollsNoTraumasTest()
    {
        await using var pair = await PoolManager.GetServerClient();

        var server = pair.Server;
        var entMng = server.ResolveDependency<IEntityManager>();
        var pathologySystem = entMng.System<SharedPathologySystem>();
        var damageableSystem = entMng.System<DamageableSystem>();

        await server.WaitAssertion(() =>
        {
            var human = entMng.SpawnEntity("MobHuman", MapCoordinates.Nullspace);
            var holder = entMng.GetComponent<PathologyHolderComponent>(human);
            // a compound fracture has 90% per hit of this damage
            var damage = new DamageSpecifier { DamageDict = { [Blunt] = 60 } };

            for (var i = 0; i < Hits; i++)
            {
                pathologySystem.TryChangeDamageWithoutTraumas(human, damage, out _);
                damageableSystem.ClearAllDamage(human);
            }

            Assert.That(holder.ActivePathologies, Is.Empty, "damage of a surgery step rolled a trauma");
            Assert.That(holder.DamageWithoutTraumas, Is.False);

            for (var i = 0; i < Hits; i++)
            {
                damageableSystem.TryChangeDamage(human, damage);
                damageableSystem.ClearAllDamage(human);
            }

            Assert.That(holder.ActivePathologies, Is.Not.Empty, "an ordinary hit rolled no trauma");
        });

        await pair.CleanReturnAsync();
    }
}
