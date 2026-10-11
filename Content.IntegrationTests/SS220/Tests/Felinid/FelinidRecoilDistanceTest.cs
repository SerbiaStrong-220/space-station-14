using System.Numerics;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Standing;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Wieldable.Components;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.SS220.Tests.Felinid;

/// <summary>
/// Fires real weapons from a felinid standing on a gravity grid and measures how far recoil pushes them.
/// The felinid shoots along the grid's +X axis, so recoil must push them along -X by the weapon's
/// <c>felinidRecoil</c> tiles. Positions are grid-local because the test grid is not axis-aligned.
/// </summary>
public sealed class FelinidRecoilDistanceTest : InteractionTest
{
    private const float MinimumPushTiles = 0.25f;

    protected override string PlayerPrototype => "MobFelinid";

    [TestCase("WeaponPistolMk58", 0.25f)]
    [TestCase("WeaponRevolverDeckard", 0.3f)]
    [TestCase("WeaponSubMachineGunC20r", 0.3f)]
    [TestCase("WeaponRifleLecter", 0.45f)]
    [TestCase("WeaponRifleAk", 0.55f)]
    [TestCase("WeaponShotgunBulldog", 0.55f)]
    [TestCase("WeaponSniperMosin", 0.7f)]
    [TestCase("WeaponLightMachineGunL6", 0.75f)]
    [TestCase("WeaponMinigun", 1.1f)]
    public async Task RecoilPushesFelinidAwayFromTheShot(string weapon, float expectedTiles)
    {
        var delta = await FireOnceAndMeasure(weapon);

        Assert.Multiple(() =>
        {
            Assert.That(delta.X, Is.Negative, "Recoil must push the felinid opposite to the shot.");
            Assert.That(MathF.Abs(delta.Y), Is.LessThan(0.05f), "Recoil must not push sideways.");
            Assert.That(delta.Length(), Is.InRange(expectedTiles * 0.75f, expectedTiles * 1.25f));
            Assert.That(delta.Length(), Is.GreaterThanOrEqualTo(MinimumPushTiles), "Even a pistol must push this far.");
        });
    }

    [Test]
    public async Task LaserDoesNotPushFelinid()
    {
        var delta = await FireOnceAndMeasure("WeaponLaserCarbine");

        Assert.That(delta.Length(), Is.LessThan(0.01f));
    }

    private async Task<Vector2> FireOnceAndMeasure(string weapon)
    {
        await AddGravity();
        await AddAtmosphere();

        // Knockdown is a separate random roll (see FelinidHeavyGunRecoilTest) that lengthens the slide.
        // Without a standing state the felinid can't be knocked down, so only the push itself is measured.
        await Server.WaitPost(() => SEntMan.RemoveComponent<StandingStateComponent>(SPlayer));

        // The test grid is a single tile; extend it behind the felinid so they land on the gravity grid.
        for (var x = -3; x < 0; x++)
        {
            var floor = SEntMan.GetNetCoordinates(MapData.GridCoords.Offset(new Vector2(x + 0.5f, 0.5f)));
            await SetTile("Plating", floor, MapData.Grid);
        }

        var gun = await PlaceInHands(weapon);
        if (HasComp<WieldableComponent>(gun))
            await UseInHand();

        // Magazine-fed guns spawn with an open bolt and an empty chamber; rack them like a player would.
        await Server.WaitPost(() =>
        {
            if (SEntMan.TryGetComponent(ToServer(gun), out ChamberMagazineAmmoProviderComponent chamber))
                SGun.SetBoltClosed(ToServer(gun), chamber, true);
        });

        // Guns have a cooldown after being picked up.
        await RunSeconds(2f);

        var start = Vector2.Zero;
        await Server.WaitPost(() => start = SEntMan.GetComponent<TransformComponent>(SPlayer).LocalPosition);

        await AttemptShoot(SEntMan.GetNetCoordinates(MapData.GridCoords.Offset(new Vector2(10.5f, 0.5f))));
        await RunSeconds(2f);

        var end = Vector2.Zero;
        await Server.WaitPost(() => end = SEntMan.GetComponent<TransformComponent>(SPlayer).LocalPosition);

        var delta = end - start;
        TestContext.Out.WriteLine($"{weapon}: pushed {delta.Length():F2} tiles, delta ({delta.X:F2}, {delta.Y:F2})");
        return delta;
    }
}
