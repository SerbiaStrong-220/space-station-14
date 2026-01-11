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
    Armed,
}

/// <summary>
/// Event raised when a trap is successfully armed.
/// </summary>
[ByRefEvent]
public record struct TrapArmedEvent;

/// <summary>
/// Event raised when a trap is successfully defused.
/// </summary>
[ByRefEvent]
public record struct TrapDefusedEvent;

/// <summary>
/// Event raised when attempting to defuse a trap to check if it can be defused.
/// </summary>
[ByRefEvent]
public record struct TrapDefuseAttemptEvent(EntityUid? User, bool Cancelled = false);

/// <summary>
/// Event raised when attempting to arm a trap to check if it can be armed.
/// </summary>
[ByRefEvent]
public record struct TrapArmAttemptEvent(EntityUid? User, bool Cancelled = false);

/// <summary>
/// Event DoAfter when interacting with traps.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class TrapInteractionDoAfterEvent : SimpleDoAfterEvent
{
    public bool ArmAction { get; set; }
}
