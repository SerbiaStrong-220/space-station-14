using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.NightVision;

/// <summary>
/// Night vision appearance. Float settings are clamped to [0, 1]; light levels are linear.
/// </summary>
[Prototype]
public sealed partial class NightVisionProfilePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Monochrome tint, e.g. "#26FF26". White gives grayscale; darker colors dim the image. Alpha is ignored.
    /// </summary>
    [DataField]
    public Color Tint = Color.FromHex("#26FF26");

    /// <summary>
    /// Minimum illumination, 0..1. 0 leaves darkness unchanged; 1 fully lights the visible area.
    /// Higher values flatten more shadows. Does not affect overexposure thresholds.
    /// </summary>
    [DataField]
    public float DarknessBrightness = 0.6f;

    /// <summary>
    /// Enables light-induced washout. Does not affect tint, darkness brightness or noise.
    /// </summary>
    [DataField]
    public bool Overexposure = true;

    /// <summary>
    /// Original light level at which washout starts, 0..1. Lower values trigger it in dimmer light.
    /// Uses the lightmap's brightest RGB channel before night vision is applied.
    /// </summary>
    [DataField]
    public float OverexposureStart = 0.25f;

    /// <summary>
    /// Light level at which washout reaches full strength, 0..1. A wider start/end gap gives a smoother transition.
    /// Values at or below OverexposureStart produce a hard cutoff at the start threshold.
    /// </summary>
    [DataField]
    public float OverexposureEnd = 0.8f;

    /// <summary>
    /// Maximum blend toward Tint, 0..1. 0 disables washout; 1 completely hides details at full exposure.
    /// </summary>
    [DataField]
    public float OverexposureStrength = 0.9f;

    /// <summary>
    /// Animated grain strength, 0..1. 0 disables it; 0.01–0.03 gives subtle grain; 1 gives heavy noise.
    /// Independent of overexposure.
    /// </summary>
    [DataField]
    public float Noise;
}
