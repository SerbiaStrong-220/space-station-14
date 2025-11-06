// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.EntityEffects;
using Content.Shared.SS220.ChemicalAdaptation;

namespace Content.Shared.SS220.EntityEffects.Effects;

public sealed partial class MakeSentientEntityEffectSystem : EntityEffectSystem<MetaDataComponent, ChemicalAdaptationEffect>
{
    [Dependency] private readonly ChemicalAdaptationSystem _adaptation = default!;

    protected override void Effect(Entity<MetaDataComponent> entity, ref EntityEffectEvent<ChemicalAdaptationEffect> args)
    {
        var modifier = args.Effect.Modifier;
        var duration = args.Effect.Duration;
        var refresh = args.Effect.Refresh;

        var chem = EnsureComp<ChemicalAdaptationComponent>(entity);
        _adaptation.EnsureChemAdaptation((entity, chem), args.Effect.Reagent, duration, modifier, refresh);

        DirtyEntity(entity);//not sure about this one
    }

}
