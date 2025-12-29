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
    /// UseDelay for an attack to differentiate it from turning on/off UseDelay
    /// </summary>
    [DataField]
    public string UseDelay = "default";

    /// <summary>
    /// Can it be applied for heavy attack (RMB)
    /// </summary>
    [DataField]
    public bool OnHeavyAttack = true;

    /// <summary>
    /// Dictionary mapping stamina resistance thresholds to (delay bonus, knockdown penalty) pairs.
    /// Resistance thresholds (0.0–1.0) - stamina resistance in %
    /// DelayBonus - seconds, added to a DelayedKnockdown
    /// KnockdownPenalty - seconds, subtracted from knockdownDuration
    /// Should be sorted in descending order of keys for correct application logic.
    /// </summary>
    [DataField]
    public Dictionary<float, (TimeSpan DelayBonus, TimeSpan KnockdownPenalty)> ResistanceModifiers = new()
    {
        { 0.3f, (TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(1.5)) }, //70% res
        { 0.5f, (TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(1.0)) }, //50% res
        { 0.8f, (TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(0.5)) }, //20% res
    };

}
