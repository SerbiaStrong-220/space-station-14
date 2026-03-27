// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Explosion;
using Content.Shared.Explosion.EntitySystems;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.DnaLock.Components;

/// <summary>
/// Clothing component with a DNA lock: when an unauthorized user equips the item,
/// it starts a warning timer with beeps and popups. When the timer expires, the item detonates and is deleted.
/// Requires a DnaLockableComponent on the same entity.
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
    /// Popup shown to everyone in PVS.
    /// </summary>
    [DataField]
    public string WarningPopupOthers = "dna-lock-clothing-unauthorized-warning";

    /// <summary>
    /// Popup shown to the unauthorized wearer.
    /// </summary>
    [DataField]
    public string WarningPopupWearer = "dna-lock-clothing-unauthorized-warning-self";

    /// <summary>
    /// Countdown popup shown to nearby players.
    /// </summary>
    [DataField]
    public string TimerPopupOthers = "dna-lock-clothing-timer-warning";

    /// <summary>
    /// Countdown popup shown to the unauthorized wearer.
    /// </summary>
    [DataField]
    public string TimerPopupWearer = "dna-lock-clothing-timer-warning-self";

    [DataField]
    public ProtoId<ExplosionPrototype> ExplosionType = SharedExplosionSystem.DefaultExplosionPrototypeId;

    /// <summary>
    /// Explosion intensity. Intended to put the wearer into crit, not kill outright.
    /// </summary>
    [DataField]
    public float ExplosionTotalIntensity = 10f;

    /// <summary>
    /// Explosion falloff slope.
    /// </summary>
    [DataField]
    public float ExplosionSlope = 100f;

    [DataField]
    public float ExplosionMaxTileIntensity = 10f;
}
