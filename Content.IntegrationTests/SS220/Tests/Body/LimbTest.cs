// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Body;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.Movement.Components;
using Content.Shared.SS220.Body;
using Content.Shared.SS220.Pathology;
using Content.Shared.SS220.Surgery.Graph.Requirements;
using Content.Shared.Stunnable;
using Content.Shared.Whitelist;
using Robust.Client.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.SS220.Tests.Body;

[TestFixture]
public sealed class LimbTest
{
    private static readonly ProtoId<OrganCategoryPrototype> ArmLeftCategory = "ArmLeft";
    private static readonly ProtoId<DamageTypePrototype> BluntDamage = "Blunt";
    private static readonly ProtoId<OrganCategoryPrototype> HandLeftCategory = "HandLeft";
    private static readonly ProtoId<OrganCategoryPrototype> LegLeftCategory = "LegLeft";
    private static readonly ProtoId<OrganCategoryPrototype> LegRightCategory = "LegRight";
    private static readonly ProtoId<OrganCategoryPrototype> FootLeftCategory = "FootLeft";
    private static readonly ProtoId<OrganCategoryPrototype> FootRightCategory = "FootRight";
    private static readonly ProtoId<PathologyPrototype> ArmDislocationProto = "LeftArmDislocation";
    private static readonly ProtoId<PathologyPrototype> HandDislocationProto = "LeftHandDislocation";
    private static readonly ProtoId<PathologyPrototype> ArmFractureProto = "LeftArmHairlineFracture";
    private static readonly ProtoId<PathologyPrototype> MissingArmProto = "MissingArmLeft";
    private static readonly ProtoId<PathologyPrototype> MissingHandProto = "MissingHandLeft";
    private static readonly ProtoId<PathologyPrototype> MissingLegProto = "MissingLegLeft";
    private static readonly ProtoId<PathologyPrototype> MissingFootProto = "MissingFootLeft";
    private static readonly ProtoId<OrganCategoryPrototype> HeadCategory = "Head";
    private static readonly ProtoId<OrganCategoryPrototype> BrainCategory = "Brain";
    private static readonly ProtoId<OrganCategoryPrototype> EyesCategory = "Eyes";
    private static readonly ProtoId<OrganCategoryPrototype> TongueCategory = "Tongue";
    private static readonly ProtoId<OrganCategoryPrototype> EarsCategory = "Ears";
    private static readonly ProtoId<PathologyPrototype> MissingHeadProto = "MissingHead";
    private static readonly ProtoId<PathologyPrototype> CranialFissureProto = "HeadCranialFissure";

    [Test]
    public async Task SeveredArmTakesHandTest()
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
        var handsSystem = entMng.System<SharedHandsSystem>();
        var containerSystem = entMng.System<SharedContainerSystem>();

        var handWhitelistRequirement = new HasOrganRequirement
        {
            Category = ArmLeftCategory,
            Whitelist = new EntityWhitelist { Components = ["HandOrgan"] },
        };
        var limbWhitelistRequirement = new HasOrganRequirement
        {
            Category = ArmLeftCategory,
            Whitelist = new EntityWhitelist { Components = ["Limb"] },
        };

        await server.WaitAssertion(() =>
        {
            var human = entMng.SpawnEntity("MobHuman", mapData.MapCoords);

            Assert.That(pathologySystem.TryAddPathology(human, ArmDislocationProto), Is.True);
            Assert.That(pathologySystem.TryAddPathology(human, HandDislocationProto), Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(handsSystem.GetHandCount(human), Is.EqualTo(2));
                Assert.That(limbWhitelistRequirement.SatisfiesRequirements(human, human, null, entMng), Is.True);
                Assert.That(handWhitelistRequirement.SatisfiesRequirements(human, human, null, entMng), Is.False);
            });

            Assert.That(bodySystem.TryExtractOrgan(human, ArmLeftCategory, out var arm), Is.True);
            Assert.That(containerSystem.TryGetContainer(arm!.Value, LimbComponent.ContainerId, out var attached), Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(bodySystem.HasOrganCategory(human, HandLeftCategory), Is.False, "The hand must leave the body with its arm");
                Assert.That(attached!.ContainedEntities, Has.Count.EqualTo(1));
                Assert.That(bodySystem.IsOrganOfCategory(attached.ContainedEntities[0], HandLeftCategory), Is.True);
                Assert.That(handsSystem.GetHandCount(human), Is.EqualTo(1));
                Assert.That(pathologySystem.HavePathology(human, MissingArmProto), Is.True);
                Assert.That(pathologySystem.HavePathology(human, MissingHandProto), Is.True);
                Assert.That(pathologySystem.HavePathology(human, ArmDislocationProto), Is.False);
                Assert.That(pathologySystem.HavePathology(human, HandDislocationProto), Is.False);
                Assert.That(pathologySystem.TryAddPathology(human, ArmFractureProto), Is.False, "A missing arm can't be fractured");
                Assert.That(limbWhitelistRequirement.SatisfiesRequirements(human, human, null, entMng), Is.False);
                Assert.That(limbWhitelistRequirement.SatisfiesRequirements(arm.Value, human, null, entMng), Is.True);
            });

            Assert.That(bodySystem.TryInsertOrgan(human, arm.Value.Owner), Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(bodySystem.HasOrganCategory(human, HandLeftCategory), Is.True, "The attached arm must bring its hand back");
                Assert.That(attached.ContainedEntities, Is.Empty);
                Assert.That(handsSystem.GetHandCount(human), Is.EqualTo(2));
                Assert.That(pathologySystem.HavePathology(human, MissingArmProto), Is.False);
                Assert.That(pathologySystem.HavePathology(human, MissingHandProto), Is.False);
                Assert.That(pathologySystem.HavePathology(human, ArmDislocationProto), Is.True);
                Assert.That(pathologySystem.HavePathology(human, HandDislocationProto), Is.True);
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task LegLossSlowsAndMakesCrawlTest()
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

        EntityUid human = default!;
        EntityUid rightLeg = default!;

        await server.WaitAssertion(() =>
        {
            human = entMng.SpawnEntity("MobHuman", mapData.MapCoords);

            Assert.That(bodySystem.TryExtractOrgan(human, FootLeftCategory, out _), Is.True);
            Assert.That(pathologySystem.HavePathology(human, MissingFootProto), Is.True);
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            var movement = entMng.GetComponent<MovementSpeedModifierComponent>(human);
            Assert.That(movement.SprintSpeedModifier, Is.EqualTo(0.85f).Within(0.01f));

            Assert.That(bodySystem.TryExtractOrgan(human, LegLeftCategory, out _), Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(pathologySystem.HavePathology(human, MissingLegProto), Is.True);
                Assert.That(pathologySystem.HavePathology(human, MissingFootProto), Is.True, "The doll still shows the foot as gone");
                Assert.That(entMng.HasComponent<WormComponent>(human), Is.False, "One leg is enough to stand");
            });
        });

        // a removed status effect leaves in the next ticks
        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            var movement = entMng.GetComponent<MovementSpeedModifierComponent>(human);
            Assert.That(movement.SprintSpeedModifier, Is.EqualTo(0.6f).Within(0.01f), "A missing leg replaces the slowdown of its missing foot");

            Assert.That(bodySystem.TryExtractOrgan(human, LegRightCategory, out var leg), Is.True);
            rightLeg = leg!.Value.Owner;
            Assert.Multiple(() =>
            {
                Assert.That(bodySystem.HasOrganCategory(human, FootRightCategory), Is.False);
                Assert.That(entMng.HasComponent<WormComponent>(human), Is.True, "Without legs the body only crawls");
                Assert.That(entMng.HasComponent<KnockedDownComponent>(human), Is.True);
            });

            Assert.That(bodySystem.TryInsertOrgan(human, rightLeg), Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(bodySystem.HasOrganCategory(human, FootRightCategory), Is.True);
                Assert.That(entMng.HasComponent<WormComponent>(human), Is.False);
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task SeveredHeadTakesBrainAndMindTest()
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
        var mindSystem = entMng.System<SharedMindSystem>();
        var inventorySystem = entMng.System<InventorySystem>();
        var containerSystem = entMng.System<SharedContainerSystem>();

        await server.WaitAssertion(() =>
        {
            var human = entMng.SpawnEntity("MobHuman", mapData.MapCoords);
            var dresser = entMng.SpawnEntity("MobHuman", mapData.MapCoords);
            var hat = entMng.SpawnEntity("ClothingHeadHatBeret", mapData.MapCoords);
            var mask = entMng.SpawnEntity("ClothingMaskGas", mapData.MapCoords);
            var mindId = mindSystem.CreateMind(null);
            mindSystem.TransferTo(mindId, human);

            Assert.That(inventorySystem.TryEquip(dresser, human, hat, "head", silent: true), Is.True);
            Assert.That(inventorySystem.TryEquip(dresser, human, mask, "mask", silent: true), Is.True);
            Assert.That(pathologySystem.TryAddPathology(human, CranialFissureProto), Is.True);
            Assert.That(bodySystem.TryGetOrganByCategory(human, BrainCategory, out var brain), Is.True);

            Assert.That(bodySystem.TryExtractOrgan(human, HeadCategory, out var head), Is.True);
            Assert.That(containerSystem.TryGetContainer(head!.Value, LimbComponent.ContainerId, out var attached), Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(attached!.ContainedEntities, Does.Contain(brain!.Value.Owner), "The brain leaves with the head");
                Assert.That(attached.ContainedEntities, Has.Count.EqualTo(4));
                Assert.That(bodySystem.HasOrganCategory(human, EyesCategory), Is.False);
                Assert.That(bodySystem.HasOrganCategory(human, TongueCategory), Is.False);
                Assert.That(bodySystem.HasOrganCategory(human, EarsCategory), Is.False);
                Assert.That(entMng.GetComponent<MindComponent>(mindId).OwnedEntity, Is.EqualTo(brain.Value.Owner), "The person goes with the brain");
                Assert.That(pathologySystem.HavePathology(human, MissingHeadProto), Is.True);
                Assert.That(pathologySystem.HavePathology(human, CranialFissureProto), Is.False, "The fissure stays in the severed head");
                Assert.That(inventorySystem.TryGetSlotEntity(human, "head", out _), Is.False, "Nothing to wear a hat on");
                Assert.That(inventorySystem.TryGetSlotEntity(human, "mask", out _), Is.False, "Nothing to wear a mask on");
                Assert.That(inventorySystem.TryEquip(dresser, human, hat, "head", silent: true), Is.False);
            });

            Assert.That(bodySystem.TryInsertOrgan(human, head.Value.Owner), Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(bodySystem.HasOrganCategory(human, BrainCategory), Is.True);
                Assert.That(bodySystem.HasOrganCategory(human, EyesCategory), Is.True);
                Assert.That(attached.ContainedEntities, Is.Empty);
                Assert.That(entMng.GetComponent<MindComponent>(mindId).OwnedEntity, Is.EqualTo(human), "The attached head brings the person back");
                Assert.That(pathologySystem.HavePathology(human, MissingHeadProto), Is.False);
                Assert.That(pathologySystem.HavePathology(human, CranialFissureProto), Is.True);
                Assert.That(inventorySystem.TryEquip(dresser, human, hat, "head", silent: true), Is.True);
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task MissingLimbHidesDamageOverlayTest()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = true,
            DummyTicker = false
        });

        var mapData = await pair.CreateTestMap();
        var server = pair.Server;
        var entMng = server.ResolveDependency<IEntityManager>();
        var clientEntMng = pair.Client.ResolveDependency<IEntityManager>();
        var protoManager = server.ResolveDependency<IPrototypeManager>();
        var bodySystem = entMng.System<BodySystem>();
        var damageableSystem = entMng.System<DamageableSystem>();
        var hit = new DamageSpecifier(protoManager.Index(BluntDamage), 30);

        EntityUid human = default!;
        EntityUid arm = default!;
        await server.WaitAssertion(() =>
        {
            human = entMng.SpawnEntity("MobHuman", mapData.MapCoords);
            damageableSystem.TryChangeDamage(human, hit, ignoreResistances: true);
            Assert.That(bodySystem.TryExtractOrgan(human, ArmLeftCategory, out var extracted), Is.True);
            arm = extracted!.Value;
            // the damage after the loss must not bring the overlay back either
            damageableSystem.TryChangeDamage(human, hit, ignoreResistances: true);
        });

        await pair.RunTicksSync(10);

        await pair.Client.WaitAssertion(() =>
        {
            var clientHuman = clientEntMng.GetEntity(entMng.GetNetEntity(human));
            var spriteSystem = clientEntMng.System<SpriteSystem>();
            var sprite = clientEntMng.GetComponent<SpriteComponent>(clientHuman);
            Assert.That(spriteSystem.LayerMapTryGet((clientHuman, sprite), "LArmBrute", out var armLayer, false), Is.True);
            Assert.That(spriteSystem.LayerMapTryGet((clientHuman, sprite), "ChestBrute", out var chest, false), Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(sprite[armLayer].Visible, Is.False, "A missing arm shows no bruises");
                Assert.That(sprite[chest].Visible, Is.True);
            });
        });

        // healed while the arm was missing: the returned arm must not show the bruises of before
        await server.WaitAssertion(() => damageableSystem.ClearAllDamage(human));
        await pair.RunTicksSync(10);
        await server.WaitAssertion(() => Assert.That(bodySystem.TryInsertOrgan(human, arm), Is.True));

        await pair.RunTicksSync(10);

        await pair.Client.WaitAssertion(() =>
        {
            var clientHuman = clientEntMng.GetEntity(entMng.GetNetEntity(human));
            var spriteSystem = clientEntMng.System<SpriteSystem>();
            var sprite = clientEntMng.GetComponent<SpriteComponent>(clientHuman);
            spriteSystem.LayerMapTryGet((clientHuman, sprite), "LArmBrute", out var armLayer, false);
            spriteSystem.LayerMapTryGet((clientHuman, sprite), "ChestBrute", out var chest, false);
            Assert.Multiple(() =>
            {
                Assert.That(sprite[armLayer].Visible, Is.False, "A returned arm of a healed body shows no bruises");
                Assert.That(sprite[chest].Visible, Is.False);
            });
        });

        // the returned arm gets damage updates again
        await server.WaitAssertion(() => damageableSystem.TryChangeDamage(human, hit, ignoreResistances: true));

        await pair.RunTicksSync(10);

        await pair.Client.WaitAssertion(() =>
        {
            var clientHuman = clientEntMng.GetEntity(entMng.GetNetEntity(human));
            var spriteSystem = clientEntMng.System<SpriteSystem>();
            var sprite = clientEntMng.GetComponent<SpriteComponent>(clientHuman);
            spriteSystem.LayerMapTryGet((clientHuman, sprite), "LArmBrute", out var armLayer, false);
            Assert.That(sprite[armLayer].Visible, Is.True, "A returned arm shows new bruises");
        });

        await pair.CleanReturnAsync();
    }
}
