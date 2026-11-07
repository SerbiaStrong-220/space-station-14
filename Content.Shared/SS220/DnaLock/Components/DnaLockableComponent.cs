// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.EntityEffects;
using Content.Shared.Roles;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.DnaLock.Components;

/// <summary>
/// Lets only players with allowed DNA use the item.
/// Without <see cref="Job"/> it is a personal lock: anyone can bind the item to their DNA through a verb, and the owner can turn the lock on and off.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DnaLockableComponent : Component
{
    /// <summary>
    /// The DNA of everyone who holds this job is added to the lock automatically. Such a lock can't be toggled or emagged.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<JobPrototype>? Job;

    /// <summary>
    /// DNA allowed to use the item: the owner's for a personal lock, every holder of the job for a job lock.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<string> AllowedDna = new();

    /// <summary>
    /// Whether a personal lock is on. While it is off, anyone can use the item or bind it to themselves. Job locks ignore it.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool LockActive;

    /// <summary>
    /// On the first insertion into a container, binds the item to the nearest entity with DNA up the container chain,
    /// such as the wearer of the backpack it was put in. Works only once, even if no such entity was found.
    /// </summary>
    [DataField]
    public bool BindToFirstHolder;

    /// <summary>
    /// Hides the lock's examine text from everyone except the owner, so others can't tell the item is locked.
    /// </summary>
    [DataField]
    public bool ExamineOwnerOnly;

    /// <summary>
    /// Effects applied to a user who is denied, such as an electric shock.
    /// </summary>
    [DataField]
    public EntityEffect[] UnauthorizedUseEffects = Array.Empty<EntityEffect>();

    /// <summary>
    /// Popup shown to a denied user. Null disables it.
    /// </summary>
    [DataField]
    public LocId? UnauthorizedUsePopup = "dna-lock-unauthorized-popup";

    /// <summary>
    /// Sound played when use is denied. Null disables it.
    /// </summary>
    [DataField]
    public SoundSpecifier? UnauthorizedUseSound = new SoundPathSpecifier("/Audio/Machines/custom_deny.ogg");

    /// <summary>
    /// Interactions the lock blocks on top of shooting and item toggling, which are always checked.
    /// </summary>
    [DataField, AutoNetworkedField]
    public DnaLockBlockedActions BlockedActions = DnaLockBlockedActions.All;
}

/// <summary>
/// Interactions a DNA lock can block on top of shooting and item toggling.
/// </summary>
[Flags]
public enum DnaLockBlockedActions : byte
{
    None = 0,

    /// <summary>
    /// Opening the item's storage.
    /// </summary>
    StorageOpen = 1 << 0,

    /// <summary>
    /// Deploying clothing attached to the item, such as a hardsuit from a backpack.
    /// </summary>
    ToggleableClothing = 1 << 1,

    /// <summary>
    /// Ejecting items from the item's slots, such as a power cell.
    /// </summary>
    BatteryEject = 1 << 2,

    /// <summary>
    /// Opening and using the item's interface.
    /// </summary>
    UserInterface = 1 << 3,

    All = StorageOpen | ToggleableClothing | BatteryEject | UserInterface
}
