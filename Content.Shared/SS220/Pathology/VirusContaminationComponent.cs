// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Pathology;

[RegisterComponent]
public sealed partial class VirusContaminationComponent : Component
{
    /// <summary>Strains carried on this.</summary>
    [ViewVariables]
    public List<VirusInstance> Viruses = new();

    /// <summary>How long virus survives outside host/mutagen.</summary>
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(60);

    /// <summary>When the contamination clears.</summary>
    [ViewVariables]
    public TimeSpan ExpiresAt;
}
