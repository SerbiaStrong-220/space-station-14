using Content.IntegrationTests.Fixtures;
using Content.Shared.Projectiles;
using Content.Shared.SS220.Felinid.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Events;

namespace Content.IntegrationTests.SS220.Tests.Felinid;

[TestFixture]
[TestOf(typeof(DodgeChanceComponent))]
public sealed class FelinidDodgeTest : GameTest
{
    [TestPrototypes]
    private const string Prototypes = """
- type: entity
  id: FelinidDodgeTestMob
  components:
  - type: Physics
  - type: DodgeChance
    baseDodgeChance: 0.5

- type: entity
  id: FelinidDodgeTestProjectile
  components:
  - type: Physics
  - type: Projectile
    damage: {}
""";

    /// <summary>
    /// A projectile keeps raising <see cref="PreventCollideEvent"/> every tick it overlaps the target.
    /// The dodge must be decided once per projectile, not re-rolled on each of those checks.
    /// </summary>
    [Test]
    public async Task OverlappingProjectileIsRolledOnce()
    {
        var server = Pair.Server;
        var testMap = await Pair.CreateTestMap();
        var entMan = server.ResolveDependency<IEntityManager>();

        await server.WaitAssertion(() =>
        {
            var mob = entMan.SpawnEntity("FelinidDodgeTestMob", testMap.GridCoords);
            var mobBody = entMan.GetComponent<PhysicsComponent>(mob);
            var dodged = 0;
            const int projectiles = 40;

            for (var i = 0; i < projectiles; i++)
            {
                var projectile = entMan.SpawnEntity("FelinidDodgeTestProjectile", testMap.GridCoords);
                var projectileBody = entMan.GetComponent<PhysicsComponent>(projectile);

                bool? first = null;
                for (var check = 0; check < 5; check++)
                {
                    var ev = new PreventCollideEvent(
                        mob,
                        projectile,
                        mobBody,
                        projectileBody,
                        new Fixture(),
                        new Fixture());
                    entMan.EventBus.RaiseLocalEvent(mob, ref ev);

                    first ??= ev.Cancelled;
                    Assert.That(ev.Cancelled, Is.EqualTo(first), "The same projectile must get the same dodge result.");
                }

                if (first == true)
                    dodged++;

                entMan.DeleteEntity(projectile);
            }

            // 50% per projectile: both outcomes must occur, so the roll still depends on the shot.
            Assert.That(dodged, Is.InRange(1, projectiles - 1));
        });
    }
}
