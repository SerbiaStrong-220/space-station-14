// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Content.Client.Inventory;
using Content.Shared.Administration.Systems;
using Content.Shared.Body;
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
using Robust.Shared.ContentPack;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.SS220.Tests.Body;

[TestFixture]
public sealed class LimbTest
{
    private static readonly ProtoId<OrganCategoryPrototype> ArmLeftCategory = "ArmLeft";
    private static readonly ProtoId<OrganCategoryPrototype> HandLeftCategory = "HandLeft";
    private static readonly ProtoId<OrganCategoryPrototype> HandRightCategory = "HandRight";
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
    private static readonly ProtoId<DamageTypePrototype> AsphyxiationDamage = "Asphyxiation";
    private const string GlovesSlot = "gloves";
    private const string ShoesSlot = "shoes";

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
                Assert.That(movement.SprintSpeedModifier, Is.EqualTo(0.85f * 0.6f).Within(0.01f));
                Assert.That(entMng.HasComponent<WormComponent>(human), Is.False, "One leg is enough to stand");
            });

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

    [TestCase("MobHuman", "ClothingShoesColorBlack", "gloves-left", "shoes-left")]
    // vox gloves are displaced by the species map, jackboots have their own vox sprite which isn't
    [TestCase("MobVox", "ClothingShoesBootsJack", "vox-gloves-left", "vox-shoes-left-undisplaced")]
    public async Task ClothesOnMissingLimbsTest(string species, string shoesProto, string glovesMask, string shoesMask)
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = true,
            DummyTicker = false
        });

        var mapData = await pair.CreateTestMap();
        var server = pair.Server;
        var client = pair.Client;
        var entMng = server.ResolveDependency<IEntityManager>();
        var clientEntMng = client.ResolveDependency<IEntityManager>();
        var bodySystem = entMng.System<BodySystem>();
        var inventorySystem = entMng.System<InventorySystem>();
        var containerSystem = entMng.System<SharedContainerSystem>();

        EntityUid patient = default!;
        EntityUid dresser = default!;
        EntityUid gloves = default!;
        EntityUid shoes = default!;
        EntityUid leftHand = default!;
        EntityUid leftLeg = default!;

        await server.WaitAssertion(() =>
        {
            patient = entMng.SpawnEntity(species, mapData.MapCoords);
            dresser = entMng.SpawnEntity("MobHuman", mapData.MapCoords);
            gloves = entMng.SpawnEntity("ClothingHandsGlovesColorBlack", mapData.MapCoords);
            shoes = entMng.SpawnEntity(shoesProto, mapData.MapCoords);

            Assert.That(inventorySystem.TryEquip(dresser, patient, gloves, GlovesSlot, silent: true), Is.True);
            Assert.That(inventorySystem.TryEquip(dresser, patient, shoes, ShoesSlot, silent: true), Is.True);

            Assert.That(bodySystem.TryExtractOrgan(patient, HandLeftCategory, out var hand), Is.True);
            Assert.That(bodySystem.TryExtractOrgan(patient, LegLeftCategory, out var leg), Is.True);
            leftHand = hand!.Value.Owner;
            leftLeg = leg!.Value.Owner;
            Assert.Multiple(() =>
            {
                Assert.That(inventorySystem.TryGetSlotEntity(patient, GlovesSlot, out _), Is.True, "Gloves stay on the remaining hand");
                Assert.That(inventorySystem.TryGetSlotEntity(patient, ShoesSlot, out _), Is.True, "Shoes stay on the remaining foot");
            });
        });

        await pair.RunTicksSync(10);

        await client.WaitAssertion(() =>
        {
            var clientPatient = clientEntMng.GetEntity(entMng.GetNetEntity(patient));
            Assert.Multiple(() =>
            {
                Assert.That(GetSlotMasks(clientEntMng, clientPatient, GlovesSlot), Is.Not.Empty.And.All.EqualTo(glovesMask));
                Assert.That(GetSlotMasks(clientEntMng, clientPatient, ShoesSlot), Is.Not.Empty.And.All.EqualTo(shoesMask));
            });
        });

        await server.WaitAssertion(() =>
        {
            Assert.That(bodySystem.TryExtractOrgan(patient, HandRightCategory, out var rightHand), Is.True);
            Assert.That(bodySystem.TryExtractOrgan(patient, LegRightCategory, out var rightLeg), Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(inventorySystem.TryGetSlotEntity(patient, GlovesSlot, out _), Is.False, "Gloves fall off a body without hands");
                Assert.That(inventorySystem.TryGetSlotEntity(patient, ShoesSlot, out _), Is.False, "Shoes fall off a body without feet");
                Assert.That(entMng.Deleted(gloves) || containerSystem.IsEntityInContainer(gloves), Is.False, "Fallen gloves lie on the floor");
                Assert.That(entMng.Deleted(shoes) || containerSystem.IsEntityInContainer(shoes), Is.False, "Fallen shoes lie on the floor");
                Assert.That(inventorySystem.TryEquip(dresser, patient, gloves, GlovesSlot, silent: true), Is.False);
                Assert.That(inventorySystem.TryEquip(dresser, patient, shoes, ShoesSlot, silent: true), Is.False);
            });

            Assert.That(bodySystem.TryInsertOrgan(patient, rightHand!.Value.Owner), Is.True);
            Assert.That(bodySystem.TryInsertOrgan(patient, rightLeg!.Value.Owner), Is.True);
            Assert.That(bodySystem.TryInsertOrgan(patient, leftHand), Is.True);
            Assert.That(bodySystem.TryInsertOrgan(patient, leftLeg), Is.True);
            Assert.That(inventorySystem.TryEquip(dresser, patient, gloves, GlovesSlot, silent: true), Is.True);
            Assert.That(inventorySystem.TryEquip(dresser, patient, shoes, ShoesSlot, silent: true), Is.True);
        });

        await pair.RunTicksSync(10);

        await client.WaitAssertion(() =>
        {
            var clientPatient = clientEntMng.GetEntity(entMng.GetNetEntity(patient));
            Assert.Multiple(() =>
            {
                Assert.That(GetSlotMasks(clientEntMng, clientPatient, GlovesSlot), Has.None.EqualTo(glovesMask), "Gloves on both hands are drawn whole");
                Assert.That(GetSlotMasks(clientEntMng, clientPatient, ShoesSlot), Has.None.EqualTo(shoesMask), "Shoes on both feet are drawn whole");
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
    public async Task HeadlessBodySuffocatesTest()
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
        var damageSystem = entMng.System<DamageableSystem>();

        EntityUid headless = default!;
        EntityUid control = default!;

        await server.WaitAssertion(() =>
        {
            headless = entMng.SpawnEntity("MobHuman", mapData.MapCoords);
            control = entMng.SpawnEntity("MobHuman", mapData.MapCoords);
            Assert.That(bodySystem.TryExtractOrgan(headless, HeadCategory, out _), Is.True);
        });

        await pair.RunSeconds(10);

        await server.WaitAssertion(() =>
        {
            var headlessDamage = damageSystem.GetAllDamage(headless).DamageDict.GetValueOrDefault(AsphyxiationDamage);
            var controlDamage = damageSystem.GetAllDamage(control).DamageDict.GetValueOrDefault(AsphyxiationDamage);
            // both breathe the same air, the difference comes from the missing head alone
            Assert.That((headlessDamage - controlDamage).Float(), Is.GreaterThan(30f), "A headless body must die within a minute");
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task RejuvenateKeepsBrainOfForeignHeadTest()
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
        var mindSystem = entMng.System<SharedMindSystem>();
        var rejuvenateSystem = entMng.System<RejuvenateSystem>();

        await server.WaitAssertion(() =>
        {
            var human = entMng.SpawnEntity("MobHuman", mapData.MapCoords);
            var lizardHead = entMng.SpawnEntity("OrganReptilianHead", mapData.MapCoords);
            var mindId = mindSystem.CreateMind(null);
            mindSystem.TransferTo(mindId, human);

            Assert.That(bodySystem.TryGetOrganByCategory(human, BrainCategory, out var brain), Is.True);
            Assert.That(bodySystem.TryExtractOrgan(human, HeadCategory, out _), Is.True);
            Assert.That(bodySystem.TryInsertOrgan(human, lizardHead), Is.True);
            Assert.That(bodySystem.TryInsertOrgan(human, brain!.Value.Owner), Is.True);
            Assert.That(entMng.GetComponent<MindComponent>(mindId).OwnedEntity, Is.EqualTo(human));

            rejuvenateSystem.PerformRejuvenate(human);

            Assert.Multiple(() =>
            {
                Assert.That(bodySystem.TryGetOrganByCategory(human, HeadCategory, out var head), Is.True);
                Assert.That(entMng.GetComponent<MetaDataComponent>(head!.Value).EntityPrototype?.ID, Is.EqualTo("OrganHumanHead"));
                Assert.That(entMng.Deleted(lizardHead), Is.True);
                Assert.That(entMng.Deleted(brain.Value), Is.False, "The brain of a replaced head must survive");
                Assert.That(bodySystem.TryGetOrganByCategory(human, BrainCategory, out var newBrain), Is.True);
                Assert.That(newBrain!.Value.Owner, Is.EqualTo(brain.Value.Owner));
                Assert.That(entMng.GetComponent<MindComponent>(mindId).OwnedEntity, Is.EqualTo(human));
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task LimbMasksLoadAsDisplacementMapsTest()
    {
        await using var pair = await PoolManager.GetServerClient();

        var client = pair.Client;
        var protoMan = client.ResolveDependency<IPrototypeManager>();
        var resMan = client.ResolveDependency<IResourceManager>();
        var compFactory = client.ResolveDependency<IComponentFactory>();

        await client.WaitAssertion(() =>
        {
            var checkedMasks = 0;
            foreach (var proto in protoMan.EnumeratePrototypes<EntityPrototype>())
            {
                if (!proto.TryGetComponent<OrganWornSlotsComponent>(out var worn, compFactory))
                    continue;

                foreach (var mask in worn.Masks.Values.Concat(worn.UndisplacedMasks.Values))
                {
                    foreach (var layer in mask.SizeMaps.Values)
                    {
                        var metaPath = new ResPath($"/Textures/{layer.RsiPath}/meta.json");
                        using var meta = JsonDocument.Parse(resMan.ContentFileReadAllText(metaPath));
                        var linear = meta.RootElement.TryGetProperty("load", out var load)
                            && load.TryGetProperty("srgb", out var srgb)
                            && srgb.ValueKind == JsonValueKind.False;

                        Assert.That(linear, Is.True, $"{proto.ID}: displacement map {layer.RsiPath} must be loaded without sRGB, otherwise the neutral offset turns into a shift");
                        checkedMasks++;
                    }
                }
            }

            Assert.That(checkedMasks, Is.GreaterThan(0));
        });

        await pair.CleanReturnAsync();
    }

    /// <summary>
    /// States of the displacement maps on the clothing layers of the slot.
    /// </summary>
    [Test]
    public async Task SeveredPartsKeepBodyLookTest()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = true,
            DummyTicker = false
        });

        var mapData = await pair.CreateTestMap();
        var server = pair.Server;
        var client = pair.Client;
        var entMng = server.ResolveDependency<IEntityManager>();
        var clientEntMng = client.ResolveDependency<IEntityManager>();
        var bodySystem = entMng.System<BodySystem>();

        EntityUid human = default!;
        EntityUid head = default!;
        EntityUid arm = default!;
        EntityUid donorArm = default!;
        Color skinColor = default!;
        Color eyeColor = default!;

        await server.WaitAssertion(() =>
        {
            human = entMng.SpawnEntity("MobHuman", mapData.MapCoords);
            donorArm = entMng.SpawnEntity("OrganReptilianArmLeft", mapData.MapCoords);
            Assert.That(bodySystem.TryGetOrganByCategory(human, HeadCategory, out var bodyHead), Is.True);
            Assert.That(bodySystem.TryGetOrganByCategory(human, EyesCategory, out var eyes), Is.True);
            skinColor = entMng.GetComponent<VisualOrganComponent>(bodyHead!.Value).Data.Color!.Value;
            eyeColor = entMng.GetComponent<VisualOrganComponent>(eyes!.Value).Data.Color!.Value;

            Assert.That(bodySystem.TryExtractOrgan(human, HeadCategory, out var severedHead), Is.True);
            Assert.That(bodySystem.TryExtractOrgan(human, ArmLeftCategory, out var severedArm), Is.True);
            head = severedHead!.Value.Owner;
            arm = severedArm!.Value.Owner;
        });

        await pair.RunTicksSync(10);

        await client.WaitAssertion(() =>
        {
            var headLayers = GetLayers(clientEntMng, clientEntMng.GetEntity(entMng.GetNetEntity(head)));
            var armLayers = GetLayers(clientEntMng, clientEntMng.GetEntity(entMng.GetNetEntity(arm)));
            var donorLayers = GetLayers(clientEntMng, clientEntMng.GetEntity(entMng.GetNetEntity(donorArm)));
            Assert.Multiple(() =>
            {
                Assert.That(headLayers[0].State, Is.EqualTo("head_m"), "The head is drawn by its body state");
                Assert.That(headLayers[0].Color, Is.EqualTo(skinColor), "The head keeps its skin color");
                Assert.That(headLayers, Has.Some.Matches<(string State, Color Color)>(x => x.State == "eyes" && x.Color == eyeColor), "The eyes stay in the head");
                Assert.That(armLayers.Select(x => x.State), Is.EqualTo(new[] { "l_arm", "l_hand" }), "The arm is drawn with its hand");
                Assert.That(donorLayers.Select(x => x.State), Is.EqualTo(new[] { "l_arm" }), "A spawned donor arm is drawn as on a body");
            });
        });

        await pair.CleanReturnAsync();
    }

    private static List<(string State, Color Color)> GetLayers(IEntityManager entMng, EntityUid uid)
    {
        var sprite = entMng.GetComponent<SpriteComponent>(uid);
        var layers = new List<(string State, Color Color)>();
        foreach (var layer in sprite.AllLayers)
        {
            if (layer is SpriteComponent.Layer { Visible: true } spriteLayer)
                layers.Add((spriteLayer.State.Name ?? string.Empty, spriteLayer.Color));
        }

        return layers;
    }

    private static List<string> GetSlotMasks(IEntityManager entMng, EntityUid uid, string slot)
    {
        var states = new List<string>();
        var sprite = entMng.GetComponent<SpriteComponent>(uid);
        var spriteSystem = entMng.System<SpriteSystem>();
        if (!entMng.GetComponent<InventorySlotsComponent>(uid).VisualLayerKeys.TryGetValue(slot, out var keys))
            return states;

        foreach (var key in keys)
        {
            if (spriteSystem.LayerMapTryGet((uid, sprite), key, out var index, false)
                && sprite[index] is SpriteComponent.Layer { CopyToShaderParameters: not null } layer)
                states.Add(layer.State.Name ?? string.Empty);
        }

        return states;
    }
}
