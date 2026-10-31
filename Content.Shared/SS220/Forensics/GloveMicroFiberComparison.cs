// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Forensics;

/// <summary>
/// Matches individual source codes for both the analyzer display and its printed comparison report.
/// </summary>
public static class GloveMicroFiberComparison
{
    /// <summary>
    /// Returns one glove trace per shared ordinal code, retaining the first sample's order and appearance.
    /// </summary>
    public static List<GloveMicroFiberTrace> FindGloveMatches(
        List<GloveMicroFiberTrace> first,
        List<GloveMicroFiberTrace> second)
    {
        return FindMatches(first, second, static trace => trace.Code);
    }

    /// <summary>
    /// Returns one storage trace per shared ordinal code, retaining the first sample's order and appearance.
    /// </summary>
    public static List<StorageMicroFiberTrace> FindStorageMatches(
        List<StorageMicroFiberTrace> first,
        List<StorageMicroFiberTrace> second)
    {
        return FindMatches(first, second, static trace => trace.Code);
    }

    private static List<T> FindMatches<T>(List<T> first, List<T> second, Func<T, string> getCode)
    {
        HashSet<string> remaining = new(StringComparer.Ordinal);
        foreach (var trace in second)
        {
            remaining.Add(getCode(trace));
        }

        List<T> matches = [];
        foreach (var trace in first)
        {
            if (!remaining.Remove(getCode(trace)))
                continue;

            matches.Add(trace);
        }

        return matches;
    }
}
