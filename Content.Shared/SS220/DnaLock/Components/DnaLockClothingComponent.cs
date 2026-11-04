// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Audio;

namespace Content.Shared.SS220.DnaLock.Components;

/// <summary>
/// Clothing component with a DNA lock: when an unauthorized user equips the item,
/// it starts a warning timer with beeps and popups. When the timer expires, the item detonates and is deleted.
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
    /// Interval between warning beeps.
    /// </summary>
    [DataField]
    public TimeSpan BeepInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Warning beep sound.
    /// </summary>
    [DataField]
    public SoundSpecifier WarningSound = new SoundPathSpecifier("/Audio/Items/beep.ogg");

    /// <summary>
    /// Announced by the item to everyone nearby when an unauthorized user equips it.
    /// </summary>
    [DataField]
    public LocId WarningPopup = "dna-lock-clothing-unauthorized-warning";

    /// <summary>
    /// Countdown announced by the item to everyone nearby.
    /// </summary>
    [DataField]
    public LocId TimerPopup = "dna-lock-clothing-timer-warning";

    /// <summary>
    /// Announced by the item to everyone nearby at the moment of detonation.
    /// </summary>
    [DataField]
    public LocId DetonationPopup = "dna-lock-clothing-detonating";
}
