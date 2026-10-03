// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.IntegrationTests.Fixtures;
using System.Collections.Generic;
using Content.Shared.Clothing;
using Content.Shared.Clothing.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Flash;
using Content.Shared.Foldable;
using Content.Shared.Inventory;
using Content.Shared.Prototypes;
using Content.Shared.SS220.IgnoreLightVision.Components;
using Content.Shared.SS220.Clothing.Components;
using Content.Shared.SS220.NightVision;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Utility;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.SS220.Clothing;

public sealed class ForeheadGlassesTest : GameTest
{
    public override PoolSettings PoolSettings => new() { Connected = true };

    [Test]
    public async Task ProtectionOnlyWorksOverEyes()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var wearer = SEntMan.SpawnEntity("MobHuman", map.GridCoords);
            var glasses = SEntMan.SpawnEntity("ClothingEyesQuartermasterGlasses", map.GridCoords);
            var inventory = Server.System<InventorySystem>();
            Assert.That(inventory.TryEquip(wearer, glasses, "eyes"), Is.True);
            CheckProtection(wearer, true);
            Assert.That(inventory.TryUnequip(wearer, "eyes"), Is.True);
            Assert.That(inventory.TryEquip(wearer, glasses, "head"), Is.True);
            CheckProtection(wearer, false);
            Assert.That(inventory.TryUnequip(wearer, "head"), Is.True);
            Assert.That(inventory.TryEquip(wearer, glasses, "eyes"), Is.True);
            CheckProtection(wearer, true);
            SEntMan.DeleteEntity(wearer);
        });
    }

    private void CheckProtection(EntityUid wearer, bool expected)
    {
        var flash = new FlashAttemptEvent(wearer, null, null);
        SEntMan.EventBus.RaiseLocalEvent(wearer, ref flash);
        Assert.That(flash.Cancelled, Is.EqualTo(expected));
        var welding = new GetEyeProtectionEvent();
        SEntMan.EventBus.RaiseLocalEvent(wearer, welding);
        Assert.That(welding.Protection > TimeSpan.Zero, Is.EqualTo(expected));
    }

    [Test]
    public async Task RemovingForeheadThermalsDoesNotDisableEyeThermals()
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var wearer = SEntMan.SpawnEntity("MobHuman", map.GridCoords);
            var eyes = SEntMan.SpawnEntity("ClothingEyesThermalGlassesSecurity", map.GridCoords);
            var head = SEntMan.SpawnEntity("ClothingEyesHudOmniversal", map.GridCoords);
            var inventory = Server.System<InventorySystem>();
            Assert.That(inventory.TryEquip(wearer, head, "head"), Is.True);
            Assert.That(SEntMan.HasComponent<ThermalVisionComponent>(wearer), Is.False);
            Assert.That(inventory.TryEquip(wearer, eyes, "eyes"), Is.True);
            Assert.That(SEntMan.HasComponent<ThermalVisionComponent>(wearer), Is.True);
            Assert.That(inventory.TryUnequip(wearer, "head"), Is.True);
            Assert.That(SEntMan.HasComponent<ThermalVisionComponent>(wearer), Is.True);
            SEntMan.DeleteEntity(head);
            SEntMan.DeleteEntity(wearer);
        });
    }

    [TestCase("ClothingEyesNightVision")]
    [TestCase("ClothingEyesNightVisionMedical")]
    [TestCase("ClothingEyesNightVisionSecurity")]
    [TestCase("ClothingEyesNightVisionSyndicate")]
    public async Task NightVisionDoesNotWorkOnForehead(string prototype)
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var wearer = SEntMan.SpawnEntity("MobHuman", map.GridCoords);
            var glasses = SEntMan.SpawnEntity(prototype, map.GridCoords);
            var inventory = Server.System<InventorySystem>();
            var vision = SEntMan.GetComponent<NightVisionComponent>(glasses);
            Assert.That(inventory.TryEquip(wearer, glasses, "eyes"), Is.True);
            Server.System<Content.Server.SS220.NightVision.NightVisionSystem>().SetEnabled((glasses, vision), true);
            Assert.That(vision.Wearer, Is.EqualTo(wearer));
            Assert.That(vision.Enabled, Is.True);
            Assert.That(inventory.TryUnequip(wearer, "eyes"), Is.True);
            Assert.That(inventory.TryEquip(wearer, glasses, "head"), Is.True);
            Assert.That(vision.Wearer, Is.Null);
            Assert.That(vision.Enabled, Is.False);
            Assert.That(inventory.TryUnequip(wearer, "head"), Is.True);
            Assert.That(inventory.TryEquip(wearer, glasses, "eyes"), Is.True);
            Assert.That(vision.Wearer, Is.EqualTo(wearer));
            Assert.That(vision.Enabled, Is.False, "Do not reactivate night vision automatically.");
            SEntMan.DeleteEntity(wearer);
        });
    }

    [TestCase("ClothingEyesChameleon")]
    [TestCase("ClothingEyesGlassesHiddenSecurity")]
    [TestCase("ThermalVisorChameleon")]
    public async Task SyndicateGlassesCanMoveBetweenEyesAndHead(string prototype)
    {
        var map = await Pair.CreateTestMap();
        await Server.WaitAssertion(() =>
        {
            var wearer = SEntMan.SpawnEntity("MobHuman", map.GridCoords);
            var glasses = SEntMan.SpawnEntity(prototype, map.GridCoords);
            var inventory = Server.System<InventorySystem>();
            Assert.That(inventory.TryEquip(wearer, glasses, "head"), Is.True);
            CheckProtection(wearer, false);
            Assert.That(SEntMan.HasComponent<ThermalVisionComponent>(wearer), Is.False);
            Assert.That(inventory.TryUnequip(wearer, "head"), Is.True);
            Assert.That(inventory.TryEquip(wearer, glasses, "eyes"), Is.True);
            if (prototype == "ThermalVisorChameleon")
                Assert.That(SEntMan.HasComponent<ThermalVisionComponent>(wearer), Is.True);
            Assert.That(inventory.TryUnequip(wearer, "eyes"), Is.True);
            Assert.That(inventory.TryEquip(wearer, glasses, "head"), Is.True);
            Assert.That(SEntMan.HasComponent<ThermalVisionComponent>(wearer), Is.False);
            CheckProtection(wearer, false);
            SEntMan.DeleteEntity(wearer);
        });
    }

    [TestCase("ClothingEyesChameleon")]
    [TestCase("ThermalVisorChameleon")]
    public async Task ChameleonWithoutForeheadDisguiseUsesOriginalVisuals(string prototype)
    {
        await Client.WaitAssertion(() =>
        {
            var entities = Client.ResolveDependency<IEntityManager>();
            var prototypes = Client.ResolveDependency<IPrototypeManager>();
            var wearer = entities.SpawnEntity("MobHuman", MapCoordinates.Nullspace);
            var glasses = entities.SpawnEntity(prototype, MapCoordinates.Nullspace);
            EntProtoId disguiseId = "ClothingEyesHudMedical";
            var disguise = prototypes.Index(disguiseId);
            Assert.That(disguise.TryGetComponent<ClothingComponent>(out var clothing, entities.ComponentFactory), Is.True);
            Client.System<Content.Shared.Clothing.EntitySystems.ClothingSystem>().CopyVisuals(glasses, clothing!);
            var visuals = new GetEquipmentVisualsEvent(wearer, "head");
            entities.EventBus.RaiseLocalEvent(glasses, visuals);
            Assert.That(visuals.Layers, Is.Not.Empty);
            entities.DeleteEntity(glasses);
            entities.DeleteEntity(wearer);
        });
    }

    [TestCase("MobHuman")]
    [TestCase("MobArachnid")]
    [TestCase("MobMoth")]
    [TestCase("MobVox")]
    [TestCase("MobReptilian")]
    [TestCase("MobDwarf")]
    [TestCase("MobHamster")]
    public async Task EverySupportedPrototypeSupportsBothSlotsWithoutFolding(string wearerPrototype)
    {
        await Client.WaitAssertion(() =>
        {
            var entities = Client.ResolveDependency<IEntityManager>();
            var prototypes = Client.ResolveDependency<IPrototypeManager>();
            var wearer = entities.SpawnEntity(wearerPrototype, MapCoordinates.Nullspace);
            var missingVisuals = new List<string>();
            var cache = Client.ResolveDependency<IResourceCache>();
            foreach (var prototype in prototypes.EnumeratePrototypes<EntityPrototype>())
            {
                if (prototype.Abstract || !prototype.HasComponent<GlassesOnForeheadComponent>(entities.ComponentFactory))
                    continue;

                var glasses = entities.SpawnEntity(prototype.ID, MapCoordinates.Nullspace);
                var clothing = entities.GetComponent<ClothingComponent>(glasses);
                Assert.That(entities.HasComponent<FoldableComponent>(glasses), Is.False, prototype.ID);
                Assert.That(clothing.Slots, Is.EqualTo(SlotFlags.HEAD | SlotFlags.EYES), prototype.ID);
                var visuals = new GetEquipmentVisualsEvent(wearer, "head");
                entities.EventBus.RaiseLocalEvent(glasses, visuals);
                if (prototype.ID.StartsWith("ClothingSponsorCyber"))
                {
                    Assert.That(visuals.Layers.Count, Is.EqualTo(2), prototype.ID);
                    Assert.That(visuals.Layers[1].Item2.State, Is.EqualTo("equipped-HELMET-unshaded"), prototype.ID);
                    Assert.That(visuals.Layers[1].Item2.Shader, Is.EqualTo("unshaded"), prototype.ID);
                }
                if (prototype.ID is "ClothingSponsorGlassesJujutsuShlepa" or "ClothingSponsorGlassesJujutsuShlepaSunglasses")
                    Assert.That(visuals.Layers[0].Item2.RsiPath,
                        Is.EqualTo("SS220/Clothing/Sponsor/Eyes/Glasses/nanamigoggles.rsi"), prototype.ID);

                foreach (var (_, layer) in visuals.Layers)
                {
                    var rsi = layer.RsiPath != null
                        ? cache.GetResource<RSIResource>(SpriteSpecifierSerializer.TextureRoot / new ResPath(layer.RsiPath)).RSI
                        : entities.GetComponent<SpriteComponent>(glasses).BaseRSI;
                    Assert.That(rsi, Is.Not.Null, prototype.ID);
                    Assert.That(rsi!.TryGetState(layer.State!, out _), Is.True,
                        $"{prototype.ID} on {wearerPrototype}: missing {layer.State}");
                }
                if (visuals.Layers.Count == 0)
                    missingVisuals.Add(prototype.ID);
                entities.DeleteEntity(glasses);
            }
            entities.DeleteEntity(wearer);
            Assert.That(missingVisuals, Is.Empty);
        });
    }
}
