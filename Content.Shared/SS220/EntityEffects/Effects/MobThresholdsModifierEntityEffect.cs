// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.EntityEffects;
using Content.Shared.SS220.Chemistry.MobThresholdsModifierStatusEffect;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.EntityEffects.Effects;

public sealed partial class MobThresholdsModifier : EntityEffectBase<MobThresholdsModifier>
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

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var factory = IoCManager.Resolve<IComponentFactory>();
        if (!prototype.TryIndex<EntityPrototype>(StatusEffectId, out var statusProto) ||
            !statusProto.TryGetComponent<MobThresholdsModifierStatusEffectComponent>(out var component, factory))
            return string.Empty;

        var lines = new List<string>();
        foreach (var (state, modifier) in component.Modifiers)
        {
            var writeMultiplier = modifier.Multiplier != 1;
            var writeFlat = modifier.Flat != 0;

            if (!writeMultiplier && !writeFlat)
                continue;

            var modifierType = writeMultiplier && writeFlat ? "both"
                : writeMultiplier ? "multiplier"
                : "flat";

            var multiplierDesc = Loc.GetString("reagent-effect-guidebook-mob-thresholds-modifier-multiplier",
                ("value", modifier.Multiplier.Float()));

            var flat = modifier.Flat.Float();
            var flatDesc = Loc.GetString("reagent-effect-guidebook-mob-thresholds-modifier-flat",
                ("increase", flat >= 0),
                ("value", flat));

            var line = "\n    " + Loc.GetString("reagent-effect-guidebook-mob-thresholds-modifier-line",
                ("mobstate", state.ToString()),
                ("modifierType", modifierType),
                ("multiplier", multiplierDesc),
                ("flat", flatDesc));
            lines.Add(line);
        }

        if (lines.Count <= 0)
            return string.Empty;

        var statesChanges = string.Join(';', lines);
        var result = Loc.GetString("reagent-effect-guidebook-mob-thresholds-modifier",
            ("refresh", Refresh),
            ("duration", Duration.TotalSeconds),
            ("stateschanges", statesChanges));

        return result;
    }
}
