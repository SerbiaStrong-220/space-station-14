namespace Content.Shared.SS220.Deafenation;

[RegisterComponent]
public sealed partial class NoiseSuppressionComponent : Component
{
    /// <summary>
    /// Any deafening noise in a tile radius greater than this, it will be suppressed (ignored)
    /// </summary>
    [DataField]
    public float SuppressionRange = 2f;
}
