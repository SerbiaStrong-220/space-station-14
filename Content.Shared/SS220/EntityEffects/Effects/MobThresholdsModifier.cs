// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.EntityEffects;
using Content.Shared.SS220.Chemistry.MobThresholdsModifierStatusEffect;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.EntityEffects.Effects;

public sealed partial class MobThresholdsModifier : EntityEffect
{
    [DataField(required: true)]
    public EntProtoId<MobThresholdsModifierStatusEffectComponent> StatusEffectId = string.Empty;

    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(2);

    [DataField]
    public bool Refresh = false;

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (string.IsNullOrEmpty(StatusEffectId))
            return;

        var statusEffectsSys = IoCManager.Resolve<IEntityManager>().System<StatusEffectsSystem>();

        if (Refresh)
            statusEffectsSys.TrySetStatusEffectDuration(args.TargetEntity, StatusEffectId, Duration);
        else
            statusEffectsSys.TryAddStatusEffectDuration(args.TargetEntity, StatusEffectId, Duration);
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return string.Empty;
    }
}
