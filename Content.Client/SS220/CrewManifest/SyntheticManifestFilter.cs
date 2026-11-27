// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.CrewManifest;

namespace Content.Client.SS220.CrewManifest;

/// <summary>
///     Filters synthetic manifest entries by a search text.
/// </summary>
public static class SyntheticManifestFilter
{
    public static SyntheticManifestEntry[] Filter(SyntheticManifestEntry[] entries, string text)
    {
        var result = new List<SyntheticManifestEntry>();
        foreach (var entry in entries)
        {
            if (entry.Name.Contains(text, StringComparison.OrdinalIgnoreCase)
                || entry.ChassisName.Contains(text, StringComparison.OrdinalIgnoreCase))
            {
                result.Add(entry);
            }
        }

        return result.ToArray();
    }
}

