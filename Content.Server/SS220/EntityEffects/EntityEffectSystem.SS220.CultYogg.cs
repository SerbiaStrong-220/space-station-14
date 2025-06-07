// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.SS220.CultYogg.Cultists;
using Content.Shared.SS220.EntityEffects.Effects;
using Content.Shared.SS220.CultYogg.Cultists;
using Content.Shared.EntityEffects;
using Content.Shared.Humanoid;
using Content.Server.SS220.CultYogg;
using Content.Shared.SS220.EntityEffects;
using Content.Server.SS220.DarkForces.Saint.Reagent;

namespace Content.Server.EntityEffects;

public sealed partial class EntityEffectSystem : EntitySystem
{
    [Dependency] private readonly CultYoggSystem _cultYogg = default!;
    [Dependency] private readonly CultYoggAnimalCorruptionSystem _cultYoggAnimalCorruption = default!;

    private void OnExecuteChemMiGomicelium(ref ExecuteEntityEffectEvent<ChemMiGomiceliumEffect> args)
    {
        var targetEntity = args.Args.TargetEntity;

        if (args.Args is EntityEffectReagentArgs reagentArgs)
        {
            if (TryComp<CultYoggComponent>(targetEntity, out var comp))
            {
                RemComp<CultYoggPurifiedComponent>(targetEntity);

                comp.ConsumedAscensionReagent += reagentArgs.Quantity.Float();
                _cultYogg.TryStartAscensionByReagent(targetEntity, comp);
                return;
            }
        }

        if (!HasComp<HumanoidAppearanceComponent>(targetEntity)) //if its an animal -- corrupt it
        {
            _cultYoggAnimalCorruption.AnimalCorruption(targetEntity);
        }
    }

    private void OnExecuteChemElixirOfLiberation(ref ExecuteEntityEffectEvent<ChemElixirOfLiberationEffect> args)
    {
        if (TryComp<CultYoggComponent>(args.Args.TargetEntity, out var comp))
            _cultYogg.NullifyShroomEffect(args.Args.TargetEntity, comp);

    }

    private void OnExecuteChemRemoveHallucination(ref ExecuteEntityEffectEvent<ChemRemoveHallucinationsEffect> args)
    {
        if (args.Args is not EntityEffectReagentArgs reagentArgs)
            return;

        var ev = new OnChemRemoveHallucinationsEvent();
        RaiseLocalEvent(reagentArgs.TargetEntity, ev);
    }

    private void OnExecuteSaintWaterDrinkEffect(ref ExecuteEntityEffectEvent<SaintWaterDrinkEffect> args)
    {
        if (args.Args is not EntityEffectReagentArgs reagentArgs)
            return;

        var saintWaterDrinkEvent = new OnSaintWaterDrinkEvent(reagentArgs.TargetEntity, reagentArgs.Quantity);
        RaiseLocalEvent(reagentArgs.TargetEntity, saintWaterDrinkEvent);
    }
}
