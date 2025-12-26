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
    /// Minimal StaminaResistance value, when delay will be altered
    /// Value 0.5 means 50% StamRes
    /// </summary>
    [DataField]
    public float ResistanceThreshold = 0.5f;
    
    /// <summary>
    /// Added seconds to knockdown delay
    /// </summary>
    [DataField]
    public TimeSpan ResistanceDelayBonus = TimeSpan.FromSeconds(2);
    
    /// <summary>
    /// Knockdown duration penalty (seconds) when min resistance is reached 
    /// </summary>
    [DataField]
    public TimeSpan ResistanceKnockdownPenalty = TimeSpan.FromSeconds(0.5);
 
}
