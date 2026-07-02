// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Pathology;

/// <summary>
/// Recoil-weakness symptom: firing a wielded two-handed weapon knocks the host off its feet.
/// </summary>
[RegisterComponent]
public sealed partial class RecoilWeaknessComponent : Component
{
    /// <summary>How long the host stays knocked down after firing a two-handed weapon.</summary>
    [DataField]
    public TimeSpan KnockdownTime = TimeSpan.FromSeconds(2);
}
