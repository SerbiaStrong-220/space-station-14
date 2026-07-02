// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Pathology;

/// <summary>
/// Bulimia symptom: eating food makes the host vomit it straight back up.
/// </summary>
[RegisterComponent]
public sealed partial class BulimiaComponent : Component
{
    /// <summary>Delay between eating and throwing up.</summary>
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(1);

    /// <summary>When the pending vomit goes.</summary>
    [ViewVariables]
    public TimeSpan? VomitAt;
}
