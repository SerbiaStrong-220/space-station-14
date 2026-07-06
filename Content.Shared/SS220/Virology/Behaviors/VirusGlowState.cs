// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Virology.Behaviors;

public struct VirusGlowState
{
    /// <summary>We added host's point light? so remove only ours.</summary>
    public bool Added;

    public Color SavedColor;
    public float SavedRadius;
    public float SavedEnergy;
    public bool SavedEnabled;
}
