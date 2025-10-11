// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.ChemicalAdaptation;
using Content.Shared.SS220.LimitationRevive;
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
    public TimeSpan AddTime;

    public override void Effect(EntityEffectBaseArgs args)
    {

        var limReviveSys = args.EntityManager.EntitySysManager.GetEntitySystem<SharedLimitationReviveSystem>();
        var chemAdaptSys = args.EntityManager.EntitySysManager.GetEntitySystem<SharedChemicalAdaptationSystem>();

        if (args is not EntityEffectReagentArgs reagentArgs)
            return;

        if (reagentArgs.Reagent is null)
            return;

        var timeBuffer = AddTime;

        if (chemAdaptSys.TryGetModifier(args.TargetEntity, reagentArgs.Reagent.ID, out var modifier))
        {
            timeBuffer *= modifier;
        }

        limReviveSys.IncreaseTimer(args.TargetEntity, timeBuffer);
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("reagent-effect-guidebook-ss220-brain-damage-slow", ("time", AddTime.TotalSeconds));
    }
}
