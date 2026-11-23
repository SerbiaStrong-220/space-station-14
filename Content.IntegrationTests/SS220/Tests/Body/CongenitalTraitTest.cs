// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.SS220.Body;
using Content.Shared.Bed.Sleep;
using Content.Shared.Body;
using Content.Shared.GameTicking;
using Content.Shared.Preferences;
using Content.Shared.Speech.Muting;
using Content.Shared.SS220.Body;
using Content.Shared.SS220.Pathology;
using Content.Shared.Traits;
using Content.Shared.Traits.Assorted;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.SS220.Tests.Body;

[TestFixture]
public sealed class CongenitalTraitTest
{
    private static readonly ProtoId<OrganCategoryPrototype> BrainCategory = "Brain";
    private static readonly ProtoId<TraitPrototype> BlindnessTrait = "Blindness";
    private static readonly ProtoId<TraitPrototype> MutedTrait = "Muted";
    private static readonly ProtoId<TraitPrototype> SnoringTrait = "Snoring";

    [Test]
    public async Task BrainTraitsMoveWithBrainTest()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true });
        var mapData = await pair.CreateTestMap();
        var server = pair.Server;
        var entMng = server.ResolveDependency<IEntityManager>();
        var protoManager = server.ResolveDependency<IPrototypeManager>();
        var bodySystem = entMng.System<BodySystem>();
        var congenitalSystem = entMng.System<CongenitalTraitSystem>();

        await server.WaitAssertion(() =>
        {
            var human = entMng.SpawnEntity("MobHuman", mapData.MapCoords);
            var recipient = entMng.SpawnEntity("MobHuman", mapData.MapCoords);
            var profile = HumanoidCharacterProfile.DefaultWithSpecies()
                .WithTraitPreference(BlindnessTrait, protoManager)
                .WithTraitPreference(MutedTrait, protoManager)
                .WithTraitPreference(SnoringTrait, protoManager);

            entMng.EventBus.RaiseEvent(EventSource.Local,
                new PlayerSpawnCompleteEvent(human, pair.Player!, "Passenger", false, true, 0, EntityUid.Invalid, profile));

            Assert.That(bodySystem.TryGetOrganByCategory(human, BrainCategory, out var brain), Is.True);
            var congenital = entMng.GetComponent<CongenitalTraitsComponent>(brain!.Value);
            Assert.Multiple(() =>
            {
                Assert.That(congenital.Traits, Is.EquivalentTo(new[] { BlindnessTrait, MutedTrait }), "Snoring isn't a brain trait");
                Assert.That(entMng.GetComponent<PathologyHolderComponent>(human).ActivePathologies, Is.Empty, "A trait is not a pathology");
                Assert.That(entMng.HasComponent<PermanentBlindnessComponent>(human), Is.True);
            });

            Assert.That(bodySystem.TryExtractOrgan(human, BrainCategory, out _), Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(entMng.HasComponent<PermanentBlindnessComponent>(human), Is.False, "The trait leaves with the brain");
                Assert.That(entMng.HasComponent<MutedComponent>(human), Is.False);
                Assert.That(entMng.HasComponent<SnoringComponent>(human), Is.True, "A body trait stays");
            });

            Assert.That(bodySystem.TryExtractOrgan(recipient, BrainCategory, out var ownBrain), Is.True);
            entMng.DeleteEntity(ownBrain!.Value);
            Assert.That(bodySystem.TryInsertOrgan(recipient, brain.Value.Owner), Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(entMng.HasComponent<PermanentBlindnessComponent>(recipient), Is.True, "The trait comes with the brain");
                Assert.That(entMng.HasComponent<MutedComponent>(recipient), Is.True);
            });

            // what a lobotomy does on success
            Assert.That(congenitalSystem.TryCureRandomTrait(recipient), Is.True);
            Assert.That(congenitalSystem.TryCureRandomTrait(recipient), Is.True);
            Assert.Multiple(() =>
            {
                Assert.That(congenitalSystem.TryCureRandomTrait(recipient), Is.False, "Nothing left to cure");
                Assert.That(congenital.Traits, Is.Empty);
                Assert.That(entMng.HasComponent<PermanentBlindnessComponent>(recipient), Is.False);
                Assert.That(entMng.HasComponent<MutedComponent>(recipient), Is.False);
            });
        });

        await pair.CleanReturnAsync();
    }
}
