using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.NightVision;

[RegisterComponent]
[NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class NightVisionComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public bool Enabled;

    /// <summary>
    /// Min brightness level in complete darkness.
    /// Controls how much user can see without any light.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public float MinLight = 0.0f;

    /// <summary>
    /// Brightness threshold after which light sources start to become overexposed.
    /// Everything below this value is normal light.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public float BrightThreshold = 0.0f;

    /// <summary>
    /// Intensity multiplier for very bright areas.
    /// Controls how strongly light sources "blind" the night vision.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public float BrightBoost = 60f;

    /// <summary>
    /// Gamma correction applied to the final image.
    /// Lower values = brighter mid-tones, higher values = darker mid-tones.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public float Gamma = 1.2f;

    /// <summary>
    /// Amount of visual noise (grain) applied over the image.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public float NoiseAmount = 0.01f;

    /// <summary>
    /// Minimum light intensity applied after the light render target.
    /// Prevents the scene from becoming completely black when night vision is enabled.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public float MinLightAfterTargetOverlay = 0.013f;

    /// <summary>
    /// Final tint color of the night vision image.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public Color VisionColor = Color.FromHex("#26FF26");

    [DataField]
    [AutoNetworkedField]
    public EntProtoId Action = "ActionToggleNightVision";

    [AutoNetworkedField]
    public EntityUid? ActionEntity;
}
