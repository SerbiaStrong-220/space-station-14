// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.StatusEffectNew;

namespace Content.Shared.SS220.Chemistry.MobThresholdsModifierStatusEffect;
public sealed class MobThresholdsModifierStatusEffectSystem : EntitySystem
{
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MobThresholdsModifierStatusEffectComponent, StatusEffectAppliedEvent>(OnStatusEffectApplied);
        SubscribeLocalEvent<MobThresholdsModifierStatusEffectComponent, StatusEffectRemovedEvent>(OnStatusEffectRemoved);
        SubscribeLocalEvent<MobThresholdsModifierStatusEffectComponent,
            StatusEffectRelayedEvent<RefreshMobThresholdsModifiersEvent>>(OnRelayedRefreshMobThresholdsModifiers);
    }

    private void OnStatusEffectApplied(Entity<MobThresholdsModifierStatusEffectComponent> entity, ref StatusEffectAppliedEvent args)
    {
        var owner = args.Target;

        if (TryComp<MobThresholdsComponent>(owner, out var mobThresholds))
            _mobThreshold.RefreshModifiers((owner, mobThresholds));
    }

    private void OnStatusEffectRemoved(Entity<MobThresholdsModifierStatusEffectComponent> entity, ref StatusEffectRemovedEvent args)
    {
        var owner = args.Target;

        if (TryComp<MobThresholdsComponent>(owner, out var mobThresholds))
            _mobThreshold.RefreshModifiers((owner, mobThresholds));
    }

    private void OnRelayedRefreshMobThresholdsModifiers(
        Entity<MobThresholdsModifierStatusEffectComponent> entity,
        ref StatusEffectRelayedEvent<RefreshMobThresholdsModifiersEvent> args)
    {
        foreach (var (state, modifier) in entity.Comp.Modifiers)
        {
            args.Args.ApplyMultiplier(state, modifier.Multiplier);
            args.Args.ApplyFlatModifier(state, modifier.Flat);
        }
    }
}
