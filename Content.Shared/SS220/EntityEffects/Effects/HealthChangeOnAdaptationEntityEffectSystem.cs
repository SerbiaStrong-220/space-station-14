using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Localizations;
using Content.Shared.SS220.ChemicalAdaptation;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Adjust the damages on this entity by specified amounts.
/// Amounts are modified by scale.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class HealthChangeOnAdaptationEntityEffectSystem : EntityEffectSystem<DamageableComponent, HealthChangeOnAdaptation>
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly ChemicalAdaptationSystem _chemicalAdaptation = default!;

    protected override void Effect(Entity<DamageableComponent> entity, ref EntityEffectEvent<HealthChangeOnAdaptation> args)
    {
        var damageSpec = new DamageSpecifier(args.Effect.Damage);

        damageSpec *= args.Scale;

        if (args.Effect.Reagent == null || !_chemicalAdaptation.TryGetMetabolized(entity, args.Effect.Reagent, out var metabolized))
            return;

        DamageSpecifier modifiedDamage = new DamageSpecifier();

        foreach (var (type, value) in damageSpec.DamageDict)
            modifiedDamage.DamageDict.Add(type, value + metabolized * args.Effect.Decay);

        _damageable.TryChangeDamage(
                entity,
                damageSpec,
                args.Effect.IgnoreResistances,
                interruptsDoAfters: false);
    }
}

/// <inheritdoc cref="EntityEffect"/>
public sealed partial class HealthChangeOnAdaptation : EntityEffectBase<HealthChangeOnAdaptation>
{
    /// <summary>
    /// Damage to apply every cycle. Damage Ignores resistances.
    /// </summary>
    [DataField(required: true)]
    public DamageSpecifier Damage = default!;

    [DataField]
    public bool IgnoreResistances = true;

    [DataField]
    public float Decay = 1;

    [DataField]
    public ProtoId<ReagentPrototype>? Reagent = null;

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        {
            var damages = new List<string>();
            var heals = false;
            var deals = false;

            var damageSpec = new DamageSpecifier(Damage);

            var universalReagentDamageModifier = entSys.GetEntitySystem<DamageableSystem>().UniversalReagentDamageModifier;
            var universalReagentHealModifier = entSys.GetEntitySystem<DamageableSystem>().UniversalReagentHealModifier;

            damageSpec = entSys.GetEntitySystem<DamageableSystem>().ApplyUniversalAllModifiers(damageSpec);

            foreach (var (kind, amount) in damageSpec.DamageDict)
            {
                var sign = FixedPoint2.Sign(amount);
                float mod;

                switch (sign)
                {
                    case < 0:
                        heals = true;
                        mod = universalReagentHealModifier;
                        break;
                    case > 0:
                        deals = true;
                        mod = universalReagentDamageModifier;
                        break;
                    default:
                        continue; // Don't need to show damage types of 0...
                }

                damages.Add(
                    Loc.GetString("health-change-display",
                        ("kind", prototype.Index<DamageTypePrototype>(kind).LocalizedName),
                        ("amount", MathF.Abs(amount.Float() * mod)),
                        ("deltasign", sign)
                    ));
            }

            var healsordeals = heals ? (deals ? "both" : "heals") : (deals ? "deals" : "none");

            return Loc.GetString("entity-effect-guidebook-health-change",
                ("chance", Probability),
                ("changes", ContentLocalizationManager.FormatList(damages)),
                ("healsordeals", healsordeals));
        }
}
