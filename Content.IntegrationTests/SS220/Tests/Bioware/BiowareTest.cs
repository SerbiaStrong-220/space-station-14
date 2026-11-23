// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Electrocution;
using Content.Server.SS220.Surgery.Actions;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Metabolism;
using Content.Shared.SS220.Bioware;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Markdown.Mapping;

namespace Content.IntegrationTests.SS220.Tests.Bioware;

[TestFixture]
public sealed class BiowareTest
{
    private const string Mob = "MobHuman";
    private static readonly ProtoId<OrganCategoryPrototype> HeartCategory = "Heart";

    [Test]
    public async Task AddComponentsActionTest()
    {
        await using var pair = await PoolManager.GetServerClient();
        var mapData = await pair.CreateTestMap();
        var server = pair.Server;
        var entMng = server.ResolveDependency<IEntityManager>();

        await server.WaitAssertion(() =>
        {
            var human = entMng.SpawnEntity(Mob, mapData.MapCoords);
            var action = new AddComponentsAction
            {
                Components = new ComponentRegistry
                {
                    ["VeinThreading"] = new EntityPrototype.ComponentRegistryEntry(new VeinThreadingComponent(), new MappingDataNode()),
                },
            };

            action.PerformAction(human, human, null, entMng);
            Assert.That(entMng.HasComponent<VeinThreadingComponent>(human), Is.True);

            // A repeated action keeps the existing component
            var existing = entMng.GetComponent<VeinThreadingComponent>(human);
            action.PerformAction(human, human, null, entMng);
            Assert.That(entMng.GetComponent<VeinThreadingComponent>(human), Is.SameAs(existing));
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task VeinThreadingHalvesBloodLossTest()
    {
        await using var pair = await PoolManager.GetServerClient();
        var mapData = await pair.CreateTestMap();
        var server = pair.Server;
        var entMng = server.ResolveDependency<IEntityManager>();
        var bloodstream = entMng.System<SharedBloodstreamSystem>();

        await server.WaitAssertion(() =>
        {
            var control = entMng.SpawnEntity(Mob, mapData.MapCoords);
            var modified = entMng.SpawnEntity(Mob, mapData.MapCoords);
            entMng.AddComponent<VeinThreadingComponent>(modified);

            var controlLoss = Bleed(control);
            var modifiedLoss = Bleed(modified);

            Assert.That(controlLoss, Is.GreaterThan(0f));
            Assert.That(modifiedLoss, Is.EqualTo(controlLoss * 0.5f).Within(0.001f));
        });

        await pair.CleanReturnAsync();
        return;

        float Bleed(EntityUid uid)
        {
            var comp = entMng.GetComponent<BloodstreamComponent>(uid);
            var before = bloodstream.GetBloodLevel(uid);
            bloodstream.TryModifyBleedAmount(uid, 10f);
            bloodstream.TickBleed((uid, comp));
            return before - bloodstream.GetBloodLevel(uid);
        }
    }

    [Test]
    public async Task NerveSplicingHalvesStaminaDamageTest()
    {
        await using var pair = await PoolManager.GetServerClient();
        var mapData = await pair.CreateTestMap();
        var server = pair.Server;
        var entMng = server.ResolveDependency<IEntityManager>();
        var stamina = entMng.System<SharedStaminaSystem>();

        await server.WaitAssertion(() =>
        {
            var control = entMng.SpawnEntity(Mob, mapData.MapCoords);
            var modified = entMng.SpawnEntity(Mob, mapData.MapCoords);
            entMng.AddComponent<NerveSplicingComponent>(modified);

            stamina.TakeStaminaDamage(control, 40f);
            stamina.TakeStaminaDamage(modified, 40f);

            var controlDamage = entMng.GetComponent<StaminaComponent>(control).StaminaDamage;
            Assert.That(controlDamage, Is.EqualTo(40f).Within(0.001f));
            Assert.That(entMng.GetComponent<StaminaComponent>(modified).StaminaDamage, Is.EqualTo(controlDamage * 0.5f).Within(0.001f));
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task NerveGroundingBlocksShockTest()
    {
        await using var pair = await PoolManager.GetServerClient();
        var mapData = await pair.CreateTestMap();
        var server = pair.Server;
        var entMng = server.ResolveDependency<IEntityManager>();
        var electrocution = entMng.System<ElectrocutionSystem>();

        await server.WaitAssertion(() =>
        {
            var control = entMng.SpawnEntity(Mob, mapData.MapCoords);
            var modified = entMng.SpawnEntity(Mob, mapData.MapCoords);
            entMng.AddComponent<NerveGroundingComponent>(modified);

            var time = TimeSpan.FromSeconds(1);
            Assert.Multiple(() =>
            {
                Assert.That(electrocution.TryDoElectrocution(control, null, 10, time, true), Is.True);
                Assert.That(electrocution.TryDoElectrocution(modified, null, 10, time, true), Is.False, "Grounded nerves block a shock");
                Assert.That(electrocution.TryDoElectrocution(modified, null, 10, time, true, ignoreInsulation: true), Is.True,
                    "A shock that ignores insulation (defibrillator) passes");
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task CapillaryWeavingSpeedsUpHeartTest()
    {
        await using var pair = await PoolManager.GetServerClient();
        var mapData = await pair.CreateTestMap();
        var server = pair.Server;
        var entMng = server.ResolveDependency<IEntityManager>();
        var bodySystem = entMng.System<BodySystem>();
        var metabolizer = entMng.System<MetabolizerSystem>();

        await server.WaitAssertion(() =>
        {
            var human = entMng.SpawnEntity(Mob, mapData.MapCoords);
            var recipient = entMng.SpawnEntity(Mob, mapData.MapCoords);
            entMng.AddComponent<CapillaryWeavingComponent>(human);

            var heart = GetOrgan(human, "Heart");
            var liver = GetOrgan(human, "Liver");
            Assert.Multiple(() =>
            {
                Assert.That(Multiplier(heart), Is.EqualTo(1f / 1.5f).Within(0.001f), "The heart metabolizes the bloodstream");
                Assert.That(Multiplier(liver), Is.EqualTo(1f).Within(0.001f), "Other stages keep their rate");
            });

            // Applying the multiplier again doesn't stack the bioware
            metabolizer.UpdateMetabolicMultiplier(human);
            Assert.That(Multiplier(heart), Is.EqualTo(1f / 1.5f).Within(0.001f));

            // The bioware stays with the body: a new heart gets it, the old heart loses it in a body without it
            Assert.That(bodySystem.TryExtractOrgan(human, HeartCategory, out var extracted), Is.True);
            Assert.That(bodySystem.TryExtractOrgan(recipient, HeartCategory, out var recipientHeart), Is.True);
            Assert.That(bodySystem.TryInsertOrgan(human, recipientHeart!.Value), Is.True);
            Assert.That(bodySystem.TryInsertOrgan(recipient, extracted!.Value), Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(Multiplier(recipientHeart.Value), Is.EqualTo(1f / 1.5f).Within(0.001f));
                Assert.That(Multiplier(extracted.Value), Is.EqualTo(1f).Within(0.001f));
            });
        });

        await pair.CleanReturnAsync();
        return;

        EntityUid GetOrgan(EntityUid body, string category)
        {
            Assert.That(bodySystem.TryGetOrganByCategory(body, category, out var organ), Is.True);
            return organ!.Value;
        }

        float Multiplier(EntityUid organ)
        {
            return entMng.GetComponent<MetabolizerComponent>(organ).UpdateIntervalMultiplier;
        }
    }
}
