using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.QuadHearing;

[Prototype]
public sealed partial class QuadHearingTargetPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Total lifetime of the target after registration.
    /// </summary>
    [DataField]
    public TimeSpan LifeTime = TimeSpan.FromSeconds(7f);

    /// <summary>
    /// Time after which the visual effect starts to fade out (alpha reduction).
    /// </summary>
    [DataField]
    public TimeSpan FadeDelay = TimeSpan.FromSeconds(4f);

    /// <summary>
    /// Maximum coords offset = distance to target * this coefficient.
    /// </summary>
    [DataField]
    public float RandomOffsetCoefficient = 0.1f;

    /// <summary>
    /// Range at which this target can be heard
    /// </summary>
    [DataField]
    public float HearingRange = 15f;

    #region Shader params
    /// <summary>
    /// Color of the waves.
    /// </summary>
    [DataField]
    public Color Color = Color.LightBlue.WithAlpha(0.3f);

    /// <summary>
    /// Thickness of wave rings.
    /// </summary>
    [DataField]
    public float WaveThickness = 0.7f;

    /// <summary>
    /// Empty space (gap) between wave rings.
    /// </summary>
    [DataField]
    public float WaveInterval = 0.2f;

    [DataField]
    public float WaveSpeed = 1.3f;

    /// <summary>
    /// Maximum radius of circular waves.
    /// </summary>
    [DataField]
    public float CircleWaveRadius = 2.2f;

    /// <summary>
    /// Radius at which circular waves begin to fade (alpha reduction).
    /// </summary>
    [DataField]
    public float CircleWaveFadeRadius = 1.2f;

    /// <summary>
    /// Minimum distance from the player at which sector waves are drawn.
    /// </summary>
    [DataField]
    public float SectorWaveMinDistance = 5f;

    /// <summary>
    /// Amplitude of the noise effect applied to the waves.  
    /// </summary>
    [DataField]
    public float NoiseAmplitude = 10f;
    #endregion
}
