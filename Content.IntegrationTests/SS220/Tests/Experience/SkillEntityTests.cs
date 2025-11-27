// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.IntegrationTests.SS220.Experience.SkillEntityTests;

/// <summary>
/// This tests ensures raising events pass to skill entity with correct order and ensures all skill condition works
/// </summary>
[TestFixture]
public sealed class SkillEntityTests
{
    [Test]
    public async Task TestSkillEntityEventsRainingAndOrdering()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = true,
            DummyTicker = false
        });
    }

    [Test]
    public async Task TestExperienceConditionOnSkillEntity()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = true,
            DummyTicker = false
        });
    }
}
