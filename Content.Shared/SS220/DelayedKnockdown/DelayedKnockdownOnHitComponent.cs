using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.SS220.DelayedKnockdown;

[RegisterComponent]
public sealed partial class DelayedKnockdownOnHitComponent : Component
{
    /// <summary>
    /// Delay before applying knockdown
    /// </summary>
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Knockdown duration
    /// </summary>
    [DataField]
    public TimeSpan Duration = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Refresh current knockdown?
    /// </summary>
    [DataField]
    public bool Refresh = true;

    /// <summary>
    /// Stand automatically after knockdown?
    /// </summary>
    [DataField]
    public bool AutoStand = true;

    /// <summary>
    /// Drop items from hands?
    /// </summary>
    [DataField]
    public bool Drop = true;

    /// <summary>
    /// Is UseDelay respected?
    /// </summary>
    [DataField]
    public bool CheckUseDelay = true;
}
