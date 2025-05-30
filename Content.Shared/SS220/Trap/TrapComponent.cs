// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
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

    [ViewVariables(VVAccess.ReadWrite), DataField]
    public DamageSpecifier? DamageOnTrapped;

    /// <summary>
    /// Reagent to inject into the tripper.
    /// </summary>
    [DataField]
    public ProtoId<ReagentPrototype>? Reagent;

    /// <summary>
    /// How much of the reagent to inject.
    /// </summary>
    [DataField]
    public FixedPoint2 Quantity;

    /// <summary>
    /// Delay time when interacting with a trap, be it set or defuse
    /// </summary>
    [DataField]
    public TimeSpan InteractionDelay = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Is trap ready?
    /// </summary>
    [AutoNetworkedField, ViewVariables]
    public bool IsArmed = false;

    [DataField]
    public SoundSpecifier SetTrapSound = new SoundPathSpecifier("/Audio/SS220/Items/Trap/sound_trap_set.ogg");

    [DataField]
    public SoundSpecifier DefuseTrapSound = new SoundPathSpecifier("/Audio/SS220/Items/Trap/sound_trap_defuse.ogg");

    [DataField]
    public SoundSpecifier HitTrapSound = new SoundPathSpecifier("/Audio/SS220/Items/Trap/sound_trap_hit.ogg");
}

[Serializable, NetSerializable]
public sealed partial class InteractionTrapDoAfterEvent : SimpleDoAfterEvent
{

}
public sealed class TrapChangedArmedEvent : EntityEventArgs
{
    public readonly bool NewIsArmed;

    public TrapChangedArmedEvent(bool newIsArmed)
    {
        NewIsArmed = newIsArmed;
    }
}
