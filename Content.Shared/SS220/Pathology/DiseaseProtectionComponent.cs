// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Pathology;

/// <summary>
/// Worn clothing that blocks virus transmission attempts relayed to it (e.g. a sterile mask).
/// </summary>
[RegisterComponent]
public sealed partial class DiseaseProtectionComponent : Component
{
    [DataField]
    public VirusTransmissionVector Vectors = VirusTransmissionVector.None;

    /// <summary>Chance (0..1) to block a matching infection attempt, 1 always blocks.</summary>
    [DataField]
    public float BlockChance = 1f;
}
