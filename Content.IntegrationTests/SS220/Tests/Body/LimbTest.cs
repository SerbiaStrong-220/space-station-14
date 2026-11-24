// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Body;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Mind;
using Content.Shared.SS220.Body;
using Content.Shared.SS220.Pathology;
using Content.Shared.SS220.Surgery.Graph.Requirements;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.SS220.Tests.Body;

[TestFixture]
public sealed class LimbTest
{
    private static readonly ProtoId<OrganCategoryPrototype> ArmLeftCategory = "ArmLeft";
    private static readonly ProtoId<OrganCategoryPrototype> HandLeftCategory = "HandLeft";
    private static readonly ProtoId<PathologyPrototype> ArmDislocationProto = "LeftArmDislocation";
    private static readonly ProtoId<PathologyPrototype> HandDislocationProto = "LeftHandDislocation";
    private static readonly ProtoId<PathologyPrototype> ArmFractureProto = "LeftArmHairlineFracture";
    private static readonly ProtoId<PathologyPrototype> MissingArmProto = "MissingArmLeft";
    private static readonly ProtoId<PathologyPrototype> MissingHandProto = "MissingHandLeft";
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
}
