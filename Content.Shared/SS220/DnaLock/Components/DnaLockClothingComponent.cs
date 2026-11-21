// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.DnaLock.Components;

/// <summary>
/// Clothing component with a DNA lock: when the item is worn by someone without access (it was put on, or the lock was turned on),
/// it counts down aloud. The countdown stops if the item is taken off or the wearer gains access.
/// When the timer expires, the item detonates and is deleted.
/// Requires a DnaLockableComponent and an ExplosiveComponent on the same entity.
/// </summary>
[RegisterComponent]
public sealed partial class DnaLockClothingComponent : Component
{
    /// <summary>
    /// Time until detonation after an unauthorized user equips the item.
    /// </summary>
    [DataField]
    public TimeSpan TimeToExplode = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Interval between countdown lines.
    /// </summary>
    [DataField]
    public TimeSpan BeepInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Said by the item when the countdown starts.
    /// </summary>
    [DataField]
    public LocId WarningMessage = "dna-lock-clothing-unauthorized-warning";

    /// <summary>
    /// Said by the item on every countdown line except the last.
    /// </summary>
    [DataField]
    public LocId TimerMessage = "dna-lock-clothing-timer-warning";

    /// <summary>
    /// Said by the item on the last countdown line, right before detonation.
    /// </summary>
    [DataField]
    public LocId DetonationMessage = "dna-lock-clothing-detonating";
}
