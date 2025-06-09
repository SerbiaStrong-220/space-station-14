// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.SS220.DarkForces.Saint.Reagent;
using Content.Shared.EntityEffects;
using Content.Shared.SS220.EntityEffects.EffectConditions;
using Content.Shared.SS220.EntityEffects.Effects;

namespace Content.Server.EntityEffects;

public sealed partial class EntityEffectSystem : EntitySystem
{
    private void InitializeSS220()
    {
        SubscribeLocalEvent<CheckEntityEffectConditionEvent<HasComponentsCondition>>(OnCheckComponentCondition);

        SubscribeLocalEvent<ExecuteEntityEffectEvent<ChemElixirOfLiberationEffect>>(OnExecuteChemElixirOfLiberation);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<ChemMiGomiceliumEffect>>(OnExecuteChemMiGomicelium);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<ChemRemoveHallucinationsEffect>>(OnExecuteChemRemoveHallucination);
        SubscribeLocalEvent<ExecuteEntityEffectEvent<SaintWaterDrinkEffect>>(OnExecuteSaintWaterDrinkEffect);
    }
}
