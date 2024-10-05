// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Atmos;
using Content.Server.SS220.SuperMatterCrystal.Components;
using Content.Shared.SS220.SuperMatter.Functions;

namespace Content.Server.SS220.SuperMatterCrystal;

public static class SuperMatterGasResponse
{
    /// <summary> Calculate result gas effect on SMs Matter Decaying rate </summary>
    /// <returns> number between -1 and 1. Which should be multiplied on base decay rate </returns>
    public static float GetRelativeGasesInfluenceToMatterDecay(SuperMatterComponent smComp, GasMixture gasMixture)
    {
        var resultRelativeInfluence = 0f;
        var decayInfluenceGases = SuperMatterGasInteraction.DecayInfluenceGases;
        if (decayInfluenceGases == null)
            return resultRelativeInfluence;
        foreach (var gasId in decayInfluenceGases.Keys)
        {
            var gasEfficiency = GetGasInfluenceEfficiency(gasId, gasMixture);
            resultRelativeInfluence = (resultRelativeInfluence + 1)
                    * (decayInfluenceGases[gasId].RelativeInfluence * gasEfficiency + 1) - 1;
        }
        return resultRelativeInfluence;
    }
    public static float GetFlatGasesInfluenceToMatterDecay(SuperMatterComponent smComp, GasMixture gasMixture)
    {
        var resultFlatInfluence = 0f;
        var decayInfluenceGases = SuperMatterGasInteraction.DecayInfluenceGases;
        if (decayInfluenceGases == null)
            return resultFlatInfluence;
        foreach (var gasId in decayInfluenceGases.Keys)
        {
            var gasEfficiency = GetGasInfluenceEfficiency(gasId, gasMixture);
            resultFlatInfluence += decayInfluenceGases[gasId].flatInfluence * gasEfficiency;
        }
        return resultFlatInfluence;
    }
    /// <summary>
    /// Checks all entrees in <see cref="SuperMatterGasInteraction.EnergyEfficiencyChangerGases"/>
    /// looks if we have it Mixture and calculate their efficiency
    /// </summary>
    /// <returns> Value from -0.5 to 1 </returns>
    public static float GetGasInfluenceReleaseEnergyEfficiency(GasMixture gasMixture)
    {
        var resultEfficiency = 0f;
        var affectGases = SuperMatterGasInteraction.EnergyEfficiencyChangerGases;
        if (affectGases == null)
            return resultEfficiency;
        foreach (var gasId in affectGases.Keys)
        {
            resultEfficiency += affectGases[gasId].RelativeInfluence
                    * GetNonLinierGasInfluenceEfficiencyFunction(gasId, gasMixture, affectGases[gasId].OptimalRatio);
        }

        return MathF.Max(Math.Min(resultEfficiency, 1f), -0.5f);
    }

    /// <summary> Standalone method for getting molar part  </summary>
    private static float GetGasInfluenceEfficiency(Gas gasId, GasMixture gasMixture)
    {
        if (gasMixture.TotalMoles == 0)
            return 0;
        return gasMixture.GetMoles(gasId) / gasMixture.TotalMoles;
    }
    private static float GetNonLinierGasInfluenceEfficiencyFunction(Gas gasId, GasMixture gasMixture, float optimalRatio)
    {
        var maxValue = MathF.Pow(optimalRatio, 2) * MathF.Exp(-2);
        var optParam = 2 / optimalRatio;
        // same function soo its okay
        var normalizedMoles = GetGasInfluenceEfficiency(gasId, gasMixture);

        return MathF.Pow(normalizedMoles, 2) * MathF.Exp(-optParam * normalizedMoles) / maxValue;
    }
}
