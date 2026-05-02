// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.EntityEffects;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.StatusEffectNew;

namespace Content.Shared.SS220.EntityEffects.Effects;

public sealed partial class MobThresholdsModifierEffectSystem : EntityEffectSystem<MobThresholdsComponent, MobThresholdsModifier>
{
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;

    protected override void Effect(Entity<MobThresholdsComponent> entity, ref EntityEffectEvent<MobThresholdsModifier> args)
    {
        if (string.IsNullOrEmpty(args.Effect.StatusEffectId))
            return;

        var statusEffectsSys = IoCManager.Resolve<IEntityManager>().System<StatusEffectsSystem>();

        if (args.Effect.Refresh)
            statusEffectsSys.TrySetStatusEffectDuration(entity, args.Effect.StatusEffectId, args.Effect.Duration);
        else
            statusEffectsSys.TryAddStatusEffectDuration(entity, args.Effect.StatusEffectId, args.Effect.Duration);

        if (args.Effect.DependsOnAdaptation)
            _mobThreshold.RefreshModifiers(entity);
    }
}
