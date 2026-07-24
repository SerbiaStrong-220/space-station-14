// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Virology;

[RegisterComponent]
public sealed partial class VirusProtectionComponent : Component
{
    /// <summary>Vectors this gear protects against.</summary>
    [DataField]
    public VirusTransmissionVector Vectors;

    /// <summary>Chance (0..1) to block a vector spread. 1 = always.</summary>
    [DataField]
    public float BlockChance = 1f;
}
