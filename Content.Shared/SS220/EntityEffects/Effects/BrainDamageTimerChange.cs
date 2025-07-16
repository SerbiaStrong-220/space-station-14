// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.LimitationRevive;
using Content.Shared.SS220.ChemicalAdaptation;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

/// <summary>
/// Narrowly targeted effect to increase time to brain damage.
/// Uses ChemicalAdaptation to reduce the effectiveness of use
/// </summary>

public sealed partial class BrainDamageTimerChange : EntityEffect
{
    /// <summary>
    /// How long will brain damage be delayed with one assimilation of the reagent?
    /// </summary>
    [DataField(required: true)]
    public TimeSpan ProlongedTime;

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (args is not EntityEffectReagentArgs reagentArgs)
            return;

        if (reagentArgs.Reagent is null)
            return;

        if (!args.EntityManager.TryGetComponent<LimitationReviveComponent>(args.TargetEntity, out var limitComp))
            return;

        if (limitComp.DamageTime is null)
            return;

        var mod = 1f;//buffer in case we don't have a modifier

        if (args.EntityManager.TryGetComponent<ChemicalAdaptationComponent>(args.TargetEntity, out var adaptComp) &&
            adaptComp.ChemicalAdaptations.TryGetValue(reagentArgs.Reagent.ID, out var value))
	    mod = value.Modifier;

        limitComp.DamageTime += ProlongedTime * mod;//not sure, maybe it should be function. but then you'll have to move a lot to shared...
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("reagent-effect-guidebook-ss220-brain-damage-slow", ("chance", Probability));
    }
}
