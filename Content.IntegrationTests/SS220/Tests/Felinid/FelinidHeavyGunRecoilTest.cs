using System.Numerics;
using Content.IntegrationTests.Fixtures;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.SS220.Felinid.Components;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.GameObjects;

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

- type: entity
  id: FelinidRecoilTestGun
  components:
  - type: Item
  - type: Gun
    felinidRecoil: 1.8
    felinidRecoilKnockdownChance: 1
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
            };
            entMan.EventBus.RaiseLocalEvent(mob, ref impulse);

            Assert.Multiple(() =>
            {
                Assert.That(impulse.Push, Is.False, "Ground recoil throws the felinid instead of the weightless push.");
                Assert.That(entMan.HasComponent<KnockedDownComponent>(mob), Is.True);
            });
        });
    }
}
