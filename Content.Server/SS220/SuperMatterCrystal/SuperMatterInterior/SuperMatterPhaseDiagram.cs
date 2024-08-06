// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Atmos;

namespace Content.Server.SS220.SuperMatterCrystal;

// TODO Draw it in guide of SM
public static class SuperMatterPhaseDiagram
{
    public const float SuperMatterTriplePointTemperature = Atmospherics.T20C;
    public const float SuperMatterTriplePointPressure = Atmospherics.OneAtmosphere;

    /// <summary> TODO </summary>
    /// <returns> Pressure value corresponded to SM Singularity-Tesla phase equilibrium as function of temperature </returns>
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
    private static float SingularityTeslaEquilibriumPressureFunction(float temperature)
    {
        return SingularityTeslaEquilibriumCoeff * temperature * temperature;
    }
    // I have almost phD in it, so just relax and have fun
    private const float SingularityResonanceEquilibriumPressureOffset = 533.8f;
    private const float SingularityResonanceEquilibriumPressureCoeff = 1 / 700;
    private const float SingularityResonanceEquilibriumTemperatureOffsetFirst = -14.475f;
    private const float SingularityResonanceEquilibriumTemperatureOffsetSecond = 275.525f;
    private static float SingularityResonanceEquilibriumPressureFunction(float temperature)
    {
        return SingularityResonanceEquilibriumPressureOffset +
                SingularityResonanceEquilibriumPressureCoeff *
                    (temperature + SingularityResonanceEquilibriumTemperatureOffsetFirst) *
                        (temperature + SingularityResonanceEquilibriumTemperatureOffsetSecond);
    }
    private const float ResonanceTeslaEquilibriumPressureOffset = 573.8f;
    private const float ResonanceTeslaEquilibriumPressureCoeff = 5;
    private const float ResonanceTeslaEquilibriumTemperatureOffset = -117.475f;
    private static float ResonanceTeslaEquilibriumPressureFunction(float temperature)
    {
        return ResonanceTeslaEquilibriumPressureOffset + ResonanceTeslaEquilibriumPressureCoeff *
               (float) MathF.Pow(temperature + ResonanceTeslaEquilibriumTemperatureOffset, 0.7f);
    }
}
