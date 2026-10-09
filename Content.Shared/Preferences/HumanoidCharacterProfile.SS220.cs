// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Text.RegularExpressions;
using Content.Shared.Humanoid.Prototypes;

namespace Content.Shared.Preferences;

public sealed partial class HumanoidCharacterProfile
{
    private static readonly Regex LatinRestrictedNameRegex = new(@"[^A-Za-zА-Яа-яёЁ0-9.' -]");

    private static string SanitizeRestrictedName(SpeciesPrototype species, string name)
    {
        return species.LatinNamesAllowed
            ? LatinRestrictedNameRegex.Replace(name, string.Empty)
            : RestrictedNameRegex.Replace(name, string.Empty);
    }
}

