// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.DoAfter;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Trap;

/// <summary>
/// The logic of traps witch look like bears. Automatically “binds to leg” when activated.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TrapComponent : Component
{
    /// <summary>
    /// If 0, there will be no stun
    /// </summary>
    [DataField]
    public TimeSpan DurationStun = TimeSpan.Zero;

    [DataField]
    public EntityWhitelist Blacklist = new();

    /// <summary>
    /// Delay time for setting trap
    /// </summary>
    [DataField]
    public TimeSpan SetTrapDelay = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Delay time for defuse trap
    /// </summary>
    [DataField]
    public TimeSpan DefuseTrapDelay = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Is trap ready?
    /// </summary>
    [AutoNetworkedField, ViewVariables]
    public TrapArmedState State = TrapArmedState.Unarmed;

    [DataField]
    public SoundSpecifier SetTrapSound = new SoundPathSpecifier("/Audio/SS220/Items/Trap/sound_trap_set.ogg");

    [DataField]
    public SoundSpecifier DefuseTrapSound = new SoundPathSpecifier("/Audio/SS220/Items/Trap/sound_trap_defuse.ogg");

    [DataField]
    public SoundSpecifier HitTrapSound = new SoundPathSpecifier("/Audio/SS220/Items/Trap/sound_trap_hit.ogg");
}

[Serializable, NetSerializable]
public enum TrapArmedState : byte
{
    Unarmed,
    Armed
}

/// <summary>
/// Event raised when a trap is successfully armed.
/// </summary>
[Serializable, NetSerializable]
public sealed class TrapArmedEvent : EntityEventArgs
{
}

/// <summary>
/// Event raised when a trap is successfully defused.
/// </summary>
[Serializable, NetSerializable]
public sealed class TrapDefusedEvent : EntityEventArgs
{
}
/// <summary>
/// Event raised when attempting to defuse a trap to check if it can be defused.
/// </summary>
public sealed partial class TrapDefuseAttemptEvent : CancellableEntityEventArgs
{
    public EntityUid? User;
    public TrapDefuseAttemptEvent(EntityUid? user)
    {
        User = user;
    }
}
/// <summary>
/// Event raised when attempting to arm a trap to check if it can be armed.
/// </summary>
public sealed partial class TrapArmAttemptEvent : CancellableEntityEventArgs
{
    public EntityUid? User;
    public TrapArmAttemptEvent(EntityUid? user)
    {
        User = user;
    }
}
/// <summary>
/// Event DoAfter when interacting with traps.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class TrapInteractionDoAfterEvent : SimpleDoAfterEvent
{
    public bool ArmAction { get; set; }
}
public sealed class TrapToggledEvent : EntityEventArgs
{
    public readonly bool IsArmed;

    public TrapToggledEvent(bool isArmed)
    {
        IsArmed = isArmed;
    }
}
