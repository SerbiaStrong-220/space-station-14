// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.SS220.ChemicalAdaptation;
using Content.Shared.EntityEffects;
using Content.Server.SS220.EntityEffects.Effects;

namespace Content.Server.EntityEffects;

public sealed partial class EntityEffectSystem : EntitySystem
{
    [Dependency] private readonly ChemicalAdaptation _adaptation = default!;
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
        _adaptation.EnsureChemAdaptation(chem, reagentArgs.Reagent.ID, duration, modifier, refresh);
    }
}
