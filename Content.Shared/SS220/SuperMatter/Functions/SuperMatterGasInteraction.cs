// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Atmos;

namespace Content.Shared.SS220.SuperMatter.Functions;
public struct SuperMatterGasInteraction
{
    public static Dictionary<Gas, (float RelativeInfluence, float flatInfluence)>? DecayInfluenceGases;
    // It is used to define how much matter will be added if 1 mole of gas consumed
    public static Dictionary<Gas, float>? GasesToMatterConvertRatio;
    public static Dictionary<Gas, (float OptimalRatio, float RelativeInfluence)>? EnergyEfficiencyChangerGases;
}
