// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Server.Antag.Components;

public sealed partial class AntagSelectionComponent
{
    /// <summary>Unfilled seats released by administrators, indexed by the original definition.</summary>
    public readonly SortedDictionary<int, int> AdminFreedSlots = new();
}
