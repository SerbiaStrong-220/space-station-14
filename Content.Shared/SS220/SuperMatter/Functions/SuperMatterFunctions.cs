// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using Content.Shared.Atmos;

namespace Content.Shared.SS220.SuperMatter.Functions;

/// <summary> Here goes functions which is request by SM Observer UI and SM server system </summary>
public static class SuperMatterFunctions
{
    public const float MatterNondimensionalization = 32f;
    public const float SuperMatterTriplePointTemperature = Atmospherics.T20C;
    public const float SuperMatterTriplePointPressure = Atmospherics.OneAtmosphere;

    public static SuperMatterPhaseState GetSuperMatterPhase(float temperature, float pressure)
    {
        if (temperature < SuperMatterTriplePointTemperature)
        {
            if (pressure < GetSingularityTeslaEquilibriumPressure(temperature))
                return SuperMatterPhaseState.TeslaRegion;

            return SuperMatterPhaseState.SingularityRegion;
        }
        if (temperature > SuperMatterTriplePointTemperature)
        {
            if (pressure < GetResonanceTeslaEquilibriumPressure(temperature))
                return SuperMatterPhaseState.TeslaRegion;
            if (pressure < GetSingularityResonanceEquilibriumPressure(temperature))
                return SuperMatterPhaseState.ResonanceRegion;
            return SuperMatterPhaseState.TeslaRegion;
        }
        return SuperMatterPhaseState.InertRegion;
    }

    public static float GetSingularityTeslaEquilibriumPressure(float temperature)
    {
        if (temperature >= SuperMatterTriplePointTemperature)
            return SuperMatterTriplePointPressure;
        if (temperature <= Atmospherics.MinimumTemperatureDeltaToConsider)
            return 0;

        return SingularityTeslaEquilibriumPressureFunction(temperature);
    }

    /// <summary> TODO desc </summary>
    /// <returns> Pressure value corresponded to SM Singularity-Resonance phase equilibrium as function of temperature </returns>
    public static float GetSingularityResonanceEquilibriumPressure(float temperature)
    {
        if (temperature <= SuperMatterTriplePointTemperature)
            return SuperMatterTriplePointPressure;

        if (temperature > Atmospherics.Tmax + Atmospherics.MinimumTemperatureDeltaToConsider)
            return SingularityResonanceEquilibriumPressureFunction(Atmospherics.Tmax);

        return SingularityResonanceEquilibriumPressureFunction(temperature);
    }

    /// <summary> TODO desc </summary>
    /// <returns> Pressure value corresponded to SM Resonance-Tesla phase equilibrium as function of temperature </returns>
    public static float GetResonanceTeslaEquilibriumPressure(float temperature)
    {
        if (temperature <= SuperMatterTriplePointTemperature)
            return SuperMatterTriplePointPressure;

        if (temperature > Atmospherics.Tmax + Atmospherics.MinimumTemperatureDeltaToConsider)
            return ResonanceTeslaEquilibriumPressureFunction(Atmospherics.Tmax);

        return ResonanceTeslaEquilibriumPressureFunction(temperature);
    }

    private const float SingularityTeslaEquilibriumCoeff = SuperMatterTriplePointPressure
                                / SuperMatterTriplePointTemperature / SuperMatterTriplePointTemperature;

    public static float SingularityTeslaEquilibriumPressureFunction(float temperature)
    {
        return SingularityTeslaEquilibriumCoeff * temperature * temperature;
    }
    // I have almost phD in it, so just relax and have fun
    private const float SingularityResonanceEquilibriumPressureOffset = 533.8f;
    private const float SingularityResonanceEquilibriumPressureCoeff = 1 / 700;
    private const float SingularityResonanceEquilibriumTemperatureOffsetFirst = -14.475f;
    private const float SingularityResonanceEquilibriumTemperatureOffsetSecond = 275.525f;

    public static float SingularityResonanceEquilibriumPressureFunction(float temperature)
    {
        return SingularityResonanceEquilibriumPressureOffset +
                SingularityResonanceEquilibriumPressureCoeff *
                    (temperature + SingularityResonanceEquilibriumTemperatureOffsetFirst) *
                        (temperature + SingularityResonanceEquilibriumTemperatureOffsetSecond);
    }

    private const float ResonanceTeslaEquilibriumPressureOffset = 573.8f;
    private const float ResonanceTeslaEquilibriumPressureCoeff = 5;
    private const float ResonanceTeslaEquilibriumTemperatureOffset = -117.475f;

    public static float ResonanceTeslaEquilibriumPressureFunction(float temperature)
    {
        return ResonanceTeslaEquilibriumPressureOffset + ResonanceTeslaEquilibriumPressureCoeff *
               (float) MathF.Pow(temperature + ResonanceTeslaEquilibriumTemperatureOffset, 0.7f);
    }

    //region Integrity
    private const float EnergyToMatterDamageFactorWide = 3500f;
    private const float EnergyToMatterDamageFactorCoeff = 2f;
    private const float EnergyToMatterDamageFactorOffset = 1f;

    public static float EnergyToMatterDamageFactorFunction(float delta, float matter)
    {
        return EnergyToMatterDamageFactorOffset - EnergyToMatterDamageFactorCoeff
                * MathF.Exp(-1 * MathF.Pow(delta / (EnergyToMatterDamageFactorWide * MassWideCoeff(matter)), 2));
    }

    private const float SafeInternalEnergyToMatterCoeff = 800f;
    private const float SafeInternalEnergyToMatterSlowerOffset = 50f;
    private static readonly float[] SafeModes = [1f, 4f, 8f];

    public record struct ModeSafeEnergy(int Mode, float ModeNumber, float Energy);

    public static ModeSafeEnergy[] SafeInternalEnergyToMatterFunction(float normalizedMatter)
    {
        var result = new ModeSafeEnergy[SafeModes.Length];
        for (var i = 0; i < SafeModes.Length; i++)
        {
            var safeEnergy = SafeInternalEnergyToMatterCoeff * SafeModes[i] * MathF.Pow(normalizedMatter, 1.5f)
                            / (normalizedMatter + SafeInternalEnergyToMatterSlowerOffset / MathF.Sqrt(SafeModes[i]));

            result[i] = new ModeSafeEnergy(i + 1, SafeModes[i], safeEnergy);
        }

        return result;
    }

    private const float MinimalWideCoeff = 0.2f;
    private const float MaxMassToAchieveMaxWide = 40f;

    public static float MassWideCoeff(float normalizedMatter)
    {
        if (normalizedMatter > MaxMassToAchieveMaxWide)
            return 1f;
        return MinimalWideCoeff + (1f - MinimalWideCoeff) * normalizedMatter / MaxMassToAchieveMaxWide;
    }

    // UI methods

    public static float GetIntegrityDamageMap(float matter, float internalEnergy)
    {
        var nonDimensionMatter = matter / MatterNondimensionalization;

        var safeInternalEnergyForModes = SafeInternalEnergyToMatterFunction(nonDimensionMatter);

        var delta = 0f;
        var minDistanceSq = float.MaxValue;
        foreach (var item in safeInternalEnergyForModes)
        {
            var currentDelta = item.Energy - internalEnergy;
            var deltaSquared = currentDelta * currentDelta;
            if (deltaSquared < minDistanceSq)
            {
                minDistanceSq = deltaSquared;
                delta = currentDelta;
            }
        }

        var damageFromDelta = EnergyToMatterDamageFactorFunction(delta, nonDimensionMatter);
        return damageFromDelta;
    }
}

public enum SuperMatterPhaseState
{
    ErrorState = -1,
    InertRegion,
    SingularityRegion,
    ResonanceRegion,
    TeslaRegion
}
