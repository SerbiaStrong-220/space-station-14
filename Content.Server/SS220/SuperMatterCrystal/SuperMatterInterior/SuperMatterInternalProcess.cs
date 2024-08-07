// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.SS220.SuperMatterCrystal.Components;
using Content.Shared.Atmos;

namespace Content.Server.SS220.SuperMatterCrystal;
/// <summary> TODO DESC </summary>
public static class SuperMatterInternalProcess
{
    private const float TriplePointTemperature = SuperMatterPhaseDiagram.SuperMatterTriplePointTemperature;
    private const float TriplePointPressure = SuperMatterPhaseDiagram.SuperMatterTriplePointPressure;
    ///<summary> TODO desc </summary>
    ///<returns> Return parrots value of  Decay multiplier </returns>
    public static float GetDecayMatterMultiplier(float temperature, float pressure)
    {
        var decayMatterMultiplierInTriplePoint = DecayMatterMultiplierFunction(TriplePointTemperature, TriplePointPressure);

        if (temperature > Atmospherics.Tmax + Atmospherics.MinimumTemperatureDeltaToConsider)
            return DecayMatterMultiplierFunction(Atmospherics.Tmax, pressure);

        return DecayMatterMultiplierFunction(temperature, pressure);
    }
    ///<summary> Use some model function to define which part of gas moles will convert to matter </summary>
    ///<returns> Return value from 0 to 1 </returns>
    public static float GetMolesReactionEfficiency(float temperature, float pressure)
    {
        var resultRatio = MolesReactionEfficiencyFunction(temperature, pressure);
        resultRatio = MathF.Max(resultRatio, 0f);
        resultRatio = MathF.Min(resultRatio, 1f);

        return resultRatio;
    }
    /// <summary> Lets make SM spicy with it, basically with part make it unstable but who knows </summary>
    public static float GetDeltaChemistryPotential(float temperature, float pressure)
    {
        // maybe compress value here?
        return DeltaChemistryPotentialFunction(temperature, pressure);
    }
    /// <summary> Defines how many J you need to raise the temperature to 1 grad </summary>
    /// <returns> heat capacity in J/K </returns>
    public static float GetHeatCapacity(float temperature, float matter)
    {
        if (temperature < Atmospherics.Tmax / 50)
            return 11 / 2 * Atmospherics.R * matter;
        if (temperature < Atmospherics.Tmax / 10)
            return 15 / 2 * Atmospherics.R * matter;
        return 27 / 2 * Atmospherics.R * matter;
    }
    /// <summary>  </summary>
    /// <returns> Value from 0.0002 to 0.03 </returns>
    public static float GetReleaseEnergyConversionEfficiency(float temperature, float pressure)
    {
        var resultEfficiency = ReleaseEnergyConversionEfficiencyFunction(temperature, pressure);

        return MathF.Max(Math.Min(resultEfficiency, 0.03f), 0.0002f);
    }
    public static float GetZapToRadiationRatio(float temperature, float pressure, SuperMatterPhaseState smState)
    {
        var normalizedTemperature = temperature / TriplePointTemperature;
        var normalizedPressure = pressure / TriplePointPressure;

        var resultZapToRadiationRatio = smState switch
        {
            SuperMatterPhaseState.SingularityRegion => 1 - ZapToRadiationRatioFunction(normalizedPressure),
            SuperMatterPhaseState.TeslaRegion => ZapToRadiationRatioFunction(normalizedTemperature),
            SuperMatterPhaseState.ResonanceRegion => ZapToRadiationRatioFunction(normalizedTemperature + normalizedPressure) / 2,
            _ => 0.5f,
        };

        return resultZapToRadiationRatio;
    }
    private const float O2ToPlasmaBaseRatio = 0.9f;
    private const float O2ToPlasmaFloatRatio = 1.7f;
    private const float RatioValueOffset = 0.44f;
    private const float SqrtOfMaxRatioValue = 1.2f;
    /// <summary> MADE IT! </summary>
    /// <returns></returns>
    public static float GetOxygenToPlasmaRatio(float temperature, float pressure, SuperMatterPhaseState smState)
    {
        var ratio = GetZapToRadiationRatio(temperature, pressure, smState);
        return (O2ToPlasmaBaseRatio + O2ToPlasmaFloatRatio * MathF.Sqrt(ratio+RatioValueOffset))
                            / (O2ToPlasmaBaseRatio + O2ToPlasmaFloatRatio * SqrtOfMaxRatioValue);
    }

    private static float DecayMatterMultiplierFunction(float temperature, float pressure)
    {
        return DecayMatterCombinedFactorFunction(temperature, pressure)
                + DecayMatterTemperatureFactorFunction(temperature);
    }
    private const float TemperatureFactorNormalizedTemperatureOffset = 20f;
    private static float DecayMatterTemperatureFactorFunction(float temperature)
    {
        var normalizedTemperature = temperature / TriplePointTemperature;

        return normalizedTemperature / (normalizedTemperature + TemperatureFactorNormalizedTemperatureOffset);
    }
    private const float CombinedFactorCoeff = 0.1f;
    private const float CombinedFactorSlowerNormalizedTemperatureOffset = 10f;
    private const float CombinedFactorSlowerNormalizedPressureOffset = 10f;
    private static float DecayMatterCombinedFactorFunction(float temperature, float pressure)
    {
        var normalizedTemperature = temperature / TriplePointTemperature;
        var normalizedPressure = pressure / TriplePointPressure;

        return CombinedFactorCoeff * normalizedTemperature * normalizedTemperature
                    * normalizedPressure * normalizedPressure
                    / (normalizedPressure + normalizedTemperature)
                    / (normalizedPressure + CombinedFactorSlowerNormalizedPressureOffset)
                    / (normalizedTemperature + CombinedFactorSlowerNormalizedTemperatureOffset);
    }

    private const float ReactionEfficiencyCoeff = 0.05f;
    private const float ReactionEfficiencySlowerNormalizedTemperatureOffset = 60f;
    private const float ReactionEfficiencySlowerNormalizedPressureOffset = 80f;
    private const float ReactionEfficiencyTemperatureCoeff = 0.5f;
    private static float MolesReactionEfficiencyFunction(float temperature, float pressure)
    {
        var normalizedTemperature = temperature / TriplePointTemperature;
        var normalizedPressure = pressure / TriplePointPressure;

        return ReactionEfficiencyCoeff
                    * (temperature / (temperature + ReactionEfficiencySlowerNormalizedTemperatureOffset) * ReactionEfficiencyTemperatureCoeff
                    + pressure / (pressure + ReactionEfficiencySlowerNormalizedPressureOffset));
    }

    private const float ChemistryPotentialCoeff = 4f;
    private const float ChemistryPotentialCombinedStretchCoeff = 200;
    private static float DeltaChemistryPotentialFunction(float temperature, float pressure)
    {
        var normalizedTemperature = temperature / TriplePointTemperature;
        var normalizedPressure = pressure / TriplePointPressure;
        var normalizedCombined = normalizedPressure * normalizedTemperature / ChemistryPotentialCombinedStretchCoeff;

        return ChemistryPotentialCoeff * MathF.Pow(normalizedCombined, 2) * MathF.Exp(MathF.Pow(normalizedCombined, 2));
    }

    private const float ReleaseEnergyConversionEfficiencyCoeff = 0.01f;
    private const float ReleaseEnergyConversionEfficiencyNormalizedCombinedCoeff = 0.0025f;
    private const float ReleaseEnergyConversionEfficiencyNormalizedTemperatureOffset = 20f;
    private const float ReleaseEnergyConversionEfficiencySlowerNormalizedPressureOffset = 80f;
    private static float ReleaseEnergyConversionEfficiencyFunction(float temperature, float pressure)
    {
        var normalizedTemperature = temperature / TriplePointTemperature;
        var normalizedPressure = pressure / TriplePointPressure;
        var normalizedCombined = normalizedPressure * normalizedTemperature / ChemistryPotentialCombinedStretchCoeff;

        return ReleaseEnergyConversionEfficiencyCoeff * (temperature / (temperature + ReleaseEnergyConversionEfficiencyNormalizedTemperatureOffset)
                                    + pressure / (pressure + ReleaseEnergyConversionEfficiencySlowerNormalizedPressureOffset)
                                    + MathF.Sqrt(normalizedCombined) * ReleaseEnergyConversionEfficiencyNormalizedCombinedCoeff);
    }

    private const float ZapToRadiationRatioOffset = 0.6f;
    private const float ZapToRadiationRatioCoeff = 0.017f;
    private static float ZapToRadiationRatioFunction(float normalizedValue)
    {
        return ZapToRadiationRatioOffset + ZapToRadiationRatioCoeff * MathF.Sqrt(normalizedValue);
    }
}
