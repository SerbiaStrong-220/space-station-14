using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Ranged.Components;

/// <summary>
/// Allows battery weapons to fire different types of projectiles
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(BatteryWeaponFireModesSystem))]
[AutoGenerateComponentState]
public sealed partial class BatteryWeaponFireModesComponent : Component
{
    /// <summary>
    /// A list of the different firing modes the weapon can switch between
    /// </summary>
    [DataField(required: true)]
    [AutoNetworkedField]
    public List<BatteryWeaponFireMode> FireModes = new();

    /// <summary>
    /// The currently selected firing mode
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public int CurrentFireMode;
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class BatteryWeaponFireMode
{
    /// <summary>
    /// The projectile prototype associated with this firing mode
    /// </summary>
    [DataField("proto", required: true)]
    public string Prototype = default!; //SS220 Add Multifaze gun

    //SS220 Add Multifaze gun begin
    /// <summary>
    /// Name of the fire mode
    /// </summary>
    [DataField("name")]
    public string? FireModeName;

    /// <summary>
    /// Gun modifiers of this fire mode
    /// </summary>
    [DataField]
    public FireModeGunModifiers? GunModifiers;

    /// <summary>
    /// Sprite of the remaining charge that is used in the selected fire mode
    /// </summary>
    [DataField]
    public string? MagState;
    //SS220 Add Multifaze gun end

    /// <summary>
    /// The battery cost to fire the projectile associated with this firing mode
    /// </summary>
    [DataField]
    public float FireCost = 100;
}

[Serializable, NetSerializable]
public enum BatteryWeaponFireModeVisuals : byte
{
    State
}

//SS220 Add firemode modificators begin
/// <summary>
/// Gun modifiers that can be applied in each fire mode
/// </summary>
[DataDefinition, Serializable, NetSerializable]
public sealed partial class FireModeGunModifiers
{
    [DataField]
    public SoundSpecifier? SoundGunshot;

    [DataField]
    public float? CameraRecoilScala;

    [DataField]
    public Angle? AngleIncrease;

    [DataField]
    public Angle? AngleDecay;

    [DataField]
    public Angle? MaxAngle;

    [DataField]
    public Angle? MinAngle;

    [DataField]
    public int? ShotsPerBurst;

    [DataField]
    public float? FireRate;

    [DataField]
    public float? ProjectileSpeed;
}
//SS220 Add firemode modificators end
