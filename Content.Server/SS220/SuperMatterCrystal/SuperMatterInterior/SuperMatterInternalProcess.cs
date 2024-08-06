// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
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
}
