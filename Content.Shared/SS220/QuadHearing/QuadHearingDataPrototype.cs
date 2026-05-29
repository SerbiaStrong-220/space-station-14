using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.QuadHearing;

[Prototype]
public sealed partial class QuadHearingTargetTypePrototype : IPrototype
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

    #region Shader params
    /// <summary>
    /// Color of the wave shader.
    /// </summary>
    [DataField]
    public Color Color = Color.LightBlue.WithAlpha(0.3f);

    /// <summary>
    /// Thickness of each wave ring.
    /// </summary>
    [DataField]
    public float WaveThikness = 0.7f;

    /// <summary>
    /// Empty space (gap) between wave rings.
    /// </summary>
    [DataField]
    public float WaveInterval = 0.2f;

    /// <summary>
    /// Wave propagation speed.
    /// </summary>
    [DataField]
    public float WaveSpeed = 1.3f;

    /// <summary>
    /// Radius of the circular wave.
    /// </summary>
    [DataField]
    public float CircleWaveRadius = 2.2f;

    /// <summary>
    /// Radius at which the circular wave begins to fade (alpha reduction).
    /// </summary>
    [DataField]
    public float CircleWaveFadeRadius = 1.2f;

    /// <summary>
    /// Minimum distance from the player at which the sector wave is drawn. 
    /// </summary>
    [DataField]
    public float SectorWaveMinDistance = 5f;

    /// <summary>
    /// Amplitude of the noise effect applied to the wave.  
    /// </summary>
    [DataField]
    public float NoiseAmplitude = 10f;
    #endregion
}
