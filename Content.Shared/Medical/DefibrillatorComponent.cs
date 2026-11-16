using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Item.ItemToggle.Components;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Medical;

/// <summary>
/// This is used for defibrillators; a machine that shocks a dead
/// person back into the world of the living.
/// Uses <see cref="ItemToggleComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DefibrillatorComponent : Component
{
    /// <summary>
    /// How much damage is healed from getting zapped.
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public DamageSpecifier ZapHeal = default!;

    /// <summary>
    /// The electrical damage from getting zapped.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int ZapDamage = 5;

    //SS220 LimitationRevive - start
    /// <summary>
    /// Multiplied damage for a failed defibrillator attempt.
    /// </summary>
    [DataField]
    public int ZapCoeffDamage = 8;
    //SS220 LimitationRevive - end

    /// <summary>
    /// How long the victim will be electrocuted after getting zapped.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan WritheDuration = TimeSpan.FromSeconds(3);

    /// <summary>
    /// ID of the cooldown use delay.
    /// </summary>
    [DataField]
    public string DelayId = "defib-delay";

    /// <summary>
    /// Cooldown after using the defibrillator.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan ZapDelay = TimeSpan.FromSeconds(5);

    /// <summary>
    /// How long the doafter for zapping someone takes.
    /// </summary>
    /// <remarks>
    /// This is synced with the audio; do not change one but not the other.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public TimeSpan DoAfterDuration = TimeSpan.FromSeconds(3);

    /// <summary>
    /// If false cancels the doafter when moving.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool AllowDoAfterMovement = true;

    /// <summary>
    /// Can the defibrilator be used on mobs in critical mobstate?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool CanDefibCrit = true;

    //SS220 LimitationRevive - start
    /// <summary>
    /// Chance of a successful shock with a defibrillator to revive a corpse.
    /// </summary>
    [DataField]
    public float ChanceWithoutMedSkill = 0.1f;
    //SS220 LimitationRevive - end

    /// <summary>
    /// The sound to play when someone is zapped.
    /// </summary>
    [DataField]
    public SoundSpecifier? ZapSound = new SoundPathSpecifier("/Audio/Items/Defib/defib_zap.ogg");

    /// <summary>
    /// The sound to play when starting the doafter.
    /// </summary>
    [DataField]
    public SoundSpecifier? ChargeSound = new SoundPathSpecifier("/Audio/Items/Defib/defib_charge.ogg");

    [DataField]
    public SoundSpecifier? FailureSound = new SoundPathSpecifier("/Audio/Items/Defib/defib_failed.ogg");

    [DataField]
    public SoundSpecifier? SuccessSound = new SoundPathSpecifier("/Audio/Items/Defib/defib_success.ogg");

    [DataField]
    public SoundSpecifier? ReadySound = new SoundPathSpecifier("/Audio/Items/Defib/defib_ready.ogg");
}

/// <summary>
/// DoAfterEvent for defibrilator use windup.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class DefibrillatorZapDoAfterEvent : SimpleDoAfterEvent;
