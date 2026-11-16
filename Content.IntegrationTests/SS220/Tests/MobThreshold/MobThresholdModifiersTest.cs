// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.SS220.Chemistry.MobThresholdsModifierStatusEffect;
using Content.Shared.StatusEffectNew;
using Robust.Client.Graphics.Clyde;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using System.Collections.Generic;
using System.Linq;

namespace Content.IntegrationTests.SS220.Tests.MobThreshold;

[TestFixture]
public sealed class MobThresholdModifiersTest
{
    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  id: ModifierStatusEffectDummy
  components:
  - type: Sprite
    drawdepth: Effects
  - type: Tag
    tags:
    - HideContextMenu
  - type: RejuvenateRemovedStatusEffect
  - type: StatusEffect
    whitelist:
      components:
      - MobThresholds
  - type: MobThresholdsModifierStatusEffect
    modifiers:
      Critical:
        multiplier: 1.5
        flat: -20
      Dead:
        multiplier: 2
        flat: 45
";

    [Test]
    public async Task AddRemoveMobThresholdsModifierStatusEffectTest()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = true,
            DummyTicker = false
        });

        var server = pair.Server;

        var protoMng = server.ResolveDependency<IPrototypeManager>();

        const string effectId = "ModifierStatusEffectDummy";
        var effectProto = protoMng.Index<EntityPrototype>(effectId);

        MobThresholdsModifierStatusEffectComponent statusEffectComp = default!;
        await server.WaitAssertion(() =>
        {
            var factory = server.ResolveDependency<IComponentFactory>();
            Assert.That(effectProto.TryGetComponent(out statusEffectComp, factory), Is.True);
        });

        var entity = pair.Player!.AttachedEntity!.Value;
        var entMng = server.ResolveDependency<IEntityManager>();
        Assert.That(entMng.TryGetComponent<MobThresholdsComponent>(entity, out var mobThresholds), Is.True);

        var targetThresholds = new Dictionary<MobState, FixedPoint2>();
        foreach (var (value, state) in mobThresholds.BaseThresholds)
        {
            var result = value;
            if (statusEffectComp.Modifiers.TryGetValue(state, out var modifier))
                modifier.Apply(ref result);

            targetThresholds[state] = result;
        }

        await server.WaitAssertion(() =>
        {
            var statusEffectSys = entMng.System<StatusEffectsSystem>();
            Assert.That(statusEffectSys.TryAddStatusEffectDuration(entity, effectId, TimeSpan.FromSeconds(2)), Is.True);
        });
        await pair.RunTicksSync(1);

        var current = mobThresholds.Thresholds.ToDictionary(x => x.Value, x => x.Key).OrderBy(x => x.Key).ToDictionary();
        var target = targetThresholds.OrderBy(x => x.Key).ToDictionary();

        // Check if effect applied
        Assert.That(current, Is.EqualTo(target));

        await pair.RunSeconds(2);

        // Check that after removing modifiers we have base thresholds
        Assert.That(mobThresholds.Thresholds, Is.EqualTo(mobThresholds.BaseThresholds));

        await pair.CleanReturnAsync();
    }
}
