using Content.Shared.Temperature.Systems;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Temperature.Components;

/// <summary>
/// Adds thermal energy to entities with <see cref="TemperatureComponent"/> placed on it.
/// </summary>
[RegisterComponent, Access(typeof(SharedEntityHeaterSystem))]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EntityHeaterComponent : Component
{
    /// <summary>
    /// Power used when heating at the high setting.
    /// Low and medium are 33% and 66% respectively.
    /// </summary>
    [DataField]
    public float Power = 2400f;

    /// <summary>
    /// Current setting of the heater. If it is off or unpowered it won't heat anything.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityHeaterSetting Setting = EntityHeaterSetting.Off;

    /// <summary>
    /// An optional sound that plays when the setting is changed.
    /// </summary>
    [DataField]
    public SoundPathSpecifier? SettingSound;

    //SS220-grill-update begin
    /// <summary>
    /// Sound that plays, when food is on the grill
    /// </summary>
    [DataField("grillSound")]
    public SoundSpecifier GrillSound = new SoundPathSpecifier("/Audio/SS220/Effects/grilling.ogg");

    // Grill smoke sprite
    [DataField, AutoNetworkedField]
    public SpriteSpecifier.Rsi GrillingSprite;

    /// <summary>
    /// Whitelist for entities that can have grilling visuals.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public EntityWhitelist? HeatingAnimationWhitelist;

    // To keep track of the grilling sound
    public EntityUid? GrillingAudioStream;
    //SS220-grill-update end
}
