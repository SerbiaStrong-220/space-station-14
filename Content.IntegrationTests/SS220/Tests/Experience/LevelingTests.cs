// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.IntegrationTests.SS220.Experience.SkillEntityTests;

/// <summary>
/// This tests ensures that leveling works correct, essentially we simulate learning and ensures that some actions add learning progress
/// </summary>
[TestFixture]
public sealed class LevelingTests
{
    [Test]
    public async Task EnsureLevelingInvariants()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = true,
            DummyTicker = false
        });

    }

    [Test]
    public async Task EnsureLevelingByActions()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = true,
            DummyTicker = false
        });

    }
}
