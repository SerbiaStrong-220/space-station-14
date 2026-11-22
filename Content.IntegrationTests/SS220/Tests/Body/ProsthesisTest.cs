// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Gravity;
using Content.Shared.Body;
using Content.Shared.Emp;
using Content.Shared.Gravity;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.SS220.Body;
using Content.Shared.SS220.Pathology;
using Content.Shared.Stunnable;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.SS220.Tests.Body;

[TestFixture]
public sealed class ProsthesisTest
{
    private static readonly ProtoId<OrganCategoryPrototype> ArmLeftCategory = "ArmLeft";
    private static readonly ProtoId<OrganCategoryPrototype> HandLeftCategory = "HandLeft";
    private static readonly ProtoId<OrganCategoryPrototype> LegLeftCategory = "LegLeft";
    private static readonly ProtoId<PathologyPrototype> ArmFractureProto = "LeftArmHairlineFracture";

    [Test]
    public async Task WoodenArmTest()
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
        var handsSystem = entMng.System<SharedHandsSystem>();
        var pathologySystem = entMng.System<SharedPathologySystem>();

        await server.WaitAssertion(() =>
        {
            var human = entMng.SpawnEntity("MobHuman", mapData.MapCoords);
            var woodenArm = entMng.SpawnEntity("ProsthesisWoodenArmLeft", mapData.MapCoords);
            var gun = entMng.SpawnEntity("WeaponPistolMk58", mapData.MapCoords);
            var crowbar = entMng.SpawnEntity("Crowbar", mapData.MapCoords);

            Assert.That(bodySystem.TryExtractOrgan(human, ArmLeftCategory, out _), Is.True);
            Assert.That(bodySystem.TryInsertOrgan(human, woodenArm), Is.True);
            Assert.That(bodySystem.TryGetOrganByCategory(human, HandLeftCategory, out var hand), Is.True, "The wooden arm comes with its hand");
            Assert.Multiple(() =>
            {
                Assert.That(entMng.HasComponent<ProsthesisComponent>(hand!.Value), Is.True);
                Assert.That(handsSystem.GetHandCount(human), Is.EqualTo(2));
                Assert.That(handsSystem.TryPickup(human, gun, "left", checkActionBlocker: false), Is.False, "A hook can't hold a gun");
                Assert.That(handsSystem.TryPickup(human, crowbar, "left", checkActionBlocker: false), Is.True);
                Assert.That(pathologySystem.TryAddPathology(human, ArmFractureProto), Is.False, "Wood doesn't fracture like a bone");
            });
        });

        await pair.CleanReturnAsync();
    }

    [TestCase("Cyber", true)]
    [TestCase("AdvancedCyber", false)]
    public async Task CyberProsthesisEmpTest(string kind, bool vulnerable)
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
        var handsSystem = entMng.System<SharedHandsSystem>();
        var empSystem = entMng.System<SharedEmpSystem>();

        EntityUid human = default!;
        EntityUid crowbar = default!;

        // nobody falls down while weightless
        await server.WaitPost(() =>
        {
            var gravity = entMng.EnsureComponent<GravityComponent>(mapData.Grid);
            entMng.System<GravitySystem>().EnableGravity(mapData.Grid, gravity);
        });

        await server.WaitAssertion(() =>
        {
            human = entMng.SpawnEntity("MobHuman", mapData.GridCoords);
            var arm = entMng.SpawnEntity($"Prosthesis{kind}ArmLeft", mapData.MapCoords);
            var leg = entMng.SpawnEntity($"Prosthesis{kind}LegLeft", mapData.MapCoords);
            crowbar = entMng.SpawnEntity("Crowbar", mapData.MapCoords);

            Assert.That(bodySystem.TryExtractOrgan(human, ArmLeftCategory, out _), Is.True);
            Assert.That(bodySystem.TryExtractOrgan(human, LegLeftCategory, out _), Is.True);
            Assert.That(bodySystem.TryInsertOrgan(human, arm), Is.True);
            Assert.That(bodySystem.TryInsertOrgan(human, leg), Is.True);
            Assert.That(handsSystem.TryPickup(human, crowbar, "left", checkActionBlocker: false), Is.True);
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            empSystem.TryEmpEffects(human, 1000f, TimeSpan.FromSeconds(5));

            var held = handsSystem.GetHeldItem(human, "left");
            Assert.Multiple(() =>
            {
                Assert.That(held == crowbar, Is.EqualTo(!vulnerable), "The EMP makes the cybernetic hand drop its item");
                Assert.That(held is { } item && entMng.HasComponent<VirtualItemComponent>(item), Is.EqualTo(vulnerable), "The jammed hand stays busy");
                Assert.That(entMng.HasComponent<KnockedDownComponent>(human), Is.EqualTo(vulnerable), "The EMP knocks a cybernetic leg down");
            });

            if (vulnerable)
                Assert.That(handsSystem.TryDrop(human, "left"), Is.False, "The jammed hand can't be freed by dropping");
        });

        if (!vulnerable)
        {
            await pair.CleanReturnAsync();
            return;
        }

        await pair.RunSeconds(11);

        await server.WaitAssertion(() =>
        {
            Assert.That(handsSystem.GetHeldItem(human, "left"), Is.Null, "The hand works again after the jam");
        });

        await pair.CleanReturnAsync();
    }
}
