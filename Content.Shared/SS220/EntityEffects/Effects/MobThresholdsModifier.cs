// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.EntityEffects;
using Content.Shared.SS220.Chemistry.MobThresholdsModifierStatusEffect;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.EntityEffects.Effects;

public sealed partial class MobThresholdsModifier : EntityEffect
{
    /// <summary>
    /// Id of the status effect entity with <see cref="MobThresholdsModifierStatusEffectComponent"/>.
    /// If different reagents should apply modifiers in parallel, then each of them should use a unique status effect entity
    /// </summary>
    [DataField(required: true)]
    public EntProtoId<MobThresholdsModifierStatusEffectComponent> StatusEffectId = string.Empty;

    /// <summary>
    /// Time during which the effect is applied/extended
    /// </summary>
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Should the duration of the effect reset with each use
    /// </summary>
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
        var factory = IoCManager.Resolve<IComponentFactory>();
        if (!prototype.TryIndex<EntityPrototype>(StatusEffectId, out var statusProto) ||
            !statusProto.TryGetComponent<MobThresholdsModifierStatusEffectComponent>(out var component, factory))
            return null;

        var lines = new List<string>();
        foreach (var (state, modifier) in component.Modifiers)
        {
            var info = string.Empty;
            if (modifier.Multiplier != 1)
                info += Loc.GetString("reagent-effect-guidebook-mob-thresholds-modifier-multiplier", ("multiplier", modifier.Multiplier));

            if (modifier.Flat != 0)
            {
                if (!string.IsNullOrEmpty(info))
                    info += " " + Loc.GetString("units-si--y") + " ";

                info += Loc.GetString("reagent-effect-guidebook-mob-thresholds-modifier-flat", ("flat", modifier.Flat));
            }

            if (string.IsNullOrEmpty(info))
                continue;

            var line = $"{state} - {info}";
            lines.Add(line);
        }

        if (lines.Count <= 0)
            return null;

        var statesChanges = string.Join("; ", lines);
        var result = Loc.GetString("reagent-effect-guidebook-mob-thresholds-modifier",
            ("refresh", Refresh),
            ("duration", Duration.TotalSeconds),
            ("stateschanges", statesChanges));

        return result;
    }
}
