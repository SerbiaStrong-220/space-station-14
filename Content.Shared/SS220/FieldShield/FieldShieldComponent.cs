// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.SS220.FieldShield;

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState(true, true)]
public sealed partial class FieldShieldComponent : Component
{
    [ViewVariables]
    [AutoNetworkedField]
    public TimeSpan RechargeEndTime;

    [ViewVariables]
    [AutoNetworkedField]
    public int ShieldCharge = 0;

    [DataField(required: true)]
    [AutoNetworkedField]
    public FieldShieldRechargeData RechargeShieldData;

    [DataField(required: true)]
    [AutoNetworkedField]
    public FieldShieldData ShieldData;

    [DataField(required: true)]
    [AutoNetworkedField]
    public FieldShieldLightData LightData;

    /// <summary>
    ///     Client side point-light entity.
    /// </summary>
    [ViewVariables]
    public EntityUid? LightEntity;
}

[DataDefinition]
[Serializable, NetSerializable]
public partial struct FieldShieldRechargeData
{
    [DataField]
    [AutoNetworkedField]
    public TimeSpan RechargeTime = TimeSpan.FromSeconds(15f);

    [DataField]
    [AutoNetworkedField]
    public float EmpRechargeMultiplier = 4f;

    /// <summary>
    /// Lower than this damage won't start recharge time again
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public FixedPoint4 DamageThreshold = 1f;
}

[DataDefinition]
[Serializable, NetSerializable]
public partial struct FieldShieldData
{
    [DataField]
    [AutoNetworkedField]
    public int ShieldMaxCharge;

    /// <summary>
    /// Lower than this damage won't be blocked by shield
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public FixedPoint4 DamageThreshold = 1f;

    /// <summary>
    /// If this damage tries to apply to entity shield will just consume part of damage
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public FixedPoint4 MaxDamageConsumable = 70f;

    [DataField]
    [AutoNetworkedField]
    public DamageModifierSet Modifiers = new()
    {
        Coefficients = new()
        {
            {"Blunt", 0.5f},
            {"Piercing", 0.5f},
            {"Slash", 0.5f},
            {"Shock", 0.5f},
            {"Heat", 0.5f},
            {"Cold", 0.5f},
            {"Stamina", 0.2f}
        }
    };

    [DataField]
    [AutoNetworkedField]
    public SpriteSpecifier? ShieldSprite;

    [DataField]
    [AutoNetworkedField]
    public SoundSpecifier? ShieldBlockSound = new SoundPathSpecifier("/Audio/SS220/Effects/FieldShield/basscannon.ogg")
    {
        Params = AudioParams.Default.WithPitchScale(1.1f).WithVariation(0.15f).WithVolume(-7f)
    };
}

[DataDefinition]
[Serializable, NetSerializable]
public partial struct FieldShieldLightData
{
    [DataField]
    [AutoNetworkedField]
    public Color Color = Color.White;

    [DataField]
    [AutoNetworkedField]
    public float Radius = 1f;

    [DataField]
    [AutoNetworkedField]
    public float Energy = 1f;
}
