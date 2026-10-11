using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Shared.Actions.Components;
using Content.Shared.Body;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Explosion.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Metabolism;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.SS220.Felinid;
using Content.Shared.SS220.Felinid.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.SS220.Tests.Felinid;

[TestFixture]
public sealed class FelinidMechanicsTest : GameTest
{
    private static readonly EntProtoId FelinidMobPrototype = "MobFelinid";
    private static readonly EntProtoId FelinidAppearancePrototype = "AppearanceFelinid";
    private static readonly EntProtoId DashActionPrototype = "ActionDashSpeedModifier";
    private static readonly EntProtoId PipecrawlActionPrototype = "ActionDisposalPipeCrawler";
    private static readonly EntProtoId FelinidLungsPrototype = "OrganFelinidLungs";
    private static readonly EntProtoId FelinidHeartPrototype = "OrganFelinidHeart";
    private static readonly EntProtoId FelinidStomachPrototype = "OrganFelinidStomach";
    private static readonly EntProtoId FelinidLiverPrototype = "OrganFelinidLiver";
    private static readonly EntProtoId FelinidKidneysPrototype = "OrganFelinidKidneys";
    private static readonly ProtoId<DamageModifierSetPrototype> DamageModifierPrototype = "Felinid";

    [TestPrototypes]
    private const string Prototypes = """
- type: entity
  id: FelinidDashTimingTestMob
  components:
  - type: MobState
  - type: MovementSpeedModifier
  - type: Hunger
  - type: Thirst
    startingThirst: 500
  - type: DashSpeedModifier
    duration: 0.05
""";

    [Test]
    public async Task PrototypesMatchAcceptedDesignValues()
    {
        var server = Pair.Server;
        var entityManager = server.ResolveDependency<IEntityManager>();
        var prototypes = server.ResolveDependency<IPrototypeManager>();
        var factory = entityManager.ComponentFactory;

        await server.WaitAssertion(() =>
        {
            var mob = prototypes.Index(FelinidMobPrototype);
            var appearance = prototypes.Index(FelinidAppearancePrototype);
            var dashAction = prototypes.Index(DashActionPrototype);
            var pipecrawlAction = prototypes.Index(PipecrawlActionPrototype);
            var lungsPrototype = prototypes.Index(FelinidLungsPrototype);
            var heartPrototype = prototypes.Index(FelinidHeartPrototype);
            var stomachPrototype = prototypes.Index(FelinidStomachPrototype);
            var liverPrototype = prototypes.Index(FelinidLiverPrototype);
            var kidneysPrototype = prototypes.Index(FelinidKidneysPrototype);
            var damageModifiers = prototypes.Index(DamageModifierPrototype);

            Assert.That(mob.TryGetComponent<DashSpeedModifierComponent>(out var dash, factory), Is.True);
            Assert.That(mob.TryGetComponent<DodgeChanceComponent>(out var dodge, factory), Is.True);
            Assert.That(mob.TryGetComponent<ClimbSpeedModifierComponent>(out var climb, factory), Is.True);
            Assert.That(mob.TryGetComponent<DisposalPipeCrawlerComponent>(out var pipecrawl, factory), Is.True);
            Assert.That(mob.TryGetComponent<HungerComponent>(out var hunger, factory), Is.True);
            Assert.That(mob.TryGetComponent<ThirstComponent>(out var thirst, factory), Is.True);
            Assert.That(mob.TryGetComponent<MobThresholdsComponent>(out var thresholds, factory), Is.True);
            Assert.That(mob.TryGetComponent<ExplosionResistanceComponent>(out var explosion, factory), Is.True);
            Assert.That(appearance.TryGetComponent<InitialBodyComponent>(out var body, factory), Is.True);
            Assert.That(dashAction.TryGetComponent<ActionComponent>(out var dashActionComponent, factory), Is.True);
            Assert.That(pipecrawlAction.TryGetComponent<ActionComponent>(out var pipecrawlActionComponent, factory), Is.True);
            Assert.That(lungsPrototype.TryGetComponent<MetabolizerComponent>(out var lungs, factory), Is.True);
            Assert.That(heartPrototype.TryGetComponent<MetabolizerComponent>(out var heart, factory), Is.True);
            Assert.That(stomachPrototype.TryGetComponent<MetabolizerComponent>(out var stomach, factory), Is.True);
            Assert.That(liverPrototype.TryGetComponent<MetabolizerComponent>(out var liver, factory), Is.True);
            Assert.That(kidneysPrototype.TryGetComponent<MetabolizerComponent>(out var kidneys, factory), Is.True);

            Assert.Multiple(() =>
            {
                Assert.That(dash!.SpeedModifier, Is.EqualTo(1.3f));
                Assert.That(dash.Duration, Is.EqualTo(TimeSpan.FromSeconds(10)));
                Assert.That(dash.HungerCost, Is.EqualTo(40f));
                Assert.That(dash.ThirstCost, Is.EqualTo(40f));
                Assert.That(dashActionComponent!.UseDelay, Is.EqualTo(TimeSpan.FromMinutes(8)));

                Assert.That(dodge!.BaseDodgeChance, Is.EqualTo(0.15f));
                Assert.That(dodge.ExcellentHealthBonus, Is.EqualTo(0.025f));
                Assert.That(dodge.BadHealthPenalty, Is.EqualTo(0.025f));
                Assert.That(dodge.AwfulHealthPenalty, Is.EqualTo(0.05f));
                Assert.That(dodge.MinorNeedPenalty, Is.EqualTo(0.05f));
                Assert.That(dodge.MajorNeedPenalty, Is.EqualTo(0.10f));

                Assert.That(hunger!.BaseDecayRate, Is.EqualTo(0.01666666666f * 1.3f).Within(0.0001f));
                Assert.That(thirst!.BaseDecayRate, Is.EqualTo(0.1f * 1.3f).Within(0.0001f));
                Assert.That(lungs!.UpdateInterval, Is.EqualTo(TimeSpan.FromSeconds(0.8333)).Within(TimeSpan.FromMilliseconds(1)));
                Assert.That(heart!.UpdateInterval, Is.EqualTo(TimeSpan.FromSeconds(0.8333)).Within(TimeSpan.FromMilliseconds(1)));
                Assert.That(stomach!.UpdateInterval, Is.EqualTo(TimeSpan.FromSeconds(0.8333)).Within(TimeSpan.FromMilliseconds(1)));
                Assert.That(liver!.UpdateInterval, Is.EqualTo(TimeSpan.FromSeconds(0.8333)).Within(TimeSpan.FromMilliseconds(1)));
                Assert.That(kidneys!.UpdateInterval, Is.EqualTo(TimeSpan.FromSeconds(0.8333)).Within(TimeSpan.FromMilliseconds(1)));
                Assert.That(lungs.Stages.Single().Id, Is.EqualTo("Respiration"));
                Assert.That(heart.Stages.Single().Id, Is.EqualTo("Bloodstream"));
                Assert.That(stomach.Stages.Single().Id, Is.EqualTo("Digestion"));
                Assert.That(liver.Stages.Single().Id, Is.EqualTo("Metabolites"));
                Assert.That(kidneys.Stages, Is.Empty);
                Assert.That(body!.Organs["Lungs"].Id, Is.EqualTo("OrganFelinidLungs"));
                Assert.That(body.Organs["Heart"].Id, Is.EqualTo("OrganFelinidHeart"));
                Assert.That(body.Organs["Stomach"].Id, Is.EqualTo("OrganFelinidStomach"));
                Assert.That(body.Organs["Liver"].Id, Is.EqualTo("OrganFelinidLiver"));
                Assert.That(body.Organs["Kidneys"].Id, Is.EqualTo("OrganFelinidKidneys"));

                Assert.That(thresholds!.BaseThresholds[(FixedPoint2) 80], Is.EqualTo(MobState.Critical));
                Assert.That(thresholds.BaseThresholds[(FixedPoint2) 160], Is.EqualTo(MobState.Dead));
                Assert.That(damageModifiers.Coefficients["Blunt"], Is.EqualTo(1.1f));
                Assert.That(damageModifiers.Coefficients["Cold"], Is.EqualTo(1.1f));
                Assert.That(damageModifiers.Coefficients["Heat"], Is.EqualTo(0.9f));
                Assert.That(explosion!.DamageCoefficient, Is.EqualTo(1.1f));

                Assert.That(climb!.DelayMultiplier, Is.EqualTo(1f / 1.5f).Within(0.0001f));
                Assert.That(pipecrawl!.EnterCooldown, Is.EqualTo(TimeSpan.FromMinutes(2)));
                Assert.That(pipecrawl.ExitCooldown, Is.EqualTo(TimeSpan.FromMinutes(10)));
                Assert.That(pipecrawl.TransitTime, Is.EqualTo(TimeSpan.FromSeconds(1.1)));
                Assert.That(pipecrawlActionComponent!.CheckCanInteract, Is.False);
            });
        });
    }

    [Test]
    public async Task DashCostsAreAppliedAfterTheSpeedBoost()
    {
        var server = Pair.Server;
        var testMap = await Pair.CreateTestMap();
        var entityManager = server.ResolveDependency<IEntityManager>();
        var hungerSystem = entityManager.System<HungerSystem>();

        EntityUid mob = default;
        float hungerBefore = 0f;
        float thirstBefore = 0f;

        await server.WaitAssertion(() =>
        {
            mob = entityManager.SpawnEntity("FelinidDashTimingTestMob", testMap.GridCoords);
            var hunger = entityManager.GetComponent<HungerComponent>(mob);
            var thirst = entityManager.GetComponent<ThirstComponent>(mob);
            var dash = entityManager.GetComponent<DashSpeedModifierComponent>(mob);
            hungerBefore = hungerSystem.GetHunger(hunger);
            thirstBefore = thirst.CurrentThirst;

            var dashEvent = new DashSpeedModifierActionEvent();
            entityManager.EventBus.RaiseLocalEvent(mob, dashEvent);

            Assert.Multiple(() =>
            {
                Assert.That(hungerSystem.GetHunger(hunger), Is.EqualTo(hungerBefore).Within(0.01f));
                Assert.That(thirst.CurrentThirst, Is.EqualTo(thirstBefore));
                Assert.That(dash.CostAt, Is.Not.Null);
            });
        });

        await server.WaitRunTicks(10);

        await server.WaitAssertion(() =>
        {
            var hunger = entityManager.GetComponent<HungerComponent>(mob);
            var thirst = entityManager.GetComponent<ThirstComponent>(mob);
            var dash = entityManager.GetComponent<DashSpeedModifierComponent>(mob);

            Assert.Multiple(() =>
            {
                Assert.That(hungerSystem.GetHunger(hunger), Is.EqualTo(hungerBefore - 40f).Within(0.1f));
                Assert.That(thirst.CurrentThirst, Is.EqualTo(thirstBefore - 40f).Within(0.2f));
                Assert.That(dash.CostAt, Is.Null);
            });
        });
    }
}
