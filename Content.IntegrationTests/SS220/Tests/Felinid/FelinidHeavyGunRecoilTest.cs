using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.SS220.Felinid.Components;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Components;

namespace Content.IntegrationTests.SS220.Tests.Felinid;

[TestFixture]
[TestOf(typeof(GunRecoilModifierComponent))]
public sealed class FelinidHeavyGunRecoilTest : GameTest
{
    [TestPrototypes]
    private const string Prototypes = """
- type: entity
  id: FelinidRecoilTestMob
  components:
  - type: Hands
    hands:
      hand_right:
        location: Right
    sortedHands:
    - hand_right
  - type: StandingState
  - type: Crawler
  - type: GunRecoilModifier
  - type: Physics
    bodyType: KinematicController
  - type: Fixtures
    fixtures:
      fix1:
        shape: !type:PhysShapeCircle
          radius: 0.35
        density: 185

- type: entity
  id: FelinidRecoilTestGun
  components:
  - type: Item
  - type: Gun
    groundRecoil: 1.8
    groundRecoilKnockdownChance: 1
""";

    [Test]
    public async Task RecoilRollsKnockdownWithoutTheWeightlessImpulse()
    {
        var server = Pair.Server;
        var testMap = await Pair.CreateTestMap();
        var entMan = server.ResolveDependency<IEntityManager>();
        var hands = entMan.System<SharedHandsSystem>();

        await server.WaitAssertion(() =>
        {
            var mob = entMan.SpawnEntity("FelinidRecoilTestMob", testMap.GridCoords);
            var gun = entMan.SpawnEntity("FelinidRecoilTestGun", testMap.GridCoords);
            Assert.That(hands.TryPickupAnyHand(mob, gun), Is.True);

            var impulse = new ShooterImpulseEvent
            {
                From = testMap.GridCoords,
                To = testMap.GridCoords.Offset(new Vector2(5f, 0f)),
                Gun = (gun, entMan.GetComponent<GunComponent>(gun)),
            };
            entMan.EventBus.RaiseLocalEvent(mob, ref impulse);

            Assert.Multiple(() =>
            {
                Assert.That(impulse.Push, Is.False, "Ground recoil throws the felinid instead of the weightless push.");
                Assert.That(entMan.HasComponent<KnockedDownComponent>(mob), Is.True);
            });
        });
    }

    /// <summary>
    /// Automatic fire raises the impulse event every shot. While the shooter is still flying from
    /// the previous shot, friction and movement are suspended, so stacked throws would keep accelerating them.
    /// </summary>
    [Test]
    public async Task BurstFireDoesNotStackRecoil()
    {
        var server = Pair.Server;
        var testMap = await Pair.CreateTestMap();
        var entMan = server.ResolveDependency<IEntityManager>();

        await server.WaitAssertion(() =>
        {
            var mob = entMan.SpawnEntity("FelinidRecoilTestMob", testMap.GridCoords);
            var gun = entMan.SpawnEntity("FelinidRecoilTestGun", testMap.GridCoords);
            var gunComp = entMan.GetComponent<GunComponent>(gun);

            float SpeedAfterShot()
            {
                var impulse = new ShooterImpulseEvent
                {
                    From = testMap.GridCoords,
                    To = testMap.GridCoords.Offset(new Vector2(5f, 0f)),
                    Gun = (gun, gunComp),
                };
                entMan.EventBus.RaiseLocalEvent(mob, ref impulse);
                return entMan.GetComponent<PhysicsComponent>(mob).LinearVelocity.Length();
            }

            var first = SpeedAfterShot();
            var second = SpeedAfterShot();
            var third = SpeedAfterShot();

            Assert.Multiple(() =>
            {
                Assert.That(first, Is.GreaterThan(0f));
                Assert.That(second, Is.EqualTo(first).Within(0.001f));
                Assert.That(third, Is.EqualTo(first).Within(0.001f));
            });
        });
    }
}
