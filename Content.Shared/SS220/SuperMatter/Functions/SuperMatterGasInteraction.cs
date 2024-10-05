// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using System.Collections.Frozen;
using Content.Shared.Atmos;

namespace Content.Shared.SS220.SuperMatter.Functions;
public struct SuperMatterGasInteraction
{
    public static FrozenDictionary<Gas, (float RelativeInfluence, float flatInfluence)> DecayInfluenceGases = new Dictionary<Gas, (float RelativeInfluence, float flatInfluence)> ()
        {
            {Gas.Oxygen, (0.6f, -4f) },
            {Gas.Frezon, (0.1f, 0f) },
            {Gas.Ammonia, (2.2f, 4f)}
        }.ToFrozenDictionary();

    public static FrozenDictionary<Gas, float> GasesToMatterConvertRatio = new Dictionary<Gas, float>()
        {
            {Gas.Tritium, 6f},
            {Gas.NitrousOxide, 2f},
        }.ToFrozenDictionary();

    public static FrozenDictionary<Gas, (float OptimalRatio, float RelativeInfluence)> EnergyEfficiencyChangerGases = new Dictionary<Gas, (float OptimalRatio, float RelativeInfluence)> ()
        {
            {Gas.Nitrogen,  (0.8f, -0.4f)},
            {Gas.Ammonia, (0.7f, 0.8f)},
            {Gas.Frezon, (0.2f, -0.8f)},
            {Gas.NitrousOxide, (0.6f, -0.6f)}
        }.ToFrozenDictionary();
}
