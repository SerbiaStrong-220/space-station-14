// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.FixedPoint;
using Content.Shared.SS220.Experience;
using Content.Shared.SS220.Experience.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.SS220.Tests.Experience;

/// <summary>
/// This tests ensures that leveling works correct, essentially we simulate learning and ensures that some actions add learning progress
/// </summary>
[TestFixture]
public sealed class LevelingTests
{
    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  id: ExperienceDummyEntity
  components:
  - type: Experience
";

    [Test]
    public async Task EnsureLeveling()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = true,
            DummyTicker = false,
            InLobby = true,
            Dirty = true
        });

        await pair.CreateTestMap();

        var server = pair.Server;

        var protoManager = server.ResolveDependency<IPrototypeManager>();
        var experienceSystem = server.System<ExperienceSystem>();

        const string entityId = "ExperienceDummyEntity";
        var effectProto = protoManager.Index<EntityPrototype>(entityId);

        var testEntity = EntityUid.Invalid;
        server.Post(() =>
        {
            testEntity = server.EntMan.Spawn(entityId);
        });

        await pair.RunTicksSync(5);

        var skillTrees = protoManager.EnumeratePrototypes<SkillTreePrototype>();

        Assert.Multiple(() =>
        {
            Assert.That(server.EntMan.TryGetComponent<ExperienceComponent>(testEntity, out var experienceComponent), Is.EqualTo(true));

            foreach (var skillTree in skillTrees)
            {
                Assert.That(experienceSystem.TryGetSkillTreeLevels(testEntity, skillTree.ID, out var level, out var sublevel));
                Assert.That(level, Is.EqualTo(ExperienceSystem.StartSkillLevel));
                Assert.That(sublevel, Is.EqualTo(ExperienceSystem.StartSublevel));

                Assert.That(experienceSystem.TryGetEarnedSublevel(testEntity, skillTree.ID, out var earnedSublevels));
                Assert.That(earnedSublevels, Is.EqualTo(0));

                Assert.That(experienceSystem.TryGetLearningProgress(testEntity, skillTree.ID, out var progress));
                Assert.That(progress, Is.EqualTo(FixedPoint4.Zero));
            }
        });

        await server.WaitAssertion(() =>
        {
            foreach (var skillTree in skillTrees)
            {
                if (!skillTree.StudyingProgressPossible)
                    continue;

                var skillProto = protoManager.Index(skillTree.SkillTree[0]);

                if (!skillProto.LevelInfo.CanStartStudying)
                    continue;

                var maxSublevels = skillProto.LevelInfo.MaximumSublevel;

                int? level;
                int? sublevel;
                for (var i = 1; i < maxSublevels; i++)
                {
                    Assert.Multiple(() =>
                    {
                        Assert.That(experienceSystem.TryChangeStudyingProgress(testEntity, skillTree.ID, ExperienceSystem.EndLearningProgress), Is.EqualTo(true));
                        Assert.That(experienceSystem.TryGetSkillTreeLevels(testEntity, skillTree.ID, out level, out sublevel));
                        Assert.That(level, Is.EqualTo(ExperienceSystem.StartSkillLevel));
                        Assert.That(sublevel, Is.EqualTo(i + ExperienceSystem.StartSublevel));
                    });
                }

                if (!skillProto.LevelInfo.CanEndStudying)
                    continue;

                Assert.Multiple(() =>
                {
                    Assert.That(experienceSystem.TryChangeStudyingProgress(testEntity, skillTree.ID, ExperienceSystem.EndLearningProgress), Is.EqualTo(true));
                    Assert.That(experienceSystem.TryGetSkillTreeLevels(testEntity, skillTree.ID, out level, out sublevel));
                    Assert.That(level, Is.EqualTo(1 + ExperienceSystem.StartSkillLevel));
                    Assert.That(sublevel, Is.EqualTo(ExperienceSystem.StartSublevel));
                });
            }
        });

        Assert.DoesNotThrowAsync(async () => await pair.RunTicksSync(5));

        await pair.CleanReturnAsync();
    }


    public const string TestMentorEffectSkillTree = "Medicine";
    public readonly FixedPoint4 TestProgressAdd = 0.1f;
    public readonly FixedPoint4 Multiplier = 2f;
    public readonly FixedPoint4 Flat = 0.05f;

    [Test]
    public async Task EnsureMentorRoleEffect()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = true,
            DummyTicker = false,
            Dirty = true
        });

        var server = pair.Server;

        var protoManager = server.ResolveDependency<IPrototypeManager>();
        var experienceSystem = server.System<ExperienceSystem>();

        const string entityId = "ExperienceDummyEntity";
        var effectProto = protoManager.Index<EntityPrototype>(entityId);

        var testEntity = EntityUid.Invalid;
        server.Post(() =>
        {
            testEntity = server.EntMan.Spawn(entityId);
        });

        await pair.RunTicksSync(5);

        var skillTrees = protoManager.EnumeratePrototypes<SkillTreePrototype>();

        Assert.Multiple(() =>
        {
            Assert.That(server.EntMan.TryGetComponent<ExperienceComponent>(testEntity, out var experienceComponent), Is.EqualTo(true));
            Assert.That(experienceSystem.TryGetSkillTreeLevels(testEntity, TestMentorEffectSkillTree, out _, out _), Is.EqualTo(true));
            Assert.That(server.EntMan.HasComponent<AffectedByMentorComponent>(testEntity), Is.EqualTo(false));

#pragma warning disable NUnit2007
            // sanity-check-asserts
            Assert.That(TestProgressAdd * (Multiplier + 1) + Flat, Is.LessThan(ExperienceSystem.EndLearningProgress));
#pragma warning restore NUnit2007

            Assert.That(experienceSystem.TryChangeStudyingProgress(testEntity, TestMentorEffectSkillTree, TestProgressAdd), Is.EqualTo(true));
            Assert.That(experienceSystem.TryGetLearningProgress(testEntity, TestMentorEffectSkillTree, out var progress), Is.EqualTo(true));
            Assert.That(progress.Value, Is.EqualTo(TestProgressAdd));
        });

        server.Post(() =>
        {
            var affectedByMentorComponent = server.EntMan.AddComponent<AffectedByMentorComponent>(testEntity);

            affectedByMentorComponent.TeachInfo.Add(TestMentorEffectSkillTree, new()
            {
                Multiplier = Multiplier,
                Flat = Flat,
                MaxBuffSkillLevel = null
            });
        });

        await pair.RunTicksSync(5);

        Assert.Multiple(() =>
        {
            Assert.That(server.EntMan.TryGetComponent<ExperienceComponent>(testEntity, out var experienceComponent), Is.EqualTo(true));
            Assert.That(experienceSystem.TryGetSkillTreeLevels(testEntity, TestMentorEffectSkillTree, out _, out _), Is.EqualTo(true));
            Assert.That(server.EntMan.HasComponent<AffectedByMentorComponent>(testEntity), Is.EqualTo(true));

            Assert.That(experienceSystem.TryChangeStudyingProgress(testEntity, TestMentorEffectSkillTree, TestProgressAdd), Is.EqualTo(true));
            Assert.That(experienceSystem.TryGetLearningProgress(testEntity, TestMentorEffectSkillTree, out var progress), Is.EqualTo(true));
            Assert.That(progress.Value, Is.EqualTo(TestProgressAdd * (1 + Multiplier) + Flat));
        });

        await pair.CleanReturnAsync();
    }
}
