// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Virology.Behaviors;

[RegisterComponent]
public sealed partial class VirusGlowComponent : Component
{
    [DataField]
    public Color LightColor = Color.White;

    [DataField]
    public float LightRadius = 1.5f;

    [DataField]
    public float LightEnergy = 1f;

    /// <summary>We added host's point light, so restore removes only ours.</summary>
    [ViewVariables]
    public bool Added;

    [ViewVariables]
    public Color SavedColor;

    [ViewVariables]
    public float SavedRadius;

    [ViewVariables]
    public float SavedEnergy;

    [ViewVariables]
    public bool SavedEnabled;
}
