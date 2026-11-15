// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage.Components;
using Content.Shared.HealthExaminable;
using Content.Shared.IdentityManagement;
using Content.Shared.SS220.Pathology;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;

namespace Content.IntegrationTests.SS220.Tests.Pathology;

[TestFixture]
public sealed class PathologyExamineTest
{
    [Test]
    public async Task HealthExamineShowsOnlyVisiblePathologiesWithSeverityColors()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = true,
            DummyTicker = false
        });

        var server = pair.Server;
        var map = await pair.CreateTestMap();
        var entMng = server.ResolveDependency<IEntityManager>();
        var pathologySystem = entMng.System<SharedPathologySystem>();
        var healthExamine = entMng.System<HealthExaminableSystem>();

        EntityUid human = default;

        await server.WaitAssertion(() =>
        {
            human = entMng.SpawnEntity("MobHuman", map.GridCoords);
            Assert.Multiple(() =>
            {
                Assert.That(entMng.TryGetComponent<PathologyHolderComponent>(human, out var holder), Is.True);
                Assert.That(entMng.TryGetComponent<HealthExaminableComponent>(human, out var examinable), Is.True);
                Assert.That(entMng.TryGetComponent<DamageableComponent>(human, out var damageable), Is.True);

                var entity = new Entity<PathologyHolderComponent>(human, holder!);
                var noneText = Loc.GetString($"health-examinable-{examinable!.LocPrefix}-none");

                // Hidden internal pathology must not appear in health examine.
                Assert.That(pathologySystem.TryAddPathology(entity, "TorsoProjectileFragment"), Is.True);
                var baselineMarkup = healthExamine.CreateMarkup(human, examinable!, damageable!).ToMarkup();
                Assert.That(baselineMarkup, Does.Contain(noneText));
                Assert.That(baselineMarkup, Does.Not.Contain("#FFCC44FF"));
                Assert.That(baselineMarkup, Does.Not.Contain("#FF5555FF"));

                // Moderate external pathology renders in moderate color (#FFCC44FF) and suppresses the "-none" fallback.
                Assert.That(pathologySystem.TryAddPathology(entity, "LeftArmDislocation"), Is.True);
                var targetIdentity = Identity.Entity(human, entMng);
                var expectedDislocationText = Loc.GetString("pathology-leftarm-dislocation-examine", ("target", targetIdentity));
                var moderateMarkup = healthExamine.CreateMarkup(human, examinable!, damageable!).ToMarkup();
                Assert.That(moderateMarkup, Does.Not.Contain(noneText));
                Assert.That(moderateMarkup, Does.Contain($"[color=#FFCC44FF]{expectedDislocationText}[/color]"));
                Assert.That(moderateMarkup, Does.Not.Contain("#FF5555FF"));

                // Severe external pathology renders in severe color (#FF5555FF).
                Assert.That(pathologySystem.TryAddPathology(entity, "HeadCranialFissure"), Is.True);
                var expectedFissureText = Loc.GetString("pathology-head-cranial-fissure-examine", ("target", targetIdentity));
                var severeMarkup = healthExamine.CreateMarkup(human, examinable!, damageable!).ToMarkup();
                Assert.That(severeMarkup, Does.Contain($"[color=#FFCC44FF]{expectedDislocationText}[/color]"));
                Assert.That(severeMarkup, Does.Contain($"[color=#FF5555FF]{expectedFissureText}[/color]"));

                // Removing severe pathology removes only its line.
                Assert.That(pathologySystem.TryRemovePathology(entity, "HeadCranialFissure"), Is.True);
                var afterRemovalMarkup = healthExamine.CreateMarkup(human, examinable!, damageable!).ToMarkup();
                Assert.That(afterRemovalMarkup, Does.Contain($"[color=#FFCC44FF]{expectedDislocationText}[/color]"));
                Assert.That(afterRemovalMarkup, Does.Not.Contain(expectedFissureText));

                // Removing all visible pathologies restores the "-none" fallback.
                Assert.That(pathologySystem.TryRemovePathology(entity, "LeftArmDislocation"), Is.True);
                var afterClearMarkup = healthExamine.CreateMarkup(human, examinable!, damageable!).ToMarkup();
                Assert.That(afterClearMarkup, Does.Contain(noneText));
                Assert.That(afterClearMarkup, Does.Not.Contain(expectedDislocationText));
            });
        });

        await pair.CleanReturnAsync();
    }
}
