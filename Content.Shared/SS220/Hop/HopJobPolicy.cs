// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
namespace Content.Shared.SS220.Hop;

/// <summary>Rules shared by the console and its regression tests. Missing slots are not unlimited slots.</summary>
public static class HopJobPolicy
{
    public const int MaxSlots = 10;

    public static bool CanBeUnlimited(string jobId) => jobId == "Passenger";

    /// <summary>
    /// The cap applies to occupied + available slots, not to each individual console write.
    /// Integer division rounds the permitted 50% increase down. Null is an explicitly unlimited map setup.
    /// </summary>
    public static int MaximumAvailable(int? baseline, int occupied)
    {
        var total = baseline is { } value ? (long) value / 2 : MaxSlots;
        return (int)Math.Clamp(total - Math.Max(occupied, 0), 0, MaxSlots);
    }

    public static bool CanSetSlots(string jobId, int? requested, int? current, int maximum, bool locked)
    {
        // Permit a priority-only edit even when an event or admin has put slots outside the console's bounds.
        if (requested == current)
            return true;
        if (locked || !ValidSlots(requested))
            return false;
        return requested == null ? CanBeUnlimited(jobId) : requested <= maximum;
    }

    public static bool IsCrewJob(bool selectable, int[]? setup, bool hasLateJoinEntry, int? slots = null)
    {
        return selectable && (hasLateJoinEntry && (setup == null || slots != 0) ||
            setup is { Length: >= 2 } && (setup[0] != 0 || setup[1] != 0));
    }

    public static int? AvailableSlots(bool hasEntry, int? slots) => hasEntry ? slots : 0;

    public static int? ConfiguredSlots(int[]? setup, int index)
    {
        if (setup == null || index < 0 || index >= setup.Length)
            return 0;
        return setup[index] < 0 ? null : setup[index];
    }

    public static bool ValidSlots(int? slots) => slots == null || slots is >= 0 and <= MaxSlots;
}
