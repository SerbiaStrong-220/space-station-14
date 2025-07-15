// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.SS220.LimitationRevive;
using Content.Shared.SS220.ChemicalAdaptation;
using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects;

public sealed partial class BrainDamageTimerChange : EntityEffect
{
    /// <summary>
    /// </summary>
    [DataField(required: true)]
    public TimeSpan ProlongedTime;

    public override void Effect(EntityEffectBaseArgs args)
    {
        if (args is not EntityEffectReagentArgs reagentArgs)
            return;

        if (reagentArgs.Reagent is null)
            return;

        if (!args.EntityManager.TryGetComponent<LimitationReviveComponent>(args.TargetEntity, out var limitComp))//ToDo_SS220 ask Kirus should we move this component into shared
            return;

        if (limitComp.DamageTime is null)
            return;

        var mod = 1f;

        if (args.EntityManager.TryGetComponent<ChemicalAdaptationComponent>(args.TargetEntity, out var adaptComp))
            if (adaptComp.ChemicalAdaptations.TryGetValue(reagentArgs.Reagent.ID, out var value))
                mod = value.Modifier;

        limitComp.DamageTime += ProlongedTime * mod;
    }

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return Loc.GetString("reagent-effect-guidebook-ss220-free-from-burden", ("chance", Probability));//ToDo_SS220 write smth here
    }
}
