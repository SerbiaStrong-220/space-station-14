// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Virology;

[RegisterComponent]
public sealed partial class VirusContaminantComponent : Component
{
    /// <summary>Strains carried on this item.</summary>
    [ViewVariables]
    public List<VirusContaminant> Viruses = [];

    /// <summary>How long a strain survives on the item after it lands or refreshed.</summary>
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(60);
}

/// <summary>One strain sitting on a contaminated item, with its own clear-out deadline.</summary>
public sealed class VirusContaminant
{
    public VirusDescriptor Descriptor = default!;
    public TimeSpan ExpiresAt;
}
