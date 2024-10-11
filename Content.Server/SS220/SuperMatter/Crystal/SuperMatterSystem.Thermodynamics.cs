// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Diagnostics.CodeAnalysis;
using Content.Server.SS220.SuperMatterCrystal.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.SS220.SuperMatter.Functions;
using Content.Shared.Atmos;
using Robust.Shared.Random;

namespace Content.Server.SS220.SuperMatterCrystal;

public sealed partial class SuperMatterSystem : EntitySystem
{
    /* TODOs:
        [ ] Make Internal log of starting SM
        [ ] Think of "Helps us prevent cases when someone dumps superhothotgas into the SM and shoots the power to the moon for one tick." (c) TG

    */

    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;

    private float GetDecayMatterMultiplier(float temperature, float pressure) => SuperMatterInternalProcess.GetDecayMatterMultiplier(temperature, pressure);
    private float GetMolesReactionEfficiency(float temperature, float pressure) => SuperMatterInternalProcess.GetMolesReactionEfficiency(temperature, pressure);
    private float GetDeltaChemistryPotential(float temperature, float pressure) => SuperMatterInternalProcess.GetDeltaChemistryPotential(temperature, pressure);
    private float GetChemistryPotential(float temperature, float pressure) => CHEMISTRY_POTENTIAL_BASE + GetDeltaChemistryPotential(temperature, pressure);
    private float GetHeatCapacity(float temperature, float matter) => SuperMatterInternalProcess.GetHeatCapacity(temperature, matter);
    private float GetReleaseEnergyConversionEfficiency(float temperature, float pressure) => SuperMatterInternalProcess.GetReleaseEnergyConversionEfficiency(temperature, pressure);
    private float GetZapToRadiationRatio(float temperature, float pressure, SuperMatterPhaseState smState) => SuperMatterInternalProcess.GetZapToRadiationRatio(temperature, pressure, smState);
    private float GetOxygenToPlasmaRatio(float temperature, float pressure, SuperMatterPhaseState smState) => SuperMatterInternalProcess.GetOxygenToPlasmaRatio(temperature, pressure, smState);
    private float GetRelativeGasesInfluenceToMatterDecay(SuperMatterComponent smComp, GasMixture gasMixture) => SuperMatterGasResponse.GetRelativeGasesInfluenceToMatterDecay(smComp, gasMixture);
    private float GetFlatGasesInfluenceToMatterDecay(SuperMatterComponent smComp, GasMixture gasMixture) => SuperMatterGasResponse.GetFlatGasesInfluenceToMatterDecay(smComp, gasMixture);

    public const float MatterNondimensionalization = SuperMatterFunctions.MatterNondimensionalization; // like C mass in Mendeleev table
    public const float CHEMISTRY_POTENTIAL_BASE = 12f; // parrots now, but need to concrete in future
    public const float MATTER_DECAY_BASE_RATE = 85f; // parrots now, but need to concrete in future
    /// <summary> Defines how fast SM gets in thermal equilibrium with gas in it. Do not make it greater than 1! </summary>
    public const float SM_HEAT_TRANSFER_RATIO = 0.07f;

    private const float RestructureProbability = 0.0002f; // remember that we have about 30 times trying it in a second. 2e-4 is like one chance in 3 minutes.
    private const float RestructureAdditionalMatterDimensionLess = 42f;

    private void EvaluateDeltaInternalEnergy(Entity<SuperMatterComponent> crystal, GasMixture gasMixture, float frameTime)
    {
        var (crystalUid, smComp) = crystal;
        var chemistryPotential = GetChemistryPotential(smComp.Temperature, gasMixture.Pressure);
        var crystalHeatFromGas = _atmosphere.GetThermalEnergy(gasMixture) * SM_HEAT_TRANSFER_RATIO
                                    * (gasMixture.Temperature - smComp.Temperature)
                                    / MathF.Max(gasMixture.Temperature, smComp.Temperature);
        var smDeltaT = crystalHeatFromGas / GetHeatCapacity(smComp.Temperature, smComp.Matter);
        var normalizedMatter = smComp.Matter / MatterNondimensionalization;
        // here we start to change mix in SM cause nothing else depends on it after
        var deltaMatter = SynthesizeMatterFromGas(crystal, gasMixture, frameTime, deleteUsedGases: true) - CalculateDecayedMatter(crystal, gasMixture);
        var normalizedDeltaMatter = deltaMatter / MatterNondimensionalization;

        var matterToTemperatureRatio = normalizedMatter / smComp.Temperature;
        var newMatterToTemperatureRatio = (normalizedMatter + normalizedDeltaMatter) / (smComp.Temperature + smDeltaT);
        // here we connect chemistry potential with internal energy, so thought of their units adequate, maybe even calculate it
        var deltaInternalEnergy = (smDeltaT * (matterToTemperatureRatio - newMatterToTemperatureRatio) * smComp.Temperature / Atmospherics.T0C
                                    - chemistryPotential * normalizedDeltaMatter)
                                    / (1 + newMatterToTemperatureRatio * smDeltaT);
        deltaInternalEnergy = Math.Clamp(deltaInternalEnergy, -SuperMatterComponent.MinimumInternalEnergy / 5f, SuperMatterComponent.MinimumInternalEnergy / 5f);
        smComp.InternalEnergy += deltaInternalEnergy * frameTime;

        if (smComp.InternalEnergy == SuperMatterComponent.MinimumInternalEnergy
            && _robustRandom.Prob(RestructureProbability))
        {
            smComp.Matter += _robustRandom.GetRandom().NextFloat(0.7f, 1.3f) * RestructureAdditionalMatterDimensionLess * MatterNondimensionalization;
            smComp.InternalEnergy = GetSafeInternalEnergyToMatterValue(crystal.Comp.Matter) * _robustRandom.GetRandom().NextFloat(0.7f, 1.3f);
            _popupSystem.PopupEntity(Loc.GetString("supermatter-crystal-restructure"), crystalUid);

            smComp.Integrity = MathF.Max(smComp.Integrity * 0.8f, 0.1f);
            smComp.Temperature *= 0.6f;
        }

        smComp.Matter = smComp.Matter + deltaMatter * frameTime;
        smComp.Temperature = smComp.Temperature + smDeltaT * frameTime;
        _atmosphere.AddHeat(gasMixture, -crystalHeatFromGas * frameTime);
    }
    /// <summary> We dont apply it to Matter field of SMComp because we need this value in internal energy evaluation </summary>
    private float CalculateDecayedMatter(Entity<SuperMatterComponent> crystal, GasMixture gasMixture)
    {
        var (_, smComp) = crystal;
        var gasEffectMultiplier = GetRelativeGasesInfluenceToMatterDecay(smComp, gasMixture);
        var gasFlatInfluence = GetFlatGasesInfluenceToMatterDecay(smComp, gasMixture);

        /// gas effect multiplier should affects only Base decay rate, f.e. for gases which mostly occupy SM decay
        var environmentMultiplier = GetDecayMatterMultiplier(smComp.Temperature, gasMixture.Pressure);
        environmentMultiplier = Math.Min(environmentMultiplier, 20f); // cut off enormous numbers, our goal fun not overwhelm

        return (MATTER_DECAY_BASE_RATE * (gasEffectMultiplier + 1) + gasFlatInfluence) * environmentMultiplier;
    }
    /// <summary> Calculate how much matter will be added this step
    ///  and distract used gas from its inner gasMixture if deleteUsedGases true
    /// We dont apply it to Matter field of SMComp because we need this value in internal energy evaluation </summary>
    private float SynthesizeMatterFromGas(Entity<SuperMatterComponent> crystal, GasMixture gasMixture, float frameTime, bool deleteUsedGases = false)
    {
        var (_, smComp) = crystal;
        var resultAdditionalMatter = 0f;
        var gasesToMatterConvertRatio = SuperMatterGasInteraction.GasesToMatterConvertRatio;

        if (gasesToMatterConvertRatio == null)
            return resultAdditionalMatter;

        foreach (var gasId in gasesToMatterConvertRatio.Keys)
        {
            var gasMolesInReact = gasMixture.GetMoles(gasId)
                                    * GetMolesReactionEfficiency(smComp.Temperature, gasMixture.Pressure);

            resultAdditionalMatter += gasMolesInReact * gasesToMatterConvertRatio[gasId];

            if (deleteUsedGases)
                gasMixture.AdjustMoles(gasId, -gasMolesInReact * frameTime);
        }

        return resultAdditionalMatter;
    }
    /// <summary>
    /// In future maybe useful if a need to make/init own SM gasStructs
    /// </summary>
    private bool TryGetCrystalGasMixture(EntityUid crystalUid, [NotNullWhen(true)] out GasMixture? gasMixture)
    {
        gasMixture = _atmosphere.GetContainingMixture(crystalUid, true, true);
        if (gasMixture == null)
            return false;
        return true;
    }
}
