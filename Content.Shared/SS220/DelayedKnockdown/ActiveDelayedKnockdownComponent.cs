using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.SS220.DelayedKnockdown;

[RegisterComponent]
public sealed partial class ActiveDelayedKnockdownComponent : Component
{
    /// <summary>
    /// Delay, after which knockdown is applied
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan Delay;

    /// <summary>
    /// Applied knockdown duration
    /// </summary>
    [DataField]
    public float KnockdownTime;

    /// <summary>
    /// Refresh current knockdown?
    /// </summary>
    [DataField]
    public bool Refresh = false;

    /// <summary>
    /// Stand automatically after knockdown?
    /// </summary>
    [DataField]
    public bool AutoStand = true;

    /// <summary>
    /// Drop items from hands?
    /// </summary>
    [DataField]
    public bool Drop = false;
}