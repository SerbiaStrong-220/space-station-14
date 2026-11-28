// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Pathology;
using Content.Shared.SS220.Surgery;
using Content.Shared.SS220.Surgery.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.Surgery.SurgeryStartUi;

[Flags]
public enum PuppetZoneFlags : byte
{
    None = 0,
    ModerateTrauma = 1 << 0,
    SevereTrauma = 1 << 1,
    Missing = 1 << 2,
    OngoingSurgery = 1 << 3,
}

/// <summary>
/// Zone states and their tints, shared by <see cref="SurgeryPuppetBox"/> and the pathology doll alert.
/// The flags of a zone are indexed by its <see cref="SurgeryPuppetPart"/>.
/// </summary>
public static class PuppetZones
{
    public static readonly Color HealthyTint = Color.FromHex("#4AC4FF");
    private static readonly Color OngoingSurgeryTint = Color.FromHex("#4CD964");
    private static readonly Color ModerateTraumaTint = Color.FromHex("#FFCC44");
    private static readonly Color SevereTraumaTint = Color.FromHex("#FF5555");
    // multiplied with the grey part texture: dark grey inside, darker outline
    private static readonly Color MissingPartTint = Color.FromHex("#707070");

    /// <summary>
    /// All the zones, the flags are indexed by their values. A span would be safer, but the sandbox forbids returning it.
    /// </summary>
    public static readonly SurgeryPuppetPart[] Parts = Enum.GetValues<SurgeryPuppetPart>();

    public static void FillFlags(
        Span<PuppetZoneFlags> flags,
        PathologyHolderComponent? holder,
        SurgeryPatientComponent? patient,
        IPrototypeManager prototype)
    {
        flags.Clear();

        if (holder != null)
        {
            foreach (var (pathologyId, data) in holder.ActivePathologies)
            {
                if (!prototype.Resolve(pathologyId, out var pathology) || pathology.TargetZone is not { } zone)
                    continue;

                if (pathology.MissingPart)
                    flags[(int) zone] |= PuppetZoneFlags.Missing;
                else if (SharedPathologySystem.IsSevere(pathology, data))
                    flags[(int) zone] |= PuppetZoneFlags.SevereTrauma;
                else
                    flags[(int) zone] |= PuppetZoneFlags.ModerateTrauma;
            }
        }

        if (patient != null)
        {
            foreach (var (graphId, node) in patient.OngoingSurgeries)
            {
                if (prototype.Resolve(graphId, out var graph) && node != graph.End)
                    flags[(int) graph.TargetPart] |= PuppetZoneFlags.OngoingSurgery;
            }
        }
    }

    /// <param name="includeHeadSubZones">The head also shows its eyes and mouth, when they have no own overlays.</param>
    /// <param name="healthyTint">Tint of a zone without any state, null - no tint.</param>
    public static Color? ResolveTint(
        SurgeryPuppetPart part,
        ReadOnlySpan<PuppetZoneFlags> flags,
        bool includeHeadSubZones,
        Color? healthyTint)
    {
        var own = flags[(int) part];
        var shown = own;
        if (includeHeadSubZones && part == SurgeryPuppetPart.Head)
            shown |= flags[(int) SurgeryPuppetPart.Eyes] | flags[(int) SurgeryPuppetPart.Mouth];

        if ((shown & PuppetZoneFlags.OngoingSurgery) != 0)
            return OngoingSurgeryTint;

        // a severed head takes its eyes and mouth along, a missing eye doesn't make the head missing
        if ((own & PuppetZoneFlags.Missing) != 0
            || part is SurgeryPuppetPart.Eyes or SurgeryPuppetPart.Mouth && (flags[(int) SurgeryPuppetPart.Head] & PuppetZoneFlags.Missing) != 0)
            return MissingPartTint;

        if ((shown & PuppetZoneFlags.SevereTrauma) != 0)
            return SevereTraumaTint;

        if ((shown & PuppetZoneFlags.ModerateTrauma) != 0)
            return ModerateTraumaTint;

        return healthyTint;
    }
}
