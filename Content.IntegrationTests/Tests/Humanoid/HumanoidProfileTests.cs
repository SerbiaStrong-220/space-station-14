using Content.IntegrationTests.Fixtures;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Content.Shared.Speech.Components;
using Content.Shared.SS220.TTS;
using Content.Shared.SS220.TTS.Prototypes;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Humanoid;

[TestFixture]
[TestOf(typeof(HumanoidProfileSystem))]
public sealed class HumanoidProfileTests : GameTest
{
    private static readonly ProtoId<SpeciesPrototype> Vox = "Vox";
    private static readonly ProtoId<TtsVoicePrototype> Voice = "father_grigori";

    [Test]
    public async Task EnsureValidLoading()
    {
        var pair = Pair;
        var server = pair.Server;

        await server.WaitIdleAsync();

        await server.WaitAssertion(() =>
        {
            var entityManager = server.ResolveDependency<IEntityManager>();
            var humanoidProfile = entityManager.System<HumanoidProfileSystem>();
            var human = entityManager.Spawn("MobHuman");
            humanoidProfile.ApplyProfileTo(human, new HumanoidCharacterProfile()
                .WithSex(Sex.Female)
                .WithAge(67)
                .WithVoicePreferences(SharedTtsSystem.DefaultVoicePreferences.Clone()) // SS220 tts
                .WithGender(Gender.Neuter)
                .WithSpecies(Vox));
            var humanoidComponent = entityManager.GetComponent<HumanoidProfileComponent>(human);
            var voiceComponent = entityManager.GetComponent<VocalComponent>(human);

            Assert.That(humanoidComponent.Age, Is.EqualTo(67));
            Assert.That(humanoidComponent.Sex, Is.EqualTo(Sex.Female));
            Assert.That(humanoidComponent.Gender, Is.EqualTo(Gender.Neuter));
            Assert.That(humanoidComponent.Species, Is.EqualTo(Vox));

            // SS220-tts-tests-begin
            Assert.That(entityManager.TryGetComponent<TtsComponent>(human, out var ttsComponent));
            Assert.That(ttsComponent.VoicePreferences, Is.EqualTo(SharedTtsSystem.DefaultVoicePreferences));
            // SS220-tts-tests-end

            Assert.That(voiceComponent.Sounds, Is.Not.Null, message: "the MobHuman spawned by this test needs to have sex-specific sound set");
            Assert.That(voiceComponent.Sounds![Sex.Female], Is.EqualTo(voiceComponent.EmoteSounds));
        });
    }
}
