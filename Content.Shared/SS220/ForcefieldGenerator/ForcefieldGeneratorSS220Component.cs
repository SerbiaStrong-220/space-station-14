// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.DeviceLinking;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.ForcefieldGenerator;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ForcefieldGeneratorSS220Component : Component
{
    public const string FIELD_FIXTURE_NAME = "fix1";

    /// <summary>
    /// How long the forcefield is
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public float FieldLength = 3;

    /// <summary>
    /// How long the forcefield is
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField, AutoNetworkedField]
    public float FieldThickness = 0.5f;

    /// <summary>
    /// Forcefield angle
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public Angle Angle = 0;

    /// <summary>
    /// Offset from the center of the generator, direction of the offset matches the angle of field
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public float Radius = 3;

    /// <summary>
    /// How much energy it costs per seond to keep the field up
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public float EnergyUpkeep = 183;

    /// <summary>
    /// How much energy it consumes per 1 unit of damage
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField]
    public float DamageToEnergyCoefficient = 15;

    /// <summary>
    /// Whether the GENERATOR is active or not
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public bool Active = false;

    /// <summary>
    /// Whether the FIELD is active or not
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public bool FieldEnabled = false;

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public Color FieldColor = Color.LightBlue;

    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float FieldVisibility = 0.1f;

    [ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier GeneratorIdleSound = new SoundPathSpecifier("/Audio/SS220/Effects/shield/eshild_loop.ogg");

    [ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier GeneratorOnSound = new SoundPathSpecifier("/Audio/SS220/Effects/shield/eshild_on.ogg");

    [ViewVariables(VVAccess.ReadOnly)]
    public SoundSpecifier GeneratorOffSound = new SoundPathSpecifier("/Audio/SS220/Effects/shield/eshild_off.ogg");

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? FieldEntity;

    [DataField]
    public ProtoId<SinkPortPrototype> TogglePort = "Toggle";

    [DataField]
    public EntProtoId ShieldProto = "forcefield220";
}

[NetSerializable, Serializable]
public enum ForcefieldGeneratorVisual
{
    Active,
    Power_1,
    Power_2,
    Power_3,
    Power_4
}
