using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.QuadHearing;

[Prototype]
public sealed partial class QuadHearingTargetTypePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public TimeSpan LifeTime = TimeSpan.FromSeconds(7f);

    [DataField]
    public TimeSpan FadeDelay = TimeSpan.FromSeconds(4f);

    [DataField]
    public float RandomOffsetCoefficient = 0.1f;

    #region Shader params
    [DataField]
    public Color Color = Color.LightBlue.WithAlpha(0.3f);

    [DataField]
    public float WaveThikness = 0.7f;

    [DataField]
    public float WaveInterval = 0.2f;

    [DataField]
    public float WaveSpeed = 1.3f;

    [DataField]
    public float CircleWaveRadius = 2.2f;

    [DataField]
    public float CircleWaveDecreaseStart = 1.2f;

    [DataField]
    public float SectorWaveMinDistance = 5f;

    [DataField]
    public Angle DirectedWaveAngle = Angle.FromDegrees(15);

    [DataField]
    public float NoiseAmplitude = 10f;
    #endregion
}
