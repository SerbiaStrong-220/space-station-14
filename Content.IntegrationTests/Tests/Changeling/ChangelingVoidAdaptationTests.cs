// SS220 Changeling
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Changeling.Components;
using Content.Shared.Changeling.Mutations;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.Changeling;

[TestFixture]
public sealed class ChangelingVoidAdaptationTests : GameTest
{
    [Test]
    [PairConfig(nameof(PsDisconnected))]
    public async Task VoidAdaptationSuppressesRespirationWithoutHealingAndRollsBack()
    {
        var pair = Pair;
        var server = pair.Server;
        var entMan = server.EntMan;
        var testMap = await pair.CreateTestMap();
        EntityUid ling = default;
        RespiratorComponent respirator = null!;
        RespiratorSystem respiratorSystem = null!;
        FixedPoint2 damageWhileAdapted = default;
        float saturationWhileAdapted = default;

        await server.WaitAssertion(() =>
        {
            ling = entMan.SpawnEntity("MobLing", testMap.GridCoords);
        });
        await pair.RunTicksSync(1);

        await server.WaitAssertion(() =>
        {
            respirator = entMan.GetComponent<RespiratorComponent>(ling);
            respiratorSystem = entMan.System<RespiratorSystem>();
            var damageable = entMan.System<DamageableSystem>();
            var existingDamage = new DamageSpecifier
            {
                DamageDict = { { "Asphyxiation", 10 } },
            };
            damageable.ChangeDamage(ling, existingDamage, ignoreResistances: true);

            respiratorSystem.UpdateSaturation(ling, respirator.SuffocationThreshold - respirator.Saturation);
        });
        await pair.RunSeconds(2.1f);

        await server.WaitAssertion(() =>
        {
            Assert.That(respirator.SuffocationCycles, Is.GreaterThan(0),
                "The test entity must begin suffocating before Void Adaptation is enabled.");
            var damageable = entMan.System<DamageableSystem>();
            damageWhileAdapted = damageable.GetAllDamage(ling).DamageDict["Asphyxiation"];

            var enable = new ChangelingVoidAdaptationActionEvent
            {
                Performer = ling,
                ChemicalCost = 0f,
                UpkeepCost = 0f,
                UpkeepInterval = TimeSpan.FromHours(1),
            };
            entMan.EventBus.RaiseLocalEvent(ling, enable);
            Assert.That(enable.Handled, Is.True);

            var protection = entMan.GetComponent<ChangelingEnvironmentalProtectionComponent>(ling);
            Assert.That(protection.RespirationImmunity, Is.True);

            saturationWhileAdapted = respirator.Saturation;
        });
        await pair.RunSeconds(2.1f);

        await server.WaitAssertion(() =>
        {
            var damageable = entMan.System<DamageableSystem>();
            Assert.Multiple(() =>
            {
                Assert.That(respirator.Saturation, Is.EqualTo(saturationWhileAdapted),
                    "Void Adaptation must prevent oxygen saturation loss.");
                Assert.That(respirator.SuffocationCycles, Is.Zero,
                    "Void Adaptation must clear the active suffocation state.");
                Assert.That(damageable.GetAllDamage(ling).DamageDict["Asphyxiation"],
                    Is.EqualTo(damageWhileAdapted),
                    "Suppressing respiration must not heal existing asphyxiation damage.");
            });

            var disable = new ChangelingVoidAdaptationActionEvent
            {
                Performer = ling,
                ChemicalCost = 0f,
            };
            entMan.EventBus.RaiseLocalEvent(ling, disable);
            Assert.That(disable.Handled, Is.True);
            Assert.That(entMan.HasComponent<ChangelingEnvironmentalProtectionComponent>(ling), Is.False);

            respiratorSystem.UpdateSaturation(ling, respirator.SuffocationThreshold - respirator.Saturation);
        });
        await pair.RunSeconds(2.1f);

        await server.WaitAssertion(() =>
        {
            var damageable = entMan.System<DamageableSystem>();
            Assert.Multiple(() =>
            {
                Assert.That(respirator.SuffocationCycles, Is.GreaterThan(0),
                    "Normal respiration must resume after Void Adaptation is disabled.");
                Assert.That(damageable.GetAllDamage(ling).DamageDict["Asphyxiation"],
                    Is.GreaterThan(damageWhileAdapted),
                    "Suffocation damage must resume after Void Adaptation is disabled.");
            });
        });
    }
}
