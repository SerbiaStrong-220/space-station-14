using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Body;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FlashSensitiveComponent : Component
{
    /// <summary>
    /// Multiplier applied to the flash duration when this entity is flashed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float FlashDurationMultiplier = 1f;

    /// <summary>
    /// Multiplier applied to the stun duration when this entity is flashed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float StunDurationMultiplier = 1f;

    /// <summary>
    /// Amount of eye damage inflicted per flash.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int? FlashEyeDamage;

    /// <summary>
    /// How long the entity remains temporarily blinded after being flashed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan? TemporaryBlindnessDuration;
}
