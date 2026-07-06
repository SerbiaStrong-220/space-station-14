// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Virology;

[RegisterComponent]
public sealed partial class VirusContaminantComponent : Component
{
    /// <summary>Strains carried on this item.</summary>
    [ViewVariables]
    public List<VirusDescriptor> Viruses = [];

    /// <summary>How long a strain survives on the item.</summary>
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(60);

    /// <summary>When the contamination clears.</summary>
    [ViewVariables]
    public TimeSpan ExpiresAt;
}
