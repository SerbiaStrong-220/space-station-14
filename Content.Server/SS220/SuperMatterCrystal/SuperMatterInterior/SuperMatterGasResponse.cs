// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Atmos;
using Content.Server.SS220.SuperMatterCrystal.Components;

namespace Content.Server.SS220.SuperMatterCrystal;

public static class SuperMatterGasResponse
{
    /// <summary> Calculate result gas effect on SMs Matter Decaying rate </summary>
    /// <returns> number between -1 and 1. Which should be multiplied on base decay rate </returns>
    public static float GetRelativeGasesInfluenceToMatterDecay(SuperMatterComponent smComp, GasMixture gasMixture)
    {
        var resultRelativeInfluence = 0f;
        foreach (var gasId in smComp.DecayInfluenceGases.Keys)
        {
            var gasEfficiency = GetGasInfluenceEfficiency(gasId, gasMixture);
            resultRelativeInfluence = (resultRelativeInfluence + 1)
                    * (smComp.DecayInfluenceGases[gasId].RelativeInfluence * gasEfficiency + 1) - 1;
        }
        return resultRelativeInfluence;
    }
    public static float GetFlatGasesInfluenceToMatterDecay(SuperMatterComponent smComp, GasMixture gasMixture)
    {
        var resultFlatInfluence = 0f;
        foreach (var gasId in smComp.DecayInfluenceGases.Keys)
        {
            var gasEfficiency = GetGasInfluenceEfficiency(gasId, gasMixture);
            resultFlatInfluence += smComp.DecayInfluenceGases[gasId].flatInfluence * gasEfficiency;
        }
        return resultFlatInfluence;
    }
    /// <summary> Standalone method for easier  </summary>
    private static float GetGasInfluenceEfficiency(Gas gasId, GasMixture gasMixture)
    {
        return gasMixture.GetMoles(gasId) / gasMixture.TotalMoles;
    }
}
public struct GasEffectOnSuperMatter
{
}
