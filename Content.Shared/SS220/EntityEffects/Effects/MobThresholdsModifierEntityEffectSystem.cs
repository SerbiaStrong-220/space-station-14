// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.EntityEffects;
using Content.Shared.Mobs.Components;
using Content.Shared.SS220.Chemistry.MobThresholdsModifierStatusEffect;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.EntityEffects.Effects;

public sealed partial class MobThresholdsModifierEffectSystem : EntityEffectSystem<MobThresholdsComponent, MobThresholdsModifier>
{
    protected override void Effect(Entity<MobThresholdsComponent> entity, ref EntityEffectEvent<MobThresholdsModifier> args)
    {
        if (string.IsNullOrEmpty(args.Effect.StatusEffectId))
            return;

        var statusEffectsSys = IoCManager.Resolve<IEntityManager>().System<StatusEffectsSystem>();

        if (args.Effect.Refresh)
            statusEffectsSys.TrySetStatusEffectDuration(entity, args.Effect.StatusEffectId, args.Effect.Duration);
        else
            statusEffectsSys.TryAddStatusEffectDuration(entity, args.Effect.StatusEffectId, args.Effect.Duration);
    }
}
