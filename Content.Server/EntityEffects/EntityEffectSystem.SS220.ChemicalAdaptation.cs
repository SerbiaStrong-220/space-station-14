// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.SS220.EntityEffects.Effects;
using Content.Shared.EntityEffects;
using Content.Shared.SS220.ChemicalAdaptation;

namespace Content.Server.EntityEffects;

public sealed partial class EntityEffectSystem
{
    [Dependency] private readonly SharedChemicalAdaptationSystem _adaptation = default!;

    private void OnChemicalAdaptation(ref ExecuteEntityEffectEvent<ChemicalAdaptationEffect> args)
    {
        if (args.Args is not EntityEffectReagentArgs reagentArgs)
            return;

        if (reagentArgs.Reagent is null)
            return;

        var modifier = args.Effect.Modifier;
        var duration = args.Effect.Duration;
        var refresh = args.Effect.Refresh;

        var chem = EnsureComp<ChemicalAdaptationComponent>(reagentArgs.TargetEntity);
        _adaptation.EnsureChemAdaptation((reagentArgs.TargetEntity, chem), reagentArgs.Reagent.ID, duration, modifier, refresh);

        DirtyEntity(reagentArgs.TargetEntity);//not sure about this one
    }
}
